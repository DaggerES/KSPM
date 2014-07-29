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
            this.PriorityGroup = Message.CommandPriority((byte)this.command);
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
        }

        public override Message Empty()
        {
            BufferedMessage item = new BufferedMessage(CommandType.Null, 0, 0);
            return item;
        }

        #region Setters/Getters        

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
        }

        #endregion
    }
}
