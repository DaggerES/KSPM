using KSPM.IO.Compression;

using KSPM.Network.Common.Messages;

namespace KSPM.Network.Common.Packet
{
    public class PacketHandler
    {

        /// <summary>
        /// Size of the raw message header, it is used to send the lenght of sent bytes.
        /// </summary>
        public static readonly uint RawMessageHeaderSize = 4;

        /// <summary>
        /// Flag to indicate if the packets are compressed or not, by default is set to False.
        /// </summary>
        protected static bool CompressingPacketsEnabled = false;

        /// <summary>
        /// Tells how many bytes are required to start to consider a bunch of bytes as packet.
        /// </summary>
        public static readonly uint PrefixSize = 8;

        /// <summary>
        /// Reference to the compression methods.
        /// </summary>
        protected static Compressor CompressingObject;

        protected KSPM.IO.Memory.CyclicalMemoryBuffer memoryReference;
        protected byte[] workingBuffer;
        protected byte[] unpackedBytes;
        protected byte[] prefixBytes;
        protected byte[] packet;
        protected int unpackedBytesCounter;
        protected enum PacketStatus : byte { NoProcessed, Splitted, Complete, BeginHeaderIncomplete, EndHeaderIncomplete };

        #region StaticMethods

        /// <summary>
        /// Initialize the compression settings to be used by the PacketHandler. If the compression flag is set to True and the compression reference is null then the compression flag shall be set to false.
        /// </summary>
        /// <param name="compressionEnabled">Tells if the packets are going to be compressed.</param>
        /// <param name="compressionObject">Reference to the compression object. Sets null if the compressionEnabled flag is set to false.</param>
        public static void InitPacketHandler(bool compressionEnabled, ref Compressor compressionObject)
        {
            PacketHandler.CompressingPacketsEnabled = compressionEnabled;
            PacketHandler.CompressingObject = compressionObject;
            if (PacketHandler.CompressingPacketsEnabled && PacketHandler.CompressingObject == null)
                PacketHandler.CompressingPacketsEnabled = false;
        }

        /// <summary>
        /// Secont level of the KSPM Network model.
        /// Creates a ManagedMessage object from the byte array stored by the given NetworkEntity, the NetworkEntity reference is set as the owner of the messageTarget.
        /// </summary>
        /// <param name="bytesOwner">Reference to the NetworkEntity who holds the raw bytes, this reference is set as the message owner ether.</param>
        /// <param name="messageTarget">Message object which should have the result of handling the raw bytes.</param>
        /// <returns></returns>
        public static Error.ErrorType InflateManagedMessage(NetworkEntity bytesOwner, out Message messageTarget)
        {
            int bytesBlockSize;
            int byteCounter;
            byte[] rawBlockSize = new byte[4];
            messageTarget = null;
            if (bytesOwner.ownerNetworkCollection.secondaryRawBuffer.Length < 4)
                return Error.ErrorType.MessageBadFormat;
            System.Buffer.BlockCopy(bytesOwner.ownerNetworkCollection.secondaryRawBuffer, 0, rawBlockSize, 0, 4);
            bytesBlockSize = System.BitConverter.ToInt32(rawBlockSize, 0);
            //bytesBlockSize = System.BitConverter.ToInt32(bytesOwner.ownerNetworkCollection.secondaryRawBuffer, 0);
            if (bytesBlockSize < 4)
                return Error.ErrorType.MessageBadFormat;
            if (bytesBlockSize > Server.ServerSettings.ServerBufferSize)
                return Error.ErrorType.MessageCRCError;
            ///Verifying the packet end of message command.
            try
            {
                for (byteCounter = 1; byteCounter <= PacketHandler.RawMessageHeaderSize; byteCounter++)
                {
                    if ((bytesOwner.ownerNetworkCollection.secondaryRawBuffer[bytesBlockSize - byteCounter] & Message.EndOfMessageCommand[Message.EndOfMessageCommand.Length - byteCounter]) != bytesOwner.ownerNetworkCollection.secondaryRawBuffer[bytesBlockSize - byteCounter])
                    {
                        return Error.ErrorType.MessageBadFormat;
                    }
                }
            }
            catch (System.IndexOutOfRangeException ex)
            {
                KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo(string.Format("{0}-{1}", bytesOwner.Id, ex.Message));
            }
            messageTarget = new ManagedMessage((Message.CommandType)bytesOwner.ownerNetworkCollection.secondaryRawBuffer[4], bytesOwner);
            messageTarget.SetBodyMessage(bytesOwner.ownerNetworkCollection.secondaryRawBuffer, (uint)bytesBlockSize);
            return Error.ErrorType.Ok;
        }

        public static void PacketizeToOwner(byte[] rawBuffer, int bytesToRead, NetworkEntity packetOwner, IOwnedPacketArrived consumer)
        {
            uint availableBytes = (uint)bytesToRead;
            int messageBlockSize = 0;
            int physicalMessageBlockSize = 0;
            int index = 0;
            int searchedHeaderIndex = 0;
            int startsAtIndex = 0;
            int endsAtIndex = 0;
            bool startHeaderFound = false;
            bool endHeaderFound = false;
            if (availableBytes != 0)
            {
                for (index = 0; index < availableBytes; index++)///Now we are searching inside the working buffer.
                {
                    if (!startHeaderFound && !endHeaderFound)
                    {
                        if (rawBuffer[index] == Message.HeaderOfMessageCommand[searchedHeaderIndex])
                        {
                            searchedHeaderIndex++;
                            if (searchedHeaderIndex >= Message.HeaderOfMessageCommand.Length)///Means that we already find all bytes of the header.
                            {
                                startHeaderFound = true;
                                searchedHeaderIndex = 0;
                                startsAtIndex = index - Message.HeaderOfMessageCommand.Length + 1;
                            }
                        }
                    }
                    else
                    {
                        if (rawBuffer[index] == Message.EndOfMessageCommand[searchedHeaderIndex])
                        {
                            searchedHeaderIndex++;
                            if (searchedHeaderIndex >= Message.EndOfMessageCommand.Length)///Means that we already find all bytes of the header.
                            {
                                endHeaderFound = true;
                                endsAtIndex = index + 1;
                                searchedHeaderIndex = 0;
                            }
                        }
                    }
                    if (startHeaderFound && endHeaderFound)///Found both headers.
                    {
                        physicalMessageBlockSize = endsAtIndex - startsAtIndex;
                        messageBlockSize = System.BitConverter.ToInt32(rawBuffer, startsAtIndex + Message.HeaderOfMessageCommand.Length);
                        if (messageBlockSize < int.MaxValue && messageBlockSize == physicalMessageBlockSize)///To avoid bad messages.
                        {
                            consumer.ProcessPacket(rawBuffer, (uint)startsAtIndex, (uint)messageBlockSize, packetOwner);
                        }
                        endHeaderFound = startHeaderFound = false;
                        startsAtIndex = endsAtIndex = 0;
                    }
                }
            }
        }

