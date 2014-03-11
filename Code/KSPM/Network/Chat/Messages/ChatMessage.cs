using System;
using KSPM.Network.Common;
using KSPM.Network.Common.Messages;
using KSPM.Network.Common.Packet;
using KSPM.Network.Client;
using KSPM.Globals;

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

        public static Error.ErrorType CreateChatMessage(NetworkEntity sender, string toUsername, string bodyMessage,out Message targetMessage)
        {
            int bytesToSend = (int)PacketHandler.RawMessageHeaderSize;
            GameClient senderClient;
            short hashSize;
            targetMessage = null;
            byte[] messageHeaderContent = null;
            byte[] userBuffer = null;
            string stringBuffer;
            if (sender == null)
            {
                return Error.ErrorType.InvalidNetworkEntity;
            }

            senderClient = (GameClient)sender;

            ///Writing the command.
            sender.ownerNetworkCollection.rawBuffer[PacketHandler.RawMessageHeaderSize] = (byte)Message.CommandType.Chat;
            bytesToSend += 1;

            ///Writing the hash's length
            hashSize = (short)senderClient.ClientOwner.Hash.Length;
            userBuffer = null;
            userBuffer = System.BitConverter.GetBytes(hashSize);
            System.Buffer.BlockCopy(userBuffer, 0, sender.ownerNetworkCollection.rawBuffer, bytesToSend, userBuffer.Length);
            bytesToSend += userBuffer.Length;

            ///Writing the user's hash code.
            System.Buffer.BlockCopy(senderClient.ClientOwner.Hash, 0, sender.ownerNetworkCollection.rawBuffer, bytesToSend, hashSize);
            bytesToSend += hashSize;


            ///Writing the username's byte length.
            KSPMGlobals.Globals.StringEncoder.GetBytes(toUsername, out userBuffer);
            sender.ownerNetworkCollection.rawBuffer[bytesToSend] = (byte)userBuffer.Length;
            bytesToSend += 1;

            ///Writing the username's bytes
            System.Buffer.BlockCopy(userBuffer, 0, sender.ownerNetworkCollection.rawBuffer, bytesToSend, userBuffer.Length);
            bytesToSend += userBuffer.Length;


            ///Writing the EndOfMessage command.
            System.Buffer.BlockCopy(Message.EndOfMessageCommand, 0, sender.ownerNetworkCollection.rawBuffer, bytesToSend, Message.EndOfMessageCommand.Length);
            bytesToSend += EndOfMessageCommand.Length;
            messageHeaderContent = System.BitConverter.GetBytes(bytesToSend);
            System.Buffer.BlockCopy(messageHeaderContent, 0, sender.ownerNetworkCollection.rawBuffer, 0, messageHeaderContent.Length);
            targetMessage = new ManagedMessage((CommandType)sender.ownerNetworkCollection.rawBuffer[PacketHandler.RawMessageHeaderSize], sender);
            targetMessage.messageRawLength = (uint)bytesToSend;
            return Error.ErrorType.Ok;
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
