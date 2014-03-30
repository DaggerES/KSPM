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
        protected int unpackedBytesCounter;

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
            //messageTarget.MessageBytesSize = (uint)bytesBlockSize;
            return Error.ErrorType.Ok;
        }

        public static Error.ErrorType Packetize(byte[] rawBuffer, int bytesToRead, System.Collections.Generic.Queue<byte[]> packets)
        {
            if (bytesToRead <= 0)
            {
                bytesToRead = (int)rawBuffer.Length;
            }
            byte[] swap = new byte[bytesToRead];
            try
            {
                System.Buffer.BlockCopy(rawBuffer, 0, swap, 0, bytesToRead);
                byte[] packet;
                int messageBlockSize = -1;
                if (swap.Length < Message.HeaderOfMessageCommand.Length + Message.EndOfMessageCommand.Length)
                    return Error.ErrorType.MessageBadFormat;
                for (int i = 0; i < swap.Length - Message.HeaderOfMessageCommand.Length; )
                {
                    ///locking for the MessageHeaderItself
                    if (swap[i] == Message.HeaderOfMessageCommand[0] && swap[i + 1] == Message.HeaderOfMessageCommand[1] && swap[i + 2] == Message.HeaderOfMessageCommand[2] && swap[i + 3] == Message.HeaderOfMessageCommand[3])
                    {
                        messageBlockSize = System.BitConverter.ToInt32(swap, i + Message.HeaderOfMessageCommand.Length);
                        packet = new byte[messageBlockSize];
                        System.Buffer.BlockCopy(swap, i, packet, 0, messageBlockSize);
                        packets.Enqueue(packet);
                        i += messageBlockSize;
                    }
                    else
                        i++;
                }
            }
            catch (System.Exception ex)
            {
                KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo(ex.Message);
                return Error.ErrorType.ByteBadFormat;
            }
            return Error.ErrorType.Ok;
        }

        public static Error.ErrorType Packetize(System.IO.MemoryStream bytesStream, int bytesToRead, System.Collections.Generic.Queue<byte[]> packets)
        {
            if (bytesToRead <= 0)
            {
                bytesToRead = (int)bytesStream.Length;
            }
            byte[] swap = new byte[bytesToRead];
            try
            {
                bytesStream.Seek(0, System.IO.SeekOrigin.Begin);
                bytesStream.Read(swap, 0, bytesToRead);
                bytesStream.Seek(0, System.IO.SeekOrigin.Begin);
                byte[] packet;
                int messageBlockSize = -1;
                if (swap.Length < Message.HeaderOfMessageCommand.Length + Message.EndOfMessageCommand.Length)
                    return Error.ErrorType.MessageBadFormat;
                for (int i = 0; i < swap.Length - Message.HeaderOfMessageCommand.Length; )
                {
                    ///locking for the MessageHeaderItself
                    if (swap[i] == Message.HeaderOfMessageCommand[0] && swap[i + 1] == Message.HeaderOfMessageCommand[1] && swap[i + 2] == Message.HeaderOfMessageCommand[2] && swap[i + 3] == Message.HeaderOfMessageCommand[3])
                    {
                        messageBlockSize = System.BitConverter.ToInt32(swap, i + Message.HeaderOfMessageCommand.Length);
                        packet = new byte[messageBlockSize];
                        System.Buffer.BlockCopy(swap, i, packet, 0, messageBlockSize);
                        packets.Enqueue(packet);
                        i += messageBlockSize;
                    }
                    else
                        i++;
                }
            }
            catch (System.Exception ex)
            {
                KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo(ex.Message);
                return Error.ErrorType.ByteBadFormat;
            }
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
                messageTarget = new ManagedMessage((Message.CommandType)rawBuffer[8], bytesOwner);
                messageTarget.SetBodyMessageNoClone(rawBuffer, (uint)bytesBlockSize);
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
        /// <param name="owner">NetworkEntity who is owner of the message.</param>
        /// <param name="messageTarget">An out reference to the Message.</param>
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
            int byteCounter;
            messageTarget = null;
            if (rawBytes.Length < 4)
                return Error.ErrorType.MessageBadFormat;
            bytesBlockSize = System.BitConverter.ToInt32(rawBytes, 0);
            if (bytesBlockSize < 4)
                return Error.ErrorType.MessageBadFormat;
            ///Verifying the packet end of message command.
            for (byteCounter = 1; byteCounter <= PacketHandler.RawMessageHeaderSize; byteCounter++)
            {
                if ((rawBytes[bytesBlockSize - byteCounter] & Message.EndOfMessageCommand[Message.EndOfMessageCommand.Length - byteCounter]) != rawBytes[bytesBlockSize - byteCounter])
                {
                    return Error.ErrorType.MessageBadFormat;
                }
            }
            messageTarget = new RawMessage((Message.CommandType)rawBytes[ PacketHandler.RawMessageHeaderSize ], rawBytes, (uint)bytesBlockSize);
            return Error.ErrorType.Ok;
        }

        #endregion

        public PacketHandler( KSPM.IO.Memory.CyclicalMemoryBuffer memoryReference )
        {
            this.memoryReference = memoryReference;
            this.workingBuffer = new byte[this.memoryReference.FixedLength];
            this.unpackedBytes = new byte[this.memoryReference.FixedLength];
            this.unpackedBytesCounter = 0;
        }

        public void Packetize(IPacketArrived consumer)
        {
            uint availableBytes = this.memoryReference.Read(ref this.workingBuffer);
            int messageBlockSize = 0;
            byte[] packet;
            int i = 0;
            if (availableBytes != 0)
            {
                if (this.unpackedBytesCounter + availableBytes >= PacketHandler.PrefixSize)
                {
                    System.Buffer.BlockCopy(this.workingBuffer, 0, this.unpackedBytes, (int)this.unpackedBytesCounter, (int)(PacketHandler.PrefixSize - this.unpackedBytesCounter));
                    if (this.unpackedBytes[i] == Message.HeaderOfMessageCommand[0] && this.unpackedBytes[i + 1] == Message.HeaderOfMessageCommand[1] && this.unpackedBytes[i + 2] == Message.HeaderOfMessageCommand[2] && this.unpackedBytes[i + 3] == Message.HeaderOfMessageCommand[3])
                    {
                        messageBlockSize = System.BitConverter.ToInt32(this.unpackedBytes, Message.HeaderOfMessageCommand.Length);
                        packet = new byte[messageBlockSize];
                        System.Buffer.BlockCopy(this.unpackedBytes, 0, packet, 0, (int)PacketHandler.PrefixSize);
                        if (PacketHandler.PrefixSize < messageBlockSize)
                        {
                            System.Buffer.BlockCopy(this.workingBuffer, (int)PacketHandler.PrefixSize - this.unpackedBytesCounter, packet, (int)PacketHandler.PrefixSize, (int)(messageBlockSize - PacketHandler.PrefixSize));
                        }
                        consumer.ProcessPacket(packet, (uint)packet.Length);
                        i = messageBlockSize - this.unpackedBytesCounter;
                    }
                }
                for (; i < availableBytes - PacketHandler.PrefixSize; )
                {
                    ///locking for the MessageHeaderItself
                    if (this.workingBuffer[i] == Message.HeaderOfMessageCommand[0] && this.workingBuffer[i + 1] == Message.HeaderOfMessageCommand[1] && this.workingBuffer[i + 2] == Message.HeaderOfMessageCommand[2] && this.workingBuffer[i + 3] == Message.HeaderOfMessageCommand[3])
                    {
                        messageBlockSize = System.BitConverter.ToInt32(this.workingBuffer, i + Message.HeaderOfMessageCommand.Length);
                        if (messageBlockSize > availableBytes - i)
                        {
                            break;
                        }
                        else
                        {
                            packet = new byte[messageBlockSize];
                            System.Buffer.BlockCopy(this.workingBuffer, i, packet, 0, messageBlockSize);
                            consumer.ProcessPacket(packet, (uint)packet.Length);
                            i += messageBlockSize;
                        }
                    }
                    else
                        i++;
                }
                this.unpackedBytesCounter = (int)availableBytes - i;
                System.Buffer.BlockCopy(this.workingBuffer, i, this.unpackedBytes, 0, this.unpackedBytesCounter);
            }
        }
    }
}
