using System;
using System.Text;

namespace KSPM.Network.Chat.Messages
{
    /// <summary>
    /// General chat message used on the system.
    /// </summary>
    public class GeneralMessage : ChatMessage
    {
        /// <summary>
        /// Creates an empty message.
        /// </summary>
        public GeneralMessage()
            : base()
        {
        }

        /// <summary>
        /// Creates an empty message.
        /// </summary>
        /// <param name="sendersHash">hash from whom has sent the message.</param>
        public GeneralMessage(byte[] sendersHash)
            : base(sendersHash)
        {
        }

        /// <summary>
        /// Relases each resource used by the object.
        /// </summary>
        public override void Release()
        {
            this.body = null;
            this.sendersUsername = null;
            this.GroupId = -1;
            this.messageId = -1;
            this.senderHash = null;
        }
    }
}
