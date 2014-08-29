namespace KSPM.Network.Common.Messages
{
    public class BufferedMessage : ManagedMessage
    {
        protected uint endsAt;

        public BufferedMessage(CommandType kindOfCommand, uint startsAt, uint endsAt)
            : base(kindOfCommand, null)
        {
            this.startsAt = startsAt;
            this.endsAt = endsAt;
        }

        public uint SetBodyMessageNoClone(byte[] rawBytes, uint rawBytesOffset, uint blockSize )
        {
            this.bodyMessage = rawBytes;
            this.startsAt = rawBytesOffset;
            this.endsAt = this.startsAt + blockSize;
            this.messageRawLength = blockSize;
            return this.messageRawLength;
        }

        /// <summary>
        /// Loads a new content into the message, seting up the message indexes.
        /// </summary>
        /// <param name="rawBytes">Byte array containing the original message.</param>
        /// <param name="rawBytesOffset">Bytes offset from where the message starts.</param>
        /// <param name="blockSize">How many bytes are being used by the message.</param>
        /// <returns></returns>
        public uint Load(byte[] rawBytes, uint rawBytesOffset, uint blockSize)
        {
            this.bodyMessage = rawBytes;
            this.command = (CommandType)this.bodyMessage[Message.HeaderOfMessageCommand.Length + 8];
            this.startsAt = rawBytesOffset;
            this.endsAt = this.startsAt + blockSize;
            this.messageRawLength = blockSize;
            this.Priority = (KSPM.Globals.KSPMSystem.PriorityLevel)Message.CommandPriority((byte)this.command);
            return this.messageRawLength;
        }

        public override void Release()
        {
            this.startsAt = this.endsAt = 0;
            this.messageRawLength = 0;
            this.command = CommandType.Null;
            this.bodyMessage = null;
            this.messageOwner = null;
            if (!this.broadcasted)
            {
                this.bodyMessage = null;
            }
            this.broadcasted = false;
            this.MessageId = 0;
            this.Priority = Globals.KSPMSystem.PriorityLevel.Disposable;
        }

        public override Message Empty()
        {
            BufferedMessage item = new BufferedMessage(CommandType.Null, 0, 0);
            return item;
        }

        #region Setters/Getters        

        /// <summary>
        /// Gets the index position where the message ends inside the byte array.
        /// </summary>
        public uint EndsAt
        {
            get
            {
                return this.endsAt;
            }
        }

        public override void Dispose()
        {
            this.startsAt = this.endsAt = 0;
            this.messageRawLength = 0;
            this.command = CommandType.Null;
            this.bodyMessage = null;
            this.messageOwner = null;
            if (!this.broadcasted)
            {
                this.bodyMessage = null;
            }
            this.broadcasted = false;
            this.MessageId = 0;
            this.Priority = Globals.KSPMSystem.PriorityLevel.Disposable;
        }

        #endregion
    }
}
