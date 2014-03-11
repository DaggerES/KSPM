using KSPM.Network.Chat.Group;
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
        protected System.DateTime timeStamp;

        /// <summary>
        /// User's name who sends the message.
        /// </summary>
        protected string fromUsername;

        /// <summary>
        /// Tells the user's hash who sends the message.
        /// </summary>
        protected byte[] senderHash;

        /// <summary>
        /// Message coded using UTF8.
        /// </summary>
        protected string body;

        /// <summary>
        /// Tells to which group was sent the message.
        /// </summary>
        public short GroupId;

        #region Initializing

        /// <summary>
        /// Creates an orphan message intance.
        /// </summary>
        public ChatMessage()
        {
            this.timeStamp = System.DateTime.Now;
            this.messageId = ChatMessage.MessageCounter++;
            this.body = null;
            this.fromUsername = null;
        }

        public ChatMessage(byte[] senderHash)
        {
            this.timeStamp = System.DateTime.Now;
            this.messageId = ChatMessage.MessageCounter++;
            this.fromUsername = null;
            this.senderHash = senderHash;
            this.body = null;
        }

        #endregion

        public void SetBody(string body)
        {
            this.body = body;
        }

        /// <summary>
        /// Creates a GeneralChat message, it means a chat to be broadcasted to every member on the group.
        /// </summary>
        /// <param name="sender">Network entity who is sending the message.</param>
        /// <param name="targetGroup">ChatGroup to whom is sent the message.</param>
        /// <param name="bodyMessage">String containing the message.</param>
        /// <param name="targetMessage">Out reference to the message to be sent.</param>
        /// <returns></returns>
        public static Error.ErrorType CreateChatMessage(NetworkEntity sender, ChatGroup targetGroup, string bodyMessage,out Message targetMessage)
        {
            int bytesToSend = (int)PacketHandler.RawMessageHeaderSize;
            GameClient senderClient;
            short shortBuffer;
            targetMessage = null;
            byte[] messageHeaderContent = null;
            byte[] bytesBuffer = null;
            if (sender == null)
            {
                return Error.ErrorType.InvalidNetworkEntity;
            }

            if (targetGroup == null)
            {
                return Error.ErrorType.ChatInvalidGroup;
            }

            senderClient = (GameClient)sender;

            ///Writing the command.
            sender.ownerNetworkCollection.rawBuffer[PacketHandler.RawMessageHeaderSize] = (byte)Message.CommandType.Chat;
            bytesToSend += 1;

            ///Writing the hash's length
            shortBuffer = (short)senderClient.ClientOwner.Hash.Length;
            bytesBuffer = null;
            bytesBuffer = System.BitConverter.GetBytes(shortBuffer);
            System.Buffer.BlockCopy(bytesBuffer, 0, sender.ownerNetworkCollection.rawBuffer, bytesToSend, bytesBuffer.Length);
            bytesToSend += bytesBuffer.Length;

            ///Writing the user's hash code.
            System.Buffer.BlockCopy(senderClient.ClientOwner.Hash, 0, sender.ownerNetworkCollection.rawBuffer, bytesToSend, shortBuffer);
            bytesToSend += shortBuffer;

            ///Writing the groupId.
            bytesBuffer = System.BitConverter.GetBytes(targetGroup.Id);
            System.Buffer.BlockCopy(bytesBuffer, 0, sender.ownerNetworkCollection.rawBuffer, bytesToSend, bytesBuffer.Length);
            bytesToSend += bytesBuffer.Length;

            ///Writing the body message.
            KSPMGlobals.Globals.StringEncoder.GetBytes(bodyMessage, out bytesBuffer);
            shortBuffer = (short)bytesBuffer.Length;
            ///bytesToSend + 2 because we have to left room for the size of the messagebody.
            System.Buffer.BlockCopy(bytesBuffer, 0, sender.ownerNetworkCollection.rawBuffer, bytesToSend + 2, shortBuffer);

            ///Writing the body message's size
            bytesBuffer = System.BitConverter.GetBytes(shortBuffer);
            System.Buffer.BlockCopy(bytesBuffer, 0, sender.ownerNetworkCollection.rawBuffer, bytesToSend, bytesBuffer.Length);
            bytesToSend += shortBuffer + bytesBuffer.Length;


            ///Writing the EndOfMessage command.
            System.Buffer.BlockCopy(Message.EndOfMessageCommand, 0, sender.ownerNetworkCollection.rawBuffer, bytesToSend, Message.EndOfMessageCommand.Length);
            bytesToSend += Message.EndOfMessageCommand.Length;
            messageHeaderContent = System.BitConverter.GetBytes(bytesToSend);
            System.Buffer.BlockCopy(messageHeaderContent, 0, sender.ownerNetworkCollection.rawBuffer, 0, messageHeaderContent.Length);
            targetMessage = new ManagedMessage((Message.CommandType)sender.ownerNetworkCollection.rawBuffer[PacketHandler.RawMessageHeaderSize], sender);
            targetMessage.MessageBytesSize = (uint)bytesToSend;
            return Error.ErrorType.Ok;
        }

        public static Error.ErrorType InflateChatMessage(byte[] rawBytes, out ChatMessage messageTarget)
        {
            int bytesBlockSize;
            short shortBuffer;
            byte[] bytesBuffer = null;
            string stringBuffer = null;
            messageTarget = null;
            if (rawBytes.Length < 4)
                return Error.ErrorType.MessageBadFormat;
            bytesBlockSize = System.BitConverter.ToInt32(rawBytes, 0);
            if (bytesBlockSize < 4)
                return Error.ErrorType.MessageBadFormat;

            ///Getting hash size
            shortBuffer = System.BitConverter.ToInt16(rawBytes, (int)PacketHandler.RawMessageHeaderSize + 1);
            bytesBuffer = new byte[shortBuffer];
            ///Getting hash
            System.Buffer.BlockCopy(rawBytes, (int)PacketHandler.RawMessageHeaderSize + 3, bytesBuffer, 0, shortBuffer);

            messageTarget = new GeneralMessage(bytesBuffer);

            ///Getting GroupId
            shortBuffer = System.BitConverter.ToInt16(rawBytes, (int)PacketHandler.RawMessageHeaderSize + 3 + shortBuffer);
            messageTarget.GroupId = shortBuffer;

            ///Getting the message size
            shortBuffer = System.BitConverter.ToInt16( rawBytes, (int)PacketHandler.RawMessageHeaderSize + 3 + messageTarget.senderHash.Length + 2 );

            ///Getting the body message.
            KSPMGlobals.Globals.StringEncoder.GetString(rawBytes, (int)PacketHandler.RawMessageHeaderSize + 3 + messageTarget.senderHash.Length + 4, shortBuffer, out stringBuffer);
            messageTarget.SetBody(stringBuffer);

            //messageTarget 
            return Error.ErrorType.Ok;
        }

        public abstract void Release();

        #region Getters

        /// <summary>
        /// Gets the message's unique Id.
        /// </summary>
        public long MessageId
        {
            get
            {
                return this.messageId;
            }
        }

        /// <summary>
        /// Gets the message's timestamp when the message was created.
        /// </summary>
        public System.DateTime Time
        {
            get
            {
                return this.timeStamp;
            }
        }

        /// <summary>
        /// Gets the username from whom is coming the message.
        /// </summary>
        public string From
        {
            get
            {
                return this.fromUsername;
            }
        }

        /// <summary>
        /// Gets the message body.<b>This is encoded using the Encoder set on the KSPMGlobals.Encoder.</b>
        /// </summary>
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
