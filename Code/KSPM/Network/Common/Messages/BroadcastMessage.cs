using KSPM.Network.Common.Messages;

namespace KSPM.Network.Common.Messages
{
    public class BroadcastMessage : Message
    {
        /// <summary>
        /// Snapshot of network entities to be broadcasted.
        /// Creating a snapshot of the given targets list to avoid race conditions when a NetworkEntity is released and set to Null.
        /// Not using a reference to the list because it can produce IndexOutOfRange exceptions due to the race condition explaided above
        /// and to the fact of changing context between threads.
        /// </summary>
        protected NetworkEntity [] targets;

        public BroadcastMessage(CommandType kindOfMessage, System.Collections.Generic.List<NetworkEntity> targets)
            : base(kindOfMessage)
        {
            this.targets = targets.ToArray();
            this.broadcasted = true;
        }

        public override Message Empty()
        {
            throw new System.NotImplementedException();
        }

        public override void Release()
        {
            this.bodyMessage = null;
            this.command = CommandType.Null;
            this.messageRawLength = 0;
            this.targets = null;
            this.broadcasted = false;
            this.targets = null;
        }

        /// <summary>
        /// Gets the targets list.
        /// </summary>
        public NetworkEntity[] Targets
        {
            get
            {
                return this.targets;
            }
        }
    }
}