        public static Error.ErrorType InflateBufferedMessage(byte[] rawBuffer, NetworkEntity bytesOwner, ref Message messageTarget)
        {
            int bytesBlockSize = 0;
            messageTarget = null;
            if (rawBuffer.Length < 4)
                return Error.ErrorType.MessageBadFormat;
            try
            {
                bytesBlockSize = System.BitConverter.ToInt32(rawBuffer, Message.HeaderOfMessageCommand.Length);
                if (bytesBlockSize < Message.HeaderOfMessageCommand.Length)
                    return Error.ErrorType.MessageBadFormat;
                if (bytesBlockSize > Server.ServerSettings.ServerBufferSize)
                    return Error.ErrorType.MessageCRCError;
                messageTarget = new ManagedMessage((Message.CommandType)rawBuffer[12], bytesOwner);
                messageTarget.SetBodyMessageNoClone(rawBuffer, (uint)bytesBlockSize);
            }
            catch (System.Exception ex)
            {
                KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo(string.Format("{0}-{1}:{2}", bytesOwner.Id, "Inflating error", ex.Message));
            }
            ///Verifying the packet end of message command.
            /*
            try
            {
                for (byteCounter = 1; byteCounter <= PacketHandler.RawMessageHeaderSize; byteCounter++)
                {
                    if ((rawBuffer[bytesBlockSize - byteCounter] & Message.EndOfMessageCommand[Message.EndOfMessageCommand.Length - byteCounter]) != rawBuffer[bytesBlockSize - byteCounter])
                    {
                        return Error.ErrorType.MessageBadFormat;
                    }
                }
            }
            catch (System.IndexOutOfRangeException ex)
            {
                KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo(string.Format("{0}-{1}", bytesOwner.Id, ex.Message));
            }
            */
            return Error.ErrorType.Ok;
        }

        public static Error.ErrorType InflateManagedMessageAlt(byte[] rawBuffer, NetworkEntity bytesOwner, out Message messageTarget)
        {
            int bytesBlockSize = 0;
            messageTarget = null;
            if (rawBuffer.Length < 4)
                return Error.ErrorType.MessageBadFormat;
            try
            {
                bytesBlockSize = System.BitConverter.ToInt32(rawBuffer, Message.HeaderOfMessageCommand.Length);
                if (bytesBlockSize < Message.HeaderOfMessageCommand.Length)
                    return Error.ErrorType.MessageBadFormat;
                if (bytesBlockSize > Server.ServerSettings.ServerBufferSize)
                    return Error.ErrorType.MessageCRCError;
                messageTarget = new ManagedMessage((Message.CommandType)rawBuffer[12], bytesOwner);
                messageTarget.SetBodyMessageNoClone(rawBuffer, (uint)bytesBlockSize);
                messageTarget.MessageId = (uint)System.BitConverter.ToInt32(messageTarget.bodyMessage, (int)PacketHandler.PrefixSize);
            }
            catch (System.Exception ex)
            {
                KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo(string.Format("{0}-{1}:{2}", bytesOwner.Id, "Inflating error",ex.Message));
            }
            ///Verifying the packet end of message command.
            /*
            try
            {
                for (byteCounter = 1; byteCounter <= PacketHandler.RawMessageHeaderSize; byteCounter++)
                {
                    if ((rawBuffer[bytesBlockSize - byteCounter] & Message.EndOfMessageCommand[Message.EndOfMessageCommand.Length - byteCounter]) != rawBuffer[bytesBlockSize - byteCounter])
                    {
                        return Error.ErrorType.MessageBadFormat;
                    }
                }
            }
            catch (System.IndexOutOfRangeException ex)
            {
                KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo(string.Format("{0}-{1}", bytesOwner.Id, ex.Message));
            }
            */
            return Error.ErrorType.Ok;
        }

        /// <summary>
        /// Lowest level method of the KSPM Network model, decompress the bytes if the compression flag is set to true, otherwise it is a passthrough method.
        /// </summary>
        /// <param name="rawBytes">Array of bytes in raw format.</param>
        /// <returns></returns>
        public static Error.ErrorType DecodeRawPacket(ref byte[] rawBytes)
        {
            byte[] decompressedBytes = null;
            if (rawBytes == null)
                return Error.ErrorType.MessageInvalidRawBytes;
            if (PacketHandler.CompressingPacketsEnabled)
            {
                try
                {
                    PacketHandler.CompressingObject.Decompress(ref rawBytes, out decompressedBytes);
                    System.Buffer.BlockCopy(decompressedBytes, 0, rawBytes, 0, rawBytes.Length);
                }
                catch (System.Exception)
                {
                    return Error.ErrorType.MessageCRCError;
                }
            }
            return Error.ErrorType.Ok;
        }

        /// <summary>
        /// Creates a Message object with the given NetworkEntity reference also it performs the compression method if the compression flag is set to True.
        /// </summary>
        /// <param name="rawBytes">Source bytes.</param>
        /// <returns>Error.ErrorType.Ok if there was not error.</returns>
        public static Error.ErrorType EncodeRawPacket(ref byte[] rawBytes)
        {
            byte [] compressedBytes = null;
            if (rawBytes == null)
                return Error.ErrorType.InvalidArray;
            if (PacketHandler.CompressingPacketsEnabled)
            {
                PacketHandler.CompressingObject.Compress(ref rawBytes, out compressedBytes);
                System.Buffer.BlockCopy(compressedBytes, 0, rawBytes, 0, rawBytes.Length);
            }
            return Error.ErrorType.Ok;
        }

        /// <summary>
        /// Secont level of the KSPM Network model.
        /// Creates a RawMessage object from the given byte array.
        /// </summary>
        /// <param name="rawBytes">Byte array contaning the message in raw format.</param>
        /// <param name="messageTarget">Out reference to the message to create.</param>
        /// <returns></returns>
        public static Error.ErrorType InflateRawMessage(byte[] rawBytes, out Message messageTarget)
        {
            int bytesBlockSize;
            messageTarget = null;
            if (rawBytes.Length < 4)
                return Error.ErrorType.MessageBadFormat;
            bytesBlockSize = System.BitConverter.ToInt32(rawBytes, Message.HeaderOfMessageCommand.Length);
            if (bytesBlockSize < 4)
                return Error.ErrorType.MessageBadFormat;
            /*
            ///Verifying the packet end of message command.
            for (byteCounter = 1; byteCounter <= PacketHandler.RawMessageHeaderSize; byteCounter++)
            {
                if ((rawBytes[bytesBlockSize - byteCounter] & Message.EndOfMessageCommand[Message.EndOfMessageCommand.Length - byteCounter]) != rawBytes[bytesBlockSize - byteCounter])
                {
                    return Error.ErrorType.MessageBadFormat;
                }
            }
            */
            messageTarget = new RawMessage((Message.CommandType)rawBytes[ PacketHandler.PrefixSize ], rawBytes, (uint)bytesBlockSize);
            return Error.ErrorType.Ok;
        }

