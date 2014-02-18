using KSPM.Network.Server;
using KSPM.Network.Common.Packet;
using KSPM.Game;

namespace KSPM.Network.Common
{
    public abstract class RawMessage
    {
        /// <summary>
        /// 4 bytes to mark the end of the message, is kind of the differential manchester encoding plus 1.
        /// </summary>
        public static readonly byte[] EndOfMessageCommand = new byte[] { 127, 255, 127, 0 };

        /// <summary>
        /// A network entity which is owner of the message.
        /// </summary>
        public NetworkEntity messageOwner;

        /// <summary>
        /// How many bytes of the buffer are usable, only used when the messages is being sent.
        /// </summary>
        public uint messageRawLenght;
    }
}
