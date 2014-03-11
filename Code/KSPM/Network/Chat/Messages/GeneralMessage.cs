using System;
using System.Text;

namespace KSPM.Network.Chat.Messages
{
    public class GeneralMessage : ChatMessage
    {
        public GeneralMessage()
            : base()
        {
        }

        public GeneralMessage(byte[] sendersHash)
            : base(sendersHash)
        {
        }

        public override void Release()
        {
            this.body = null;
            this.fromUsername = null;
            this.GroupId = -1;
            this.messageId = -1;
            this.senderHash = null;
        }
    }
}