        #endregion

        public PacketHandler( KSPM.IO.Memory.CyclicalMemoryBuffer memoryReference )
        {
            this.memoryReference = memoryReference;
            this.workingBuffer = new byte[this.memoryReference.FixedLength];
            this.unpackedBytes = new byte[this.memoryReference.FixedLength];
            this.prefixBytes = new byte[PacketHandler.PrefixSize];
            this.packet = new byte[this.memoryReference.FixedLength * 10];
            this.unpackedBytesCounter = 0;
        }

        /// <summary>
        /// Releases all the resources holded by thie object.
        /// </summary>
        public void Release()
        {
            this.workingBuffer = null;
            this.unpackedBytes = null;
            this.prefixBytes = null;
            this.packet = null;
            if (this.memoryReference != null && this.memoryReference.FixedLength > 0)
            {
                this.memoryReference.Release();
                this.memoryReference = null;
            }
        }

        /// <summary>
        /// <b>Used by the server itself.</b>
        /// Packetize the incoming bytes, searching the Begin and End headers, then calls to the ProcessPacket method.
        /// </summary>
        /// <param name="consumer"></param>
        public void PacketizeCRC(IPacketArrived consumer)
        {
            uint availableBytes = this.memoryReference.Read(ref this.workingBuffer);
            int messageBlockSize = 0;
            int physicalMessageBlockSize = 0;
            int index = 0;
            int searchedHeaderIndex = 0;
            int startsAtIndex = 0;
            int endsAtIndex = 0;
            int stolenBytesFromWorkingBuffer = 0;
            bool startHeaderFound = false;
            bool endHeaderFound = false;
            PacketStatus packetStatus = PacketStatus.NoProcessed;
            if (availableBytes != 0)
            {
                for (index = 0; index < this.unpackedBytesCounter; index++)///Scan the unpacked bytes first.
                {
                    if (!startHeaderFound && !endHeaderFound)
                    {
                        if (this.unpackedBytes[index] == Message.HeaderOfMessageCommand[searchedHeaderIndex])
                        {
                            this.prefixBytes[searchedHeaderIndex] = this.unpackedBytes[index];
                            searchedHeaderIndex++;
                            packetStatus = PacketStatus.BeginHeaderIncomplete;
                            if (searchedHeaderIndex >= Message.HeaderOfMessageCommand.Length)///Means that we already find all bytes of the header.
                            {
                                startHeaderFound = true;
                                searchedHeaderIndex = 0;
                                startsAtIndex = index - Message.HeaderOfMessageCommand.Length + 1;
                                packetStatus = PacketStatus.Splitted;
                            }
                        }
                    }
                    else
                    {
                        if (this.unpackedBytes[index] == Message.EndOfMessageCommand[searchedHeaderIndex])
                        {
                            searchedHeaderIndex++;
                            packetStatus = PacketStatus.EndHeaderIncomplete;
                            if (searchedHeaderIndex >= Message.EndOfMessageCommand.Length)///Means that we already find all bytes of the header.
                            {
                                endHeaderFound = true;
                                endsAtIndex = index + 1;
                                packetStatus = PacketStatus.Complete;
                                searchedHeaderIndex = 0;
                            }
                        }
                    }
                    if (startHeaderFound && endHeaderFound)///Found both headers on the same buffer.
                    {
                        physicalMessageBlockSize = endsAtIndex - startsAtIndex;
                        messageBlockSize = System.BitConverter.ToInt32(this.unpackedBytes, startsAtIndex);
                        if (messageBlockSize < int.MaxValue && messageBlockSize == physicalMessageBlockSize)///To avoid bad messages.
                        {
                            //packet = new byte[messageBlockSize];
                            System.Buffer.BlockCopy(this.unpackedBytes, startsAtIndex, this.packet, 0, messageBlockSize);
                            //consumer.ProcessPacket(this.packet, (uint)messageBlockSize);
                            consumer.ProcessPacket(this.packet, 0, (uint)messageBlockSize);
                            packetStatus = PacketStatus.NoProcessed;
                        }
                        endHeaderFound = startHeaderFound = false;
                        startsAtIndex = endsAtIndex = 0;
                    }
                }


                for (index = 0; index < availableBytes; index++)///Now we are searching inside the working buffer.
                {
                    if (!startHeaderFound && !endHeaderFound)
                    {
                        if (this.workingBuffer[index] == Message.HeaderOfMessageCommand[searchedHeaderIndex])
                        {
                            this.prefixBytes[searchedHeaderIndex] = this.workingBuffer[index];
                            searchedHeaderIndex++;
                            if (searchedHeaderIndex >= Message.HeaderOfMessageCommand.Length)///Means that we already find all bytes of the header.
                            {
                                startHeaderFound = true;
                                searchedHeaderIndex = 0;
                                startsAtIndex = index - Message.HeaderOfMessageCommand.Length + 1;
                            }
                        }
                    }
                    else
                    {
                        if (this.workingBuffer[index] == Message.EndOfMessageCommand[searchedHeaderIndex])
                        {
                            searchedHeaderIndex++;
                            if (searchedHeaderIndex >= Message.EndOfMessageCommand.Length)///Means that we already find all bytes of the header.
                            {
                                endHeaderFound = true;
                                endsAtIndex = index + 1;
                                searchedHeaderIndex = 0;
                            }
                        }
                    }
                    if (startHeaderFound && endHeaderFound)///Found both headers.
                    {
                        if (packetStatus == PacketStatus.Splitted || packetStatus == PacketStatus.EndHeaderIncomplete)
                        {
                            if (this.unpackedBytesCounter - startsAtIndex >= PacketHandler.PrefixSize)///The whole prefix is unpacked.
                            {
                                messageBlockSize = System.BitConverter.ToInt32(this.unpackedBytes, startsAtIndex + Message.HeaderOfMessageCommand.Length);
                                physicalMessageBlockSize = endsAtIndex + this.unpackedBytesCounter - startsAtIndex;
                                if (messageBlockSize < int.MaxValue && messageBlockSize == physicalMessageBlockSize)///To avoid bad messages.
                                {
                                    //packet = new byte[messageBlockSize];
                                    ///Copying those bytes stored inside the unpacked bytes array.
                                    System.Buffer.BlockCopy(this.unpackedBytes, startsAtIndex, this.packet, 0, this.unpackedBytesCounter - startsAtIndex);

                                    ///Copying the rest of the message stored inside the working array.
                                    System.Buffer.BlockCopy(this.workingBuffer, 0, this.packet, this.unpackedBytesCounter, endsAtIndex);
                                    //consumer.ProcessPacket(this.packet, (uint)messageBlockSize);
                                    consumer.ProcessPacket(this.packet, 0, (uint)messageBlockSize);
                                    packetStatus = PacketStatus.NoProcessed;
                                }
                            }
                            else
                            {
                                KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo("Avoiding");
                                stolenBytesFromWorkingBuffer = 4 - (this.unpackedBytesCounter - (startsAtIndex + Message.HeaderOfMessageCommand.Length));
                                ///Copying those bytes at the end of the unpacked buffer.
                                System.Buffer.BlockCopy(this.unpackedBytes, startsAtIndex + Message.HeaderOfMessageCommand.Length, this.prefixBytes, Message.HeaderOfMessageCommand.Length, this.unpackedBytesCounter - (startsAtIndex + Message.HeaderOfMessageCommand.Length));
                                ///Copying the rest of the needed bytes to convert them into a int32
                                System.Buffer.BlockCopy(this.workingBuffer, 0, this.prefixBytes, Message.HeaderOfMessageCommand.Length + (this.unpackedBytesCounter - (startsAtIndex + Message.HeaderOfMessageCommand.Length)), stolenBytesFromWorkingBuffer);
                                messageBlockSize = System.BitConverter.ToInt32(this.prefixBytes, Message.HeaderOfMessageCommand.Length);
                                physicalMessageBlockSize = endsAtIndex + this.unpackedBytesCounter - startsAtIndex;
                                if (messageBlockSize < int.MaxValue && messageBlockSize == physicalMessageBlockSize)///To avoid bad messages.
                                {
                                    //packet = new byte[messageBlockSize];
                                    System.Buffer.BlockCopy(this.prefixBytes, 0, this.packet, 0, (int)PacketHandler.PrefixSize);
                                    System.Buffer.BlockCopy(this.workingBuffer, stolenBytesFromWorkingBuffer, this.packet, (int)PacketHandler.PrefixSize, endsAtIndex - stolenBytesFromWorkingBuffer);
                                    //consumer.ProcessPacket(this.packet, (uint)messageBlockSize);
                                    consumer.ProcessPacket(this.packet, 0, (uint)messageBlockSize);
                                    packetStatus = PacketStatus.NoProcessed;
                                }
                            }
                        }
                        else
                        {
                            messageBlockSize = System.BitConverter.ToInt32(this.workingBuffer, startsAtIndex + Message.HeaderOfMessageCommand.Length);
                            physicalMessageBlockSize = endsAtIndex - startsAtIndex;
                            if (messageBlockSize < int.MaxValue && messageBlockSize == physicalMessageBlockSize)///To avoid bad messages.
                            {
                                //packet = new byte[messageBlockSize];
                                if (packetStatus == PacketStatus.BeginHeaderIncomplete)
                                {
                                    System.Buffer.BlockCopy(this.prefixBytes, 0, this.packet, 0, Message.HeaderOfMessageCommand.Length);
                                    System.Buffer.BlockCopy(this.workingBuffer, startsAtIndex + Message.HeaderOfMessageCommand.Length, this.packet, Message.HeaderOfMessageCommand.Length, messageBlockSize - Message.HeaderOfMessageCommand.Length);
                                }
                                else
                                {
                                    System.Buffer.BlockCopy(this.workingBuffer, startsAtIndex, this.packet, 0, messageBlockSize);
                                }
                                //consumer.ProcessPacket(this.packet, (uint)messageBlockSize);
                                consumer.ProcessPacket(this.packet, 0, (uint)messageBlockSize);
                                packetStatus = PacketStatus.NoProcessed;
                            }
                            //startsAtIndex = endsAtIndex = 0;
                        }
                        endHeaderFound = startHeaderFound = false;
                        //startsAtIndex = endsAtIndex = 0;
                    }
                }
                this.unpackedBytesCounter = (int)availableBytes - endsAtIndex;
                if (this.unpackedBytesCounter > 0)
                {
                    System.Buffer.BlockCopy(this.workingBuffer, endsAtIndex, this.unpackedBytes, 0, (int)availableBytes - endsAtIndex);
                }
            }
        }

