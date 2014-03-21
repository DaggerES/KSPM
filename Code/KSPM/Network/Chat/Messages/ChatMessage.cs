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
        public string sendersUsername;

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
            this.sendersUsername = null;
        }

        public ChatMessage(byte[] senderHash)
        {
            this.timeStamp = System.DateTime.Now;
            this.messageId = ChatMessage.MessageCounter++;
            this.sendersUsername = null;
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
            byte[] rawBuffer = new byte[Server.ServerSettings.ServerBufferSize];
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
            rawBuffer[PacketHandler.RawMessageHeaderSize] = (byte)Message.CommandType.Chat;
            bytesToSend += 1;

            ///Writing the hash's length
            shortBuffer = (short)senderClient.ClientOwner.Hash.Length;
            bytesBuffer = null;
            bytesBuffer = System.BitConverter.GetBytes(shortBuffer);
            System.Buffer.BlockCopy(bytesBuffer, 0, rawBuffer, bytesToSend, bytesBuffer.Length);
            bytesToSend += bytesBuffer.Length;

            ///Writing the user's hash code.
            System.Buffer.BlockCopy(senderClient.ClientOwner.Hash, 0, rawBuffer, bytesToSend, shortBuffer);
            bytesToSend += shortBuffer;

            ///Writing the user name length bytesToSend + 2 to make room to the name's length.
            KSPMGlobals.Globals.StringEncoder.GetBytes(((GameClient)sender).ClientOwner.Username, out bytesBuffer);
            shortBuffer = (short)bytesBuffer.Length;
            System.Buffer.BlockCopy(bytesBuffer, 0, rawBuffer, bytesToSend + 2, bytesBuffer.Length);

            bytesBuffer = System.BitConverter.GetBytes(shortBuffer);
            System.Buffer.BlockCopy(bytesBuffer, 0, rawBuffer, bytesToSend, bytesBuffer.Length);
            bytesToSend += bytesBuffer.Length + shortBuffer;

            ///Writing the groupId.
            bytesBuffer = System.BitConverter.GetBytes(targetGroup.Id);
            System.Buffer.BlockCopy(bytesBuffer, 0, rawBuffer, bytesToSend, bytesBuffer.Length);
            bytesToSend += bytesBuffer.Length;

            ///Writing the body message.
            KSPMGlobals.Globals.StringEncoder.GetBytes(bodyMessage, out bytesBuffer);
            shortBuffer = (short)bytesBuffer.Length;
            ///bytesToSend + 2 because we have to left room for the size of the messagebody.
            System.Buffer.BlockCopy(bytesBuffer, 0, rawBuffer, bytesToSend + 2, shortBuffer);

            ///Writing the body message's size
            bytesBuffer = System.BitConverter.GetBytes(shortBuffer);
            System.Buffer.BlockCopy(bytesBuffer, 0, rawBuffer, bytesToSend, bytesBuffer.Length);
            bytesToSend += shortBuffer + bytesBuffer.Length;


            ///Writing the EndOfMessage command.
            System.Buffer.BlockCopy(Message.EndOfMessageCommand, 0, rawBuffer, bytesToSend, Message.EndOfMessageCommand.Length);
            bytesToSend += Message.EndOfMessageCommand.Length;
            messageHeaderContent = System.BitConverter.GetBytes(bytesToSend);
            System.Buffer.BlockCopy(messageHeaderContent, 0, rawBuffer, 0, messageHeaderContent.Length);
            targetMessage = new ManagedMessage((Message.CommandType)rawBuffer[PacketHandler.RawMessageHeaderSize], sender);
            targetMessage.SetBodyMessage(rawBuffer, (uint)bytesToSend);
            //targetMessage.MessageBytesSize = (uint)bytesToSend;
            return Error.ErrorType.Ok;
        }

        public static Error.ErrorType InflateChatMessage(byte[] rawBytes, out ChatMessage messageTarget)
        {
            int bytesBlockSize;
            int readingIndex = (int)PacketHandler.RawMessageHeaderSize + 1;
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
            shortBuffer = System.BitConverter.ToInt16(rawBytes, readingIndex);
            bytesBuffer = new byte[shortBuffer];
            readingIndex += 2;
            ///Getting hash
            System.Buffer.BlockCopy(rawBytes, readingIndex, bytesBuffer, 0, shortBuffer);
            readingIndex+= shortBuffer;

            ///Creating the chat message.
            messageTarget = new GeneralMessage(bytesBuffer);

            ///Getting sender's usename lenght
            shortBuffer = System.BitConverter.ToInt16(rawBytes, readingIndex);
            readingIndex += 2;

            ///Getting sender's username
            KSPMGlobals.Globals.StringEncoder.GetString(rawBytes, readingIndex, shortBuffer, out messageTarget.sendersUsername);
            readingIndex += shortBuffer;

            ///Getting GroupId
            shortBuffer = System.BitConverter.ToInt16(rawBytes, readingIndex);
            readingIndex += 2;
            messageTarget.GroupId = shortBuffer;

            ///Getting the message size
            shortBuffer = System.BitConverter.ToInt16( rawBytes, readingIndex );
            readingIndex += 2;

            ///Getting the body message.
            KSPMGlobals.Globals.StringEncoder.GetString(rawBytes, readingIndex, shortBuffer, out stringBuffer);
            readingIndex += shortBuffer;
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
