using KSPM.Network.Server;
using KSPM.Network.Common.Messages;

namespace KSPM.Network.Common.Messages
{
    /// <summary>
    /// Message designed to be broadcasted through the specified clients.
    /// </summary>
    public class BroadcastMessage : Message
    {
        /// <summary>
        /// Snapshot of network entities to be broadcasted.
        /// Creating a snapshot of the given targets list to avoid race conditions when a NetworkEntity is released and set to Null.
        /// Not using a reference to the list because it can produce IndexOutOfRange exceptions due to the race condition explained above
        /// and to the fact of changing context between threads.
        /// </summary>
        protected System.Collections.Generic.List<NetworkEntity> targets;

        public static short DEFAULT_TARGETSLISTSIZE = 32;

        
        /// <summary>
        /// Creates a new broadcast message reference.
        /// </summary>
        /// <param name="kindOfMessage">Command type of the message.</param>
        /// <param name="targets">NetworkEntities to be broadcasted.</param>
        public BroadcastMessage(CommandType kindOfMessage, System.Collections.Generic.List<NetworkEntity> targets)
            : base(kindOfMessage)
        {
            this.targets = targets;
            this.broadcasted = false;
        }

        /// <summary>
        /// Protected contructor used to create empty messages.<b>DO NOT USE IT IF YOU DO NOT WHAT YOU ARE DOING.</b>
        /// </summary>
        protected BroadcastMessage():base( CommandType.Null)
        {
            this.bodyMessage = new byte[ServerSettings.ServerBufferSize];
            ///Creaing a NetworkEntity list to hold the targets of this message.
            this.targets = new System.Collections.Generic.List<NetworkEntity>(BroadcastMessage.DEFAULT_TARGETSLISTSIZE);

            ///Setting it as false to avoid bad implementations of this message.
            this.broadcasted = false;
        }

        /// <summary>
        /// Copies the src array into the preallocated buffer.
        /// </summary>
        /// <param name="src">Byte array to be copied.</param>
        /// <param name="srcOffset">From which index position is allocate the data inside the src array.</param>
        /// <param name="bytesToCopy">Amount of bytes to be copied.</param>
        public void LoadWith(byte[] src, uint srcOffset, uint bytesToCopy)
        {
            if (src == null)
                return;
            if (bytesToCopy > this.bodyMessage.Length)
                bytesToCopy = (uint)this.bodyMessage.Length;
            System.Buffer.BlockCopy(src, (int)srcOffset, this.bodyMessage, 0, (int)bytesToCopy);
            this.messageRawLength = bytesToCopy;
            this.command = (CommandType)this.bodyMessage[Message.HeaderOfMessageCommand.Length + 8];
            this.Priority = (KSPM.Globals.KSPMSystem.PriorityLevel)Message.CommandPriority((byte)this.command);
        }

        /// <summary>
        /// <b>NOT IMPLEMENTED YET.</b>Creates an Empty BroadcastMessage reference.
        /// </summary>
        /// <returns>An Empty Message reference to be used as broadcast container.</returns>
        public override Message Empty()
        {
            Message messageReference = new BroadcastMessage();
            return messageReference;
        }

        /// <summary>
        /// Releases all the resources used by this message.
        /// </summary>
        public override void Release()
        {
            this.bodyMessage = null;
            this.command = CommandType.Null;
            this.messageRawLength = 0;
            this.broadcasted = false;
            this.targets = null;
            this.MessageId = 0;
            if( this.targets != null)
            {
                this.targets.Clear();
            }
            this.targets = null;
            this.Priority = Globals.KSPMSystem.PriorityLevel.Disposable;
        }

        /// <summary>
        /// Gets the targets list as read-only reference.
        /// </summary>
        public System.Collections.Generic.List<NetworkEntity> Targets
        {
            get
            {
                return this.targets;
            }
        }

        /// <summary>
        /// Sets the current reference to an invalid object, but no harm is performed to the buffer. If you want to free all the resource yous must call Release.
        /// </summary>
        public override void Dispose()
        {
            this.command = CommandType.Null;
            this.messageRawLength = 0;
            this.targets.Clear();
            this.broadcasted = false;
            this.MessageId = 0;
            this.Priority = Globals.KSPMSystem.PriorityLevel.Disposable;
        }
    }
}
