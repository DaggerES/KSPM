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
        /// Copies the src array into the preallocated buffer.
        /// </summary>
        /// <param name="src">Byte array to be copied.</param>
        /// <param name="offset">From which index position is allocate the data inside the src array.</param>
        /// <param name="bytesToCopy">Amount of bytes to be copied.</param>
        public void LoadWith(byte[] src, uint srcOffset, uint bytesToCopy)
        {
            if (src == null)
                return;
            if (bytesToCopy > this.bodyMessage.Length)
                bytesToCopy = (uint)this.bodyMessage.Length;
            System.Buffer.BlockCopy(src, (int)srcOffset, this.bodyMessage, 0, (int)bytesToCopy);
            this.messageRawLength = bytesToCopy;
            this.command = (CommandType)this.bodyMessage[Message.HeaderOfMessageCommand.Length + 4];
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

        /// <summary>
        /// Releases all the properties and frees the memory buffers.
        /// </summary>
        public override void Release()
        {
            this.command = CommandType.Null;
            this.messageRawLength = 0;
            this.bodyMessage = null;
            //KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo("Releasing");
        }

        /// <summary>
        /// Sets the current reference to an invalid object, but no harm is performed to the buffer. If you want to free all the resource yous must call
        /// Release.
        /// </summary>
        public override void Dispose()
        {
            this.command = CommandType.Null;
            this.messageRawLength = 0;
        }
    }
}