        public void PacketizeCRCCreateMemory(IPacketArrived consumer)
        {
            uint availableBytes = this.memoryReference.Read(ref this.workingBuffer);
            int messageBlockSize = 0;
            int physicalMessageBlockSize = 0;
            int index = 0;
            int searchedHeaderIndex = 0;
            int startsAtIndex = 0;
            int endsAtIndex = 0;
            int stolenBytesFromWorkingBuffer = 0;
            bool startHeaderFound = false;
            bool endHeaderFound = false;
            PacketStatus packetStatus = PacketStatus.NoProcessed;
            if (availableBytes != 0)
            {
                for (index = 0; index < this.unpackedBytesCounter; index++)///Scan the unpacked bytes first.
                {
                    if (!startHeaderFound && !endHeaderFound)
                    {
                        if (this.unpackedBytes[index] == Message.HeaderOfMessageCommand[searchedHeaderIndex])
                        {
                            this.prefixBytes[searchedHeaderIndex] = this.unpackedBytes[index];
                            searchedHeaderIndex++;
                            packetStatus = PacketStatus.BeginHeaderIncomplete;
                            if (searchedHeaderIndex >= Message.HeaderOfMessageCommand.Length)///Means that we already find all bytes of the header.
                            {
                                startHeaderFound = true;
                                searchedHeaderIndex = 0;
                                startsAtIndex = index - Message.HeaderOfMessageCommand.Length + 1;
                                packetStatus = PacketStatus.Splitted;
                            }
                        }
                    }
                    else
                    {
                        if (this.unpackedBytes[index] == Message.EndOfMessageCommand[searchedHeaderIndex])
                        {
                            searchedHeaderIndex++;
                            packetStatus = PacketStatus.EndHeaderIncomplete;
                            if (searchedHeaderIndex >= Message.EndOfMessageCommand.Length)///Means that we already find all bytes of the header.
                            {
                                endHeaderFound = true;
                                endsAtIndex = index + 1;
                                packetStatus = PacketStatus.Complete;
                                searchedHeaderIndex = 0;
                            }
                        }
                    }
                    if (startHeaderFound && endHeaderFound)///Found both headers on the same buffer.
                    {
                        physicalMessageBlockSize = endsAtIndex - startsAtIndex;
                        messageBlockSize = System.BitConverter.ToInt32(this.unpackedBytes, startsAtIndex);
                        if (messageBlockSize < int.MaxValue && messageBlockSize == physicalMessageBlockSize)///To avoid bad messages.
                        {
                            packet = new byte[messageBlockSize];
                            System.Buffer.BlockCopy(this.unpackedBytes, startsAtIndex, packet, 0, messageBlockSize);
                            consumer.ProcessPacket(packet, (uint)messageBlockSize);
                            //consumer.ProcessPacket(this.packet, 0, (uint)messageBlockSize);
                            packetStatus = PacketStatus.NoProcessed;
                        }
                        endHeaderFound = startHeaderFound = false;
                        startsAtIndex = endsAtIndex = 0;
                    }
                }


                for (index = 0; index < availableBytes; index++)///Now we are searching inside the working buffer.
                {
                    if (!startHeaderFound && !endHeaderFound)
                    {
                        if (this.workingBuffer[index] == Message.HeaderOfMessageCommand[searchedHeaderIndex])
                        {
                            this.prefixBytes[searchedHeaderIndex] = this.workingBuffer[index];
                            searchedHeaderIndex++;
                            if (searchedHeaderIndex >= Message.HeaderOfMessageCommand.Length)///Means that we already find all bytes of the header.
                            {
                                startHeaderFound = true;
                                searchedHeaderIndex = 0;
                                startsAtIndex = index - Message.HeaderOfMessageCommand.Length + 1;
                            }
                        }
                    }
                    else
                    {
                        if (this.workingBuffer[index] == Message.EndOfMessageCommand[searchedHeaderIndex])
                        {
                            searchedHeaderIndex++;
                            if (searchedHeaderIndex >= Message.EndOfMessageCommand.Length)///Means that we already find all bytes of the header.
                            {
                                endHeaderFound = true;
                                endsAtIndex = index + 1;
                                searchedHeaderIndex = 0;
                            }
                        }
                    }
                    if (startHeaderFound && endHeaderFound)///Found both headers.
                    {
                        if (packetStatus == PacketStatus.Splitted || packetStatus == PacketStatus.EndHeaderIncomplete)
                        {
                            if (this.unpackedBytesCounter - startsAtIndex >= PacketHandler.PrefixSize)///The whole prefix is unpacked.
                            {
                                messageBlockSize = System.BitConverter.ToInt32(this.unpackedBytes, startsAtIndex + Message.HeaderOfMessageCommand.Length);
                                physicalMessageBlockSize = endsAtIndex + this.unpackedBytesCounter - startsAtIndex;
                                if (messageBlockSize < int.MaxValue && messageBlockSize == physicalMessageBlockSize)///To avoid bad messages.
                                {
                                    packet = new byte[messageBlockSize];
                                    ///Copying those bytes stored inside the unpacked bytes array.
                                    System.Buffer.BlockCopy(this.unpackedBytes, startsAtIndex, packet, 0, this.unpackedBytesCounter - startsAtIndex);

                                    ///Copying the rest of the message stored inside the working array.
                                    System.Buffer.BlockCopy(this.workingBuffer, 0, packet, this.unpackedBytesCounter, endsAtIndex);
                                    consumer.ProcessPacket(this.packet, (uint)messageBlockSize);
                                    //consumer.ProcessPacket(this.packet, 0, (uint)messageBlockSize);
                                    packetStatus = PacketStatus.NoProcessed;
                                }
                            }
                            else
                            {
                                KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo("UPS");
                                
                                stolenBytesFromWorkingBuffer = 4 - (this.unpackedBytesCounter - (startsAtIndex + Message.HeaderOfMessageCommand.Length));
                                ///Copying those bytes at the end of the unpacked buffer.
                                System.Buffer.BlockCopy(this.unpackedBytes, startsAtIndex + Message.HeaderOfMessageCommand.Length, this.prefixBytes, Message.HeaderOfMessageCommand.Length, this.unpackedBytesCounter - (startsAtIndex + Message.HeaderOfMessageCommand.Length));
                                ///Copying the rest of the needed bytes to convert them into a int32
                                System.Buffer.BlockCopy(this.workingBuffer, 0, this.prefixBytes, Message.HeaderOfMessageCommand.Length + (this.unpackedBytesCounter - (startsAtIndex + Message.HeaderOfMessageCommand.Length)), stolenBytesFromWorkingBuffer);
                                messageBlockSize = System.BitConverter.ToInt32(this.prefixBytes, Message.HeaderOfMessageCommand.Length);
                                physicalMessageBlockSize = endsAtIndex + this.unpackedBytesCounter - startsAtIndex;
                                if (messageBlockSize < int.MaxValue && messageBlockSize == physicalMessageBlockSize)///To avoid bad messages.
                                {
                                    packet = new byte[messageBlockSize];
                                    System.Buffer.BlockCopy(this.prefixBytes, 0, packet, 0, (int)PacketHandler.PrefixSize);
                                    System.Buffer.BlockCopy(this.workingBuffer, stolenBytesFromWorkingBuffer, packet, (int)PacketHandler.PrefixSize, endsAtIndex - stolenBytesFromWorkingBuffer);
                                    consumer.ProcessPacket(packet, (uint)messageBlockSize);
                                    //consumer.ProcessPacket(this.packet, 0, (uint)messageBlockSize);
                                    packetStatus = PacketStatus.NoProcessed;
                                }
                                
                            }
                        }
                        else
                        {
                            messageBlockSize = System.BitConverter.ToInt32(this.workingBuffer, startsAtIndex + Message.HeaderOfMessageCommand.Length);
                            physicalMessageBlockSize = endsAtIndex - startsAtIndex;
                            if (messageBlockSize < int.MaxValue && messageBlockSize == physicalMessageBlockSize)///To avoid bad messages.
                            {
                                packet = new byte[messageBlockSize];
                                if (packetStatus == PacketStatus.BeginHeaderIncomplete)
                                {
                                    System.Buffer.BlockCopy(this.prefixBytes, 0, packet, 0, Message.HeaderOfMessageCommand.Length);
                                    System.Buffer.BlockCopy(this.workingBuffer, startsAtIndex + Message.HeaderOfMessageCommand.Length, packet, Message.HeaderOfMessageCommand.Length, messageBlockSize - Message.HeaderOfMessageCommand.Length);
                                }
                                else
                                {
                                    System.Buffer.BlockCopy(this.workingBuffer, startsAtIndex, packet, 0, messageBlockSize);
                                }
                                consumer.ProcessPacket(packet, (uint)messageBlockSize);
                                //consumer.ProcessPacket(this.packet, 0, (uint)messageBlockSize);
                                packetStatus = PacketStatus.NoProcessed;
                            }
                            //startsAtIndex = endsAtIndex = 0;
                        }
                        endHeaderFound = startHeaderFound = false;
                        //startsAtIndex = endsAtIndex = 0;
                    }
                }
                this.unpackedBytesCounter = (int)availableBytes - endsAtIndex;
                if (this.unpackedBytesCounter > 0)
                {
                    System.Buffer.BlockCopy(this.workingBuffer, endsAtIndex, this.unpackedBytes, 0, (int)availableBytes - endsAtIndex);
                }
            }
        }

