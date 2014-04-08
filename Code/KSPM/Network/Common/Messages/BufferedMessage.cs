﻿namespace KSPM.Network.Common.Messages
{
    public class BufferedMessage : ManagedMessage
    {
        protected uint startsAt;
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

        public uint Load(byte[] rawBytes, uint rawBytesOffset, uint blockSize)
        {
            this.bodyMessage = rawBytes;
            this.command = (CommandType)this.bodyMessage[Message.HeaderOfMessageCommand.Length + 4];
            this.startsAt = rawBytesOffset;
            this.endsAt = this.startsAt + blockSize;
            this.messageRawLength = blockSize;
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
        }

        public override Message Empty()
        {
            BufferedMessage item = new BufferedMessage(CommandType.Null, 0, 0);
            return item;
        }

        #region Setters/Getters

        public uint StartsAt
        {
            get
            {
                return this.startsAt;
            }
        }

        public uint EndsAt
        {
            get
            {
                return this.endsAt;
            }
        }

        #endregion
    }
}
