using KSPM.IO.Compression;

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
        /// Creates a Message object from the byte array stored by the given NetworkEntity, the NetworkEntity reference is set as the owner of the messageTarget.
        /// </summary>
        /// <param name="bytesOwner">Reference to the NetworkEntity who holds the raw bytes, this reference is set as the message owner ether.</param>
        /// <param name="messageTarget">Message object which should have the result of handling the raw bytes.</param>
        /// <returns></returns>
        public static Error.ErrorType InflateMessage(ref NetworkEntity bytesOwner, out Message messageTarget)
        {
            int bytesBlockSize;
            int byteCounter;
            messageTarget = null;
            if (bytesOwner.secondaryRawBuffer.Length < 4)
                return Error.ErrorType.MessageBadFormat;
            bytesBlockSize = System.BitConverter.ToInt32(bytesOwner.secondaryRawBuffer, 0);
            if (bytesBlockSize < 4)
                return Error.ErrorType.MessageBadFormat;
            ///Verifying the packet end of message command.
            for (byteCounter = 1 ; byteCounter <= PacketHandler.RawMessageHeaderSize; byteCounter++)
            {
                if( ( bytesOwner.secondaryRawBuffer[ bytesBlockSize - byteCounter ] & Message.EndOfMessageCommand[ Message.EndOfMessageCommand.Length - byteCounter ] ) != bytesOwner.secondaryRawBuffer[ bytesBlockSize - byteCounter ] )
                {
                    return Error.ErrorType.MessageBadFormat;
                }
            }
            /*
            if (bytesBlockSize != bytesOwner.Length)
                return Error.ErrorType.MessageIncompleteBytes;
            */
            messageTarget = new Message((Message.CommandType)bytesOwner.secondaryRawBuffer[4], ref bytesOwner);
            messageTarget.BytesSize = (uint)bytesBlockSize;
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
        public static Error.ErrorType EncodeRawPacket(ref NetworkEntity owner)
        {
            byte [] compressedBytes = null;
            Message.CommandType command;
            if (owner == null)
                return Error.ErrorType.InvalidNetworkEntity;
            command = (Message.CommandType)owner.rawBuffer[PacketHandler.RawMessageHeaderSize];
            if (PacketHandler.CompressingPacketsEnabled)
            {
                PacketHandler.CompressingObject.Compress(ref owner.rawBuffer, out compressedBytes);
                System.Buffer.BlockCopy(compressedBytes, 0, owner.rawBuffer, 0, owner.rawBuffer.Length);
            }
            return Error.ErrorType.Ok;
        }
    }
}
