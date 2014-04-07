namespace KSPM.Network.Common.Messages
{
    public class BufferedMessage : Message
    {
        protected uint startsAt;
        protected uint endsAt;

        public BufferedMessage(CommandType kindOfCommand, uint startsAt, uint endsAt)
            : base(kindOfCommand)
        {
            this.startsAt = startsAt;
            this.endsAt = endsAt;
            this.messageRawLength = this.endsAt - this.startsAt;
            this.bodyMessage = null;
        }

        public override void Release()
        {
            this.startsAt = this.endsAt = 0;
            this.messageRawLength = 0;
            this.command = CommandType.Null;
            this.bodyMessage = null;
        }
    }
}
