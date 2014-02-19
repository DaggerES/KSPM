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
        /// Reference to the compression methods.
        /// </summary>
        protected static Compressor CompressingObject;

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
            messageTarget = null;
            if (bytesOwner.ownerNetworkCollection.secondaryRawBuffer.Length < 4)
                return Error.ErrorType.MessageBadFormat;
            bytesBlockSize = System.BitConverter.ToInt32(bytesOwner.ownerNetworkCollection.secondaryRawBuffer, 0);
            if (bytesBlockSize < 4)
                return Error.ErrorType.MessageBadFormat;
            ///Verifying the packet end of message command.
            for (byteCounter = 1 ; byteCounter <= PacketHandler.RawMessageHeaderSize; byteCounter++)
            {
                if ((bytesOwner.ownerNetworkCollection.secondaryRawBuffer[bytesBlockSize - byteCounter] & Message.EndOfMessageCommand[Message.EndOfMessageCommand.Length - byteCounter]) != bytesOwner.ownerNetworkCollection.secondaryRawBuffer[bytesBlockSize - byteCounter])
                {
                    return Error.ErrorType.MessageBadFormat;
                }
            }
            messageTarget = new ManagedMessage((Message.CommandType)bytesOwner.ownerNetworkCollection.secondaryRawBuffer[4], bytesOwner);
            messageTarget.MessageBytesSize = (uint)bytesBlockSize;
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
    }
}