        public void UDPPacketizeCRCMemoryAlloc(IUDPPacketArrived consumer)
        {
            uint availableBytes = this.memoryReference.Read(ref this.workingBuffer);
            int messageBlockSize = 0;
            int physicalMessageBlockSize = 0;
            int index = 0;
            int searchedHeaderIndex = 0;
            int startsAtIndex = 0;
            int endsAtIndex = 0;
            int stolenBytesFromWorkingBuffer = 0;
            bool startHeaderFound = false;
            bool endHeaderFound = false;
            PacketStatus packetStatus = PacketStatus.NoProcessed;
            if (availableBytes != 0)
            {
                for (index = 0; index < this.unpackedBytesCounter; index++)///Scan the unpacked bytes first.
                {
                    if (!startHeaderFound && !endHeaderFound)
                    {
                        if (this.unpackedBytes[index] == Message.HeaderOfMessageCommand[searchedHeaderIndex])
                        {
                            this.prefixBytes[searchedHeaderIndex] = this.unpackedBytes[index];
                            searchedHeaderIndex++;
                            packetStatus = PacketStatus.BeginHeaderIncomplete;
                            if (searchedHeaderIndex >= Message.HeaderOfMessageCommand.Length)///Means that we already find all bytes of the header.
                            {
                                startHeaderFound = true;
                                searchedHeaderIndex = 0;
                                startsAtIndex = index - Message.HeaderOfMessageCommand.Length + 1;
                                packetStatus = PacketStatus.Splitted;
                            }
                        }
                    }
                    else
                    {
                        if (this.unpackedBytes[index] == Message.EndOfMessageCommand[searchedHeaderIndex])
                        {
                            searchedHeaderIndex++;
                            packetStatus = PacketStatus.EndHeaderIncomplete;
                            if (searchedHeaderIndex >= Message.EndOfMessageCommand.Length)///Means that we already find all bytes of the header.
                            {
                                endHeaderFound = true;
                                endsAtIndex = index + 1;
                                packetStatus = PacketStatus.Complete;
                                searchedHeaderIndex = 0;
                            }
                        }
                    }
                    if (startHeaderFound && endHeaderFound)///Found both headers on the same buffer.
                    {
                        physicalMessageBlockSize = endsAtIndex - startsAtIndex;
                        messageBlockSize = System.BitConverter.ToInt32(this.unpackedBytes, startsAtIndex);
                        if (messageBlockSize < int.MaxValue && messageBlockSize == physicalMessageBlockSize)///To avoid bad messages.
                        {
                            packet = new byte[messageBlockSize];
                            System.Buffer.BlockCopy(this.unpackedBytes, startsAtIndex, packet, 0, messageBlockSize);
                            consumer.ProcessUDPPacket(packet, (uint)messageBlockSize);
                            //consumer.ProcessPacket(this.packet, 0, (uint)messageBlockSize);
                            packetStatus = PacketStatus.NoProcessed;
                        }
                        endHeaderFound = startHeaderFound = false;
                        startsAtIndex = endsAtIndex = 0;
                    }
                }


                for (index = 0; index < availableBytes; index++)///Now we are searching inside the working buffer.
                {
                    if (!startHeaderFound && !endHeaderFound)
                    {
                        if (this.workingBuffer[index] == Message.HeaderOfMessageCommand[searchedHeaderIndex])
                        {
                            this.prefixBytes[searchedHeaderIndex] = this.workingBuffer[index];
                            searchedHeaderIndex++;
                            if (searchedHeaderIndex >= Message.HeaderOfMessageCommand.Length)///Means that we already find all bytes of the header.
                            {
                                startHeaderFound = true;
                                searchedHeaderIndex = 0;
                                startsAtIndex = index - Message.HeaderOfMessageCommand.Length + 1;
                            }
                        }
                    }
                    else
                    {
                        if (this.workingBuffer[index] == Message.EndOfMessageCommand[searchedHeaderIndex])
                        {
                            searchedHeaderIndex++;
                            if (searchedHeaderIndex >= Message.EndOfMessageCommand.Length)///Means that we already find all bytes of the header.
                            {
                                endHeaderFound = true;
                                endsAtIndex = index + 1;
                                searchedHeaderIndex = 0;
                            }
                        }
                    }
                    if (startHeaderFound && endHeaderFound)///Found both headers.
                    {
                        if (packetStatus == PacketStatus.Splitted || packetStatus == PacketStatus.EndHeaderIncomplete)
                        {
                            if (this.unpackedBytesCounter - startsAtIndex >= PacketHandler.PrefixSize)///The whole prefix is unpacked.
                            {
                                messageBlockSize = System.BitConverter.ToInt32(this.unpackedBytes, startsAtIndex + Message.HeaderOfMessageCommand.Length);
                                physicalMessageBlockSize = endsAtIndex + this.unpackedBytesCounter - startsAtIndex;
                                if (messageBlockSize < int.MaxValue && messageBlockSize == physicalMessageBlockSize)///To avoid bad messages.
                                {
                                    packet = new byte[messageBlockSize];
                                    ///Copying those bytes stored inside the unpacked bytes array.
                                    System.Buffer.BlockCopy(this.unpackedBytes, startsAtIndex, packet, 0, this.unpackedBytesCounter - startsAtIndex);

                                    ///Copying the rest of the message stored inside the working array.
                                    System.Buffer.BlockCopy(this.workingBuffer, 0, packet, this.unpackedBytesCounter, endsAtIndex);
                                    consumer.ProcessUDPPacket(this.packet, (uint)messageBlockSize);
                                    //consumer.ProcessPacket(this.packet, 0, (uint)messageBlockSize);
                                    packetStatus = PacketStatus.NoProcessed;
                                }
                            }
                            else
                            {
                                stolenBytesFromWorkingBuffer = 4 - (this.unpackedBytesCounter - (startsAtIndex + Message.HeaderOfMessageCommand.Length));
                                ///Copying those bytes at the end of the unpacked buffer.
                                System.Buffer.BlockCopy(this.unpackedBytes, startsAtIndex + Message.HeaderOfMessageCommand.Length, this.prefixBytes, Message.HeaderOfMessageCommand.Length, this.unpackedBytesCounter - (startsAtIndex + Message.HeaderOfMessageCommand.Length));
                                ///Copying the rest of the needed bytes to convert them into a int32
                                System.Buffer.BlockCopy(this.workingBuffer, 0, this.prefixBytes, Message.HeaderOfMessageCommand.Length + (this.unpackedBytesCounter - (startsAtIndex + Message.HeaderOfMessageCommand.Length)), stolenBytesFromWorkingBuffer);
                                messageBlockSize = System.BitConverter.ToInt32(this.prefixBytes, Message.HeaderOfMessageCommand.Length);
                                physicalMessageBlockSize = endsAtIndex + this.unpackedBytesCounter - startsAtIndex;
                                if (messageBlockSize < int.MaxValue && messageBlockSize == physicalMessageBlockSize)///To avoid bad messages.
                                {
                                    packet = new byte[messageBlockSize];
                                    System.Buffer.BlockCopy(this.prefixBytes, 0, packet, 0, (int)PacketHandler.PrefixSize);
                                    System.Buffer.BlockCopy(this.workingBuffer, stolenBytesFromWorkingBuffer, packet, (int)PacketHandler.PrefixSize, endsAtIndex - stolenBytesFromWorkingBuffer);
                                    consumer.ProcessUDPPacket(packet, (uint)messageBlockSize);
                                    //consumer.ProcessPacket(this.packet, 0, (uint)messageBlockSize);
                                    packetStatus = PacketStatus.NoProcessed;
                                }
                            }
                        }
                        else
                        {
                            messageBlockSize = System.BitConverter.ToInt32(this.workingBuffer, startsAtIndex + Message.HeaderOfMessageCommand.Length);
                            physicalMessageBlockSize = endsAtIndex - startsAtIndex;
                            if (messageBlockSize < int.MaxValue && messageBlockSize == physicalMessageBlockSize)///To avoid bad messages.
                            {
                                packet = new byte[messageBlockSize];
                                if (packetStatus == PacketStatus.BeginHeaderIncomplete)
                                {
                                    System.Buffer.BlockCopy(this.prefixBytes, 0, packet, 0, Message.HeaderOfMessageCommand.Length);
                                    System.Buffer.BlockCopy(this.workingBuffer, startsAtIndex + Message.HeaderOfMessageCommand.Length, packet, Message.HeaderOfMessageCommand.Length, messageBlockSize - Message.HeaderOfMessageCommand.Length);
                                }
                                else
                                {
                                    System.Buffer.BlockCopy(this.workingBuffer, startsAtIndex, packet, 0, messageBlockSize);
                                }
                                consumer.ProcessUDPPacket(packet, (uint)messageBlockSize);
                                //consumer.ProcessPacket(this.packet, 0, (uint)messageBlockSize);
                                packetStatus = PacketStatus.NoProcessed;
                            }
                            //startsAtIndex = endsAtIndex = 0;
                        }
                        endHeaderFound = startHeaderFound = false;
                        //startsAtIndex = endsAtIndex = 0;
                    }
                }
                this.unpackedBytesCounter = (int)availableBytes - endsAtIndex;
                if (this.unpackedBytesCounter > 0)
                {
                    System.Buffer.BlockCopy(this.workingBuffer, endsAtIndex, this.unpackedBytes, 0, (int)availableBytes - endsAtIndex);
                }
            }
        }

