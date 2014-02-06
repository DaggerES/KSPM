

namespace KSPM.Network.Common.Packet
{
    public class PacketHandler
    {

        /// <summary>
        /// Size of the raw message header, it is used to send the lenght of sent bytes.
        /// </summary>
        public static readonly uint RawMessageHeaderSize = 4;

        /// <summary>
        /// Creates a Message object from the byte array given, you have to set a proper NetworkEntity because every message created is marked as a Loopback entity, which means that it
        /// should be handle by the server/client itself.
        /// To Do: Gives support to compression methods.
        /// </summary>
        /// <param name="rawBytes">Bytes composing the raw message.</param>
        /// <param name="messageTarget">Message object which should have the result of handling the raw bytes.</param>
        /// <returns></returns>
        public static Error.ErrorType InflateMessage(ref byte[] rawBytes, ref Message messageTarget)
        {
            int bytesBlockSize;
            messageTarget = null;
            if (rawBytes.Length < 4)
                return Error.ErrorType.MessageBadFormat;
            bytesBlockSize = System.BitConverter.ToInt32(rawBytes, 0);
            if (bytesBlockSize != rawBytes.Length)
                return Error.ErrorType.MessageIncompleteBytes;
            messageTarget = new Message((Message.CommandType)rawBytes[4], ref NetworkEntity.LoopbackNetworkEntity );
            return Error.ErrorType.Ok;
        }

        public static Error.ErrorType EncodeMessage(ref NetworkEntity owner, out Message messageTarget)
        {
            messageTarget = new Message((Message.CommandType)owner.rawBuffer[PacketHandler.RawMessageHeaderSize], ref owner);
            return Error.ErrorType.Ok;
        }
    }
}
