using KSPM.Network.Server;
using KSPM.Network.Common.Packet;
using KSPM.Game;

namespace KSPM.Network.Common.Messages
{
    public class RawMessage : Message
    {
        /// <summary>
        /// Flags to tell if this message belongs to a pool.
        /// </summary>
        protected bool pooling;

        /// <summary>
        /// Creates a RawMessage instance, copying the amount of bytes specified by the messageSize parameter into the bodyMessage array.
        /// </summary>
        /// <param name="kindOfMessage">Command type of the message.</param>
        /// <param name="rawBytes">Reference to the byte array.</param>
        /// <param name="messageSize">The amount of usable bytes.</param>
        public RawMessage( CommandType kindOfMessage, byte[] rawBytes, uint messageSize) : base( kindOfMessage )
        {
            this.bodyMessage = rawBytes;
            this.messageRawLength = messageSize;
            this.pooling = false;
            /*
            this.bodyMessage = new byte[rawBytes.Length];
            this.messageRawLength = messageSize;
            System.Buffer.BlockCopy(rawBytes, 0, this.bodyMessage, 0, (int)messageSize);
            */
        }

        /// <summary>
        /// Creates a RawMessage as a buffer setting the pooling flag to True.<b>Be careful about the pooling flag.</b>
        /// </summary>
        public RawMessage() : base(CommandType.Null)
        {
            this.bodyMessage = new byte[ServerSettings.ServerBufferSize];
            this.messageRawLength = 0;
            this.pooling = true;
        }

        /// <summary>
        /// Gets/Sets the pooling flag.
        /// </summary>
        public bool Pooling
        {
            get
            {
                return this.pooling;
            }
            set
            {
                this.pooling = value;
            }
        }

        /// <summary>
        /// Returns an empty RawMessage prepared to be used as a buffer.
        /// </summary>
        /// <returns></returns>
        public override Message Empty()
        {
            return new RawMessage();
        }

        public override void Release()
        {
            this.command = CommandType.Null;
            this.messageRawLength = 0;
            if (!this.pooling)
            {
                this.bodyMessage = null;
            }
        }
    }
}