        /// <summary>
        /// Packetizes the incoming bytes and takes a Message from the pool.
        /// </summary>
        /// <param name="consumer"></param>
        /// <param name="pooling"></param>
        public void UDPPacketizeCRCLoadIntoMessage(IUDPPacketArrived consumer, MessagesPool pooling)
        {
            uint availableBytes = this.memoryReference.Read(ref this.workingBuffer);
            int messageBlockSize = 0;
            int physicalMessageBlockSize = 0;
            int index = 0;
            int searchedHeaderIndex = 0;
            int startsAtIndex = 0;
            int endsAtIndex = 0;
            int stolenBytesFromWorkingBuffer = 0;
            bool startHeaderFound = false;
            bool endHeaderFound = false;
            Message incomingMessage = null;
            PacketStatus packetStatus = PacketStatus.NoProcessed;
            if (availableBytes != 0)
            {
                for (index = 0; index < this.unpackedBytesCounter; index++)///Scan the unpacked bytes first.
                {
                    if (!startHeaderFound && !endHeaderFound)
                    {
                        if (this.unpackedBytes[index] == Message.HeaderOfMessageCommand[searchedHeaderIndex])
                        {
                            this.prefixBytes[searchedHeaderIndex] = this.unpackedBytes[index];
                            searchedHeaderIndex++;
                            packetStatus = PacketStatus.BeginHeaderIncomplete;
                            if (searchedHeaderIndex >= Message.HeaderOfMessageCommand.Length)///Means that we already find all bytes of the header.
                            {
                                startHeaderFound = true;
                                searchedHeaderIndex = 0;
                                startsAtIndex = index - Message.HeaderOfMessageCommand.Length + 1;
                                packetStatus = PacketStatus.Splitted;
                            }
                        }
                    }
                    else
                    {
                        if (this.unpackedBytes[index] == Message.EndOfMessageCommand[searchedHeaderIndex])
                        {
                            searchedHeaderIndex++;
                            packetStatus = PacketStatus.EndHeaderIncomplete;
                            if (searchedHeaderIndex >= Message.EndOfMessageCommand.Length)///Means that we already find all bytes of the header.
                            {
                                endHeaderFound = true;
                                endsAtIndex = index + 1;
                                packetStatus = PacketStatus.Complete;
                                searchedHeaderIndex = 0;
                            }
                        }
                    }
                    if (startHeaderFound && endHeaderFound)///Found both headers on the same buffer.
                    {
                        physicalMessageBlockSize = endsAtIndex - startsAtIndex;
                        messageBlockSize = System.BitConverter.ToInt32(this.unpackedBytes, startsAtIndex);
                        if (messageBlockSize < int.MaxValue && messageBlockSize == physicalMessageBlockSize)///To avoid bad messages.
                        {
                            incomingMessage = pooling.BorrowMessage;
                            System.Buffer.BlockCopy(this.unpackedBytes, startsAtIndex, incomingMessage.bodyMessage, 0, messageBlockSize);
                            incomingMessage.MessageBytesSize = (uint)messageBlockSize;
                            ((RawMessage)incomingMessage).ReallocateCommand();
                            consumer.ProcessUDPMessage(incomingMessage);
                            packetStatus = PacketStatus.NoProcessed;
                        }
                        endHeaderFound = startHeaderFound = false;
                        startsAtIndex = endsAtIndex = 0;
                    }
                }


                for (index = 0; index < availableBytes; index++)///Now we are searching inside the working buffer.
                {
                    if (!startHeaderFound && !endHeaderFound)
                    {
                        if (this.workingBuffer[index] == Message.HeaderOfMessageCommand[searchedHeaderIndex])
                        {
                            this.prefixBytes[searchedHeaderIndex] = this.workingBuffer[index];
                            searchedHeaderIndex++;
                            if (searchedHeaderIndex >= Message.HeaderOfMessageCommand.Length)///Means that we already find all bytes of the header.
                            {
                                startHeaderFound = true;
                                searchedHeaderIndex = 0;
                                startsAtIndex = index - Message.HeaderOfMessageCommand.Length + 1;
                            }
                        }
                    }
                    else
                    {
                        if (this.workingBuffer[index] == Message.EndOfMessageCommand[searchedHeaderIndex])
                        {
                            searchedHeaderIndex++;
                            if (searchedHeaderIndex >= Message.EndOfMessageCommand.Length)///Means that we already find all bytes of the header.
                            {
                                endHeaderFound = true;
                                endsAtIndex = index + 1;
                                searchedHeaderIndex = 0;
                            }
                        }
                    }
                    if (startHeaderFound && endHeaderFound)///Found both headers.
                    {
                        if (packetStatus == PacketStatus.Splitted || packetStatus == PacketStatus.EndHeaderIncomplete)
                        {
                            if (this.unpackedBytesCounter - startsAtIndex >= PacketHandler.PrefixSize)///The whole prefix is unpacked.
                            {
                                messageBlockSize = System.BitConverter.ToInt32(this.unpackedBytes, startsAtIndex + Message.HeaderOfMessageCommand.Length);
                                physicalMessageBlockSize = endsAtIndex + this.unpackedBytesCounter - startsAtIndex;
                                if (messageBlockSize < int.MaxValue && messageBlockSize == physicalMessageBlockSize)///To avoid bad messages.
                                {
                                    incomingMessage = pooling.BorrowMessage;
                                    ///Copying those bytes stored inside the unpacked bytes array.
                                    System.Buffer.BlockCopy(this.unpackedBytes, startsAtIndex, incomingMessage.bodyMessage, 0, this.unpackedBytesCounter - startsAtIndex);

                                    ///Copying the rest of the message stored inside the working array.
                                    System.Buffer.BlockCopy(this.workingBuffer, 0, incomingMessage.bodyMessage, this.unpackedBytesCounter, endsAtIndex);
                                    incomingMessage.MessageBytesSize = (uint)messageBlockSize;
                                    ((RawMessage)incomingMessage).ReallocateCommand();
                                    consumer.ProcessUDPMessage(incomingMessage);
                                    //consumer.ProcessUDPPacket(this.packet, (uint)messageBlockSize);
                                    //consumer.ProcessPacket(this.packet, 0, (uint)messageBlockSize);
                                    packetStatus = PacketStatus.NoProcessed;
                                }
                            }
                            else
                            {
                                stolenBytesFromWorkingBuffer = 4 - (this.unpackedBytesCounter - (startsAtIndex + Message.HeaderOfMessageCommand.Length));
                                ///Copying those bytes at the end of the unpacked buffer.
                                System.Buffer.BlockCopy(this.unpackedBytes, startsAtIndex + Message.HeaderOfMessageCommand.Length, this.prefixBytes, Message.HeaderOfMessageCommand.Length, this.unpackedBytesCounter - (startsAtIndex + Message.HeaderOfMessageCommand.Length));
                                ///Copying the rest of the needed bytes to convert them into a int32
                                System.Buffer.BlockCopy(this.workingBuffer, 0, this.prefixBytes, Message.HeaderOfMessageCommand.Length + (this.unpackedBytesCounter - (startsAtIndex + Message.HeaderOfMessageCommand.Length)), stolenBytesFromWorkingBuffer);
                                messageBlockSize = System.BitConverter.ToInt32(this.prefixBytes, Message.HeaderOfMessageCommand.Length);
                                physicalMessageBlockSize = endsAtIndex + this.unpackedBytesCounter - startsAtIndex;
                                if (messageBlockSize < int.MaxValue && messageBlockSize == physicalMessageBlockSize)///To avoid bad messages.
                                {
                                    incomingMessage = pooling.BorrowMessage;
                                    System.Buffer.BlockCopy(this.prefixBytes, 0, incomingMessage.bodyMessage, 0, (int)PacketHandler.PrefixSize);
                                    System.Buffer.BlockCopy(this.workingBuffer, stolenBytesFromWorkingBuffer, incomingMessage.bodyMessage, (int)PacketHandler.PrefixSize, endsAtIndex - stolenBytesFromWorkingBuffer);
                                    incomingMessage.MessageBytesSize = (uint)messageBlockSize;
                                    ((RawMessage)incomingMessage).ReallocateCommand();
                                    consumer.ProcessUDPMessage(incomingMessage);
                                    packetStatus = PacketStatus.NoProcessed;
                                }
                            }
                        }
                        else
                        {
                            messageBlockSize = System.BitConverter.ToInt32(this.workingBuffer, startsAtIndex + Message.HeaderOfMessageCommand.Length);
                            physicalMessageBlockSize = endsAtIndex - startsAtIndex;
                            if (messageBlockSize < int.MaxValue && messageBlockSize == physicalMessageBlockSize)///To avoid bad messages.
                            {
                                incomingMessage = pooling.BorrowMessage;
                                if (packetStatus == PacketStatus.BeginHeaderIncomplete)
                                {
                                    System.Buffer.BlockCopy(this.prefixBytes, 0, incomingMessage.bodyMessage, 0, Message.HeaderOfMessageCommand.Length);
                                    System.Buffer.BlockCopy(this.workingBuffer, startsAtIndex + Message.HeaderOfMessageCommand.Length, incomingMessage.bodyMessage, Message.HeaderOfMessageCommand.Length, messageBlockSize - Message.HeaderOfMessageCommand.Length);
                                }
                                else
                                {
                                    //KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo("S: " + startsAtIndex.ToString());
                                    //KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo(messageBlockSize.ToString());
                                    /*
                                    if (messageBlockSize > incomingMessage.bodyMessage.Length)
                                    {
                                        KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo("MessageBlockSize: " + messageBlockSize.ToString());
                                        KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo(startsAtIndex.ToString());
                                    }
                                    if (startsAtIndex > incomingMessage.bodyMessage.Length)
                                    {
                                        KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo("S: " + startsAtIndex.ToString());
                                    }
                                    */
                                    if (messageBlockSize <= incomingMessage.bodyMessage.Length)
                                    {
                                        System.Buffer.BlockCopy(this.workingBuffer, startsAtIndex, incomingMessage.bodyMessage, 0, messageBlockSize);
                                    }
                                    else
                                    {
                                        KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo("CRC:");
                                    }
                                }
                                incomingMessage.MessageBytesSize = (uint)messageBlockSize;
                                ((RawMessage)incomingMessage).ReallocateCommand();
                                consumer.ProcessUDPMessage(incomingMessage);
                                packetStatus = PacketStatus.NoProcessed;
                            }
                            //startsAtIndex = endsAtIndex = 0;
                        }
                        endHeaderFound = startHeaderFound = false;
                        //startsAtIndex = endsAtIndex = 0;
                    }
                }
                this.unpackedBytesCounter = (int)availableBytes - endsAtIndex;
                if (this.unpackedBytesCounter > 0)
                {
                    System.Buffer.BlockCopy(this.workingBuffer, endsAtIndex, this.unpackedBytes, 0, (int)availableBytes - endsAtIndex);
                }
            }
        }

    }
}
