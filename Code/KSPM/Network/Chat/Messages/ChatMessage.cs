using System;

namespace KSPM.Network.Chat.Messages
{
    public abstract class ChatMessage
    {
        /// <summary>
        /// Static message counter, this gives an unique autoincremental id to each message.
        /// </summary>
        protected static long MessageCounter = 1;

        /// <summary>
        /// Unique id.
        /// </summary>
        protected long messageId;

        /// <summary>
        /// Timestamp when the message was created.
        /// </summary>
        protected DateTime timeStamp;

        /// <summary>
        /// User´s name who sends the message.
        /// </summary>
        protected string fromUsername;

        /// <summary>
        /// Message coded using UTF8.
        /// </summary>
        protected string body;

        #region Initializing

        /// <summary>
        /// Creates an orphan message intance.
        /// </summary>
        public ChatMessage()
        {
            this.timeStamp = DateTime.Now;
            this.messageId = ChatMessage.MessageCounter++;
            this.body = null;
            this.fromUsername = null;
        }

        public ChatMessage(string fromUsername)
        {
            this.timeStamp = DateTime.Now;
            this.messageId = ChatMessage.MessageCounter++;
            this.fromUsername = fromUsername;
            this.body = null;
        }

        #endregion

        public void SetBody(string body)
        {
            this.body = body;
        }

        #region Getters

        public long MessageId
        {
            get
            {
                return this.messageId;
            }
        }

        public DateTime Time
        {
            get
            {
                return this.timeStamp;
            }
        }

        public string From
        {
            get
            {
                return this.fromUsername;
            }
        }

        public string Body
        {
            get
            {
                return this.body;
            }
        }

        #endregion
    }
}
