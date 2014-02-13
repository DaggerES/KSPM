
using KSPM.Network.Server;
using KSPM.Network.Common.Packet;
using KSPM.Game;

namespace KSPM.Network.Common
{
    public class Message
    {
        /// <summary>
        /// An enum representing what kind of commands could be handled by the server and the client.
        /// </summary>
        public enum CommandType : byte
        {
            Null = 0,
            Unknown,
            #region ServerCommands
            StopServer,
            RestartServer,
            #endregion

            #region AuthenticationCommands
            /// <summary>
            /// Handshake command used to begin a connection between the server and the client.
            /// [Header {byte:4}][ Command {byte:1} ][ EndOfMessage {byte:4} ]
            /// </summary>
            Handshake,

            /// <summary>
            /// NewClient command used by the client to try to stablish a connection with the server.
            /// [Header {byte:4}][ Command {byte:1} ][ EndOfMessage {byte:4} ]
            /// </summary>
            NewClient,
            RefuseConnection,
            ServerFull,

            /// <summary>
            /// Command used by the client to send its authentication information.
            /// [Header {byte:4}][ Command {byte:1} ][ UsernameLenght {byte:1}] [ Username {byte:1-} ][HashLength{2}][ HashedUsernameAndPassword {byte:1-} ][ EndOfMessage {byte:4} ]
            /// </summary>
            Authentication,

            /// <summary>
            /// Command to tells that something went wrong while the authentication process.
            /// [Header {byte:4}][ Command {byte:1} ][ EndOfMessage {byte:4} ]
            /// </summary>
            AuthenticationFail,

            /// <summary>
            /// Command to tells that the access is granted.
            /// [Header {byte:4}][ Command {byte:1} ][ EndOfMessage {byte:4} ]
            /// </summary>
            AuthenticationSuccess,
            #endregion

            #region UserInteractionCommands

            /// <summary>
            /// Disconnect command to a nicely way to say goodbye.
            /// [Header {byte:4}][ Command {byte:1} ][ EndOfMessage {byte:4} ]
            /// </summary>
            Disconnect
            #endregion
        }

        /// <summary>
        /// 4 bytes to mark the end of the message, is kind of the differential manchester encoding plus 1.
        /// </summary>
        public static readonly byte[] EndOfMessageCommand = new byte[] { 127, 255, 127, 0 };

        /// <summary>
        /// Command type
        /// </summary>
        protected CommandType command;

        /// <summary>
        /// A network entity which is owner of the message.
        /// </summary>
        protected NetworkEntity messageOwner;

        /// <summary>
        /// How many bytes of the buffer are usable, only used when the messages is being sent.
        /// </summary>
        protected uint messageRawLenght;

        /// <summary>
        /// Constructor, I have to rethink this method.
        /// </summary>
        /// <param name="kindOfMessage">Command kind</param>
        /// <param name="messageOwner">Network entity who is owner of this message.</param>
        public Message(CommandType kindOfMessage, ref NetworkEntity messageOwner)
        {
            this.command = kindOfMessage;
            this.messageOwner= messageOwner;
            this.messageRawLenght = 0;
        }

        /// <summary>
        /// Gets the command type of this message.
        /// </summary>
        public CommandType Command
        {
            get
            {
                return this.command;
            }
        }

        /// <summary>
        /// Gets or sets the amount of usable bytes inside the buffer and that amount of bytes are going to be sent.
        /// Use this property instead of the ServerSettings.ServerBufferSize property.
        /// </summary>
        public uint BytesSize
        {
            get
            {
                return this.messageRawLenght;
            }
            set
            {
                this.messageRawLenght = value;
            }
        }

        /// <summary>
        /// Sets a new NetworkEntity owner for this message.
        /// </summary>
        /// <param name="messageOwner"></param>
        public void SetOwnerMessageNetworkEntity(ref NetworkEntity messageOwner)
        {
            this.messageOwner = messageOwner;
        }

        /// <summary>
        /// Return the current NetworkEntity owner of this message.
        /// </summary>
        public NetworkEntity OwnerNetworkEntity
        {
            get
            {
                return this.messageOwner;
            }
        }

        /// <summary>
        /// Writes a handshake message in a raw format into the sender's buffer then creates a Message object. <b>The previous content is discarded.</b>
        /// </summary>
        /// <param name="sender">Reference to sender that holds the buffer to write in.</param>
        /// <param name="targetMessage">Out reference to the Message object to be created.</param>
        /// <returns></returns>
        public static Error.ErrorType HandshakeAccetpMessage(ref NetworkEntity sender, out Message targetMessage)
        {
            int bytesToSend = (int)PacketHandler.RawMessageHeaderSize;
            targetMessage = null;
            byte [] messageHeaderContent = null;
            if (sender == null)
            {
                return Error.ErrorType.InvalidNetworkEntity;
            }
            sender.rawBuffer[PacketHandler.RawMessageHeaderSize] = (byte)Message.CommandType.Handshake;
            bytesToSend += 1;
            System.Buffer.BlockCopy(Message.EndOfMessageCommand, 0, sender.rawBuffer, bytesToSend, Message.EndOfMessageCommand.Length);
            bytesToSend += EndOfMessageCommand.Length;
            messageHeaderContent = System.BitConverter.GetBytes( bytesToSend );
            System.Buffer.BlockCopy( messageHeaderContent, 0, sender.rawBuffer, 0, messageHeaderContent.Length );
            targetMessage = new Message((CommandType)sender.rawBuffer[PacketHandler.RawMessageHeaderSize], ref sender);
            targetMessage.BytesSize = (uint)bytesToSend;
            return Error.ErrorType.Ok;
        }

        /// <summary>
        /// Writes a NewUser message in a raw format into the sender's buffer then creates a Message object. <b>The previous content is discarded.</b>
        /// </summary>
        /// <param name="sender">Reference to sender that holds the buffer to write in.</param>
        /// <param name="targetMessage">Out reference to the Message object to be created.</param>
        /// <returns></returns>
        public static Error.ErrorType NewUserMessage(ref NetworkEntity sender, out Message targetMessage)
        {
            int bytesToSend = (int)PacketHandler.RawMessageHeaderSize;
            targetMessage = null;
            byte[] messageHeaderContent = null;
            if (sender == null)
            {
                return Error.ErrorType.InvalidNetworkEntity;
            }
            sender.rawBuffer[PacketHandler.RawMessageHeaderSize] = (byte)Message.CommandType.NewClient;
            bytesToSend += 1;
            System.Buffer.BlockCopy(Message.EndOfMessageCommand, 0, sender.rawBuffer, bytesToSend, Message.EndOfMessageCommand.Length);
            bytesToSend += EndOfMessageCommand.Length;
            messageHeaderContent = System.BitConverter.GetBytes(bytesToSend);
            System.Buffer.BlockCopy(messageHeaderContent, 0, sender.rawBuffer, 0, messageHeaderContent.Length);
            targetMessage = new Message((CommandType)sender.rawBuffer[PacketHandler.RawMessageHeaderSize], ref sender);
            targetMessage.BytesSize = (uint)bytesToSend;
            return Error.ErrorType.Ok;
        }

        /// <summary>
        /// Writes a disconnect message into de buffer.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="targetMessage"></param>
        /// <returns></returns>
        public static Error.ErrorType DisconnectMessage(ref NetworkEntity sender, out Message targetMessage)
        {
            int bytesToSend = (int)PacketHandler.RawMessageHeaderSize;
            targetMessage = null;
            byte[] messageHeaderContent = null;
            if (sender == null)
            {
                return Error.ErrorType.InvalidNetworkEntity;
            }
            sender.rawBuffer[PacketHandler.RawMessageHeaderSize] = (byte)Message.CommandType.Disconnect;
            bytesToSend += 1;
            System.Buffer.BlockCopy(Message.EndOfMessageCommand, 0, sender.rawBuffer, bytesToSend, Message.EndOfMessageCommand.Length);
            bytesToSend += EndOfMessageCommand.Length;
            messageHeaderContent = System.BitConverter.GetBytes(bytesToSend);
            System.Buffer.BlockCopy(messageHeaderContent, 0, sender.rawBuffer, 0, messageHeaderContent.Length);
            targetMessage = new Message((CommandType)sender.rawBuffer[PacketHandler.RawMessageHeaderSize], ref sender);
            targetMessage.BytesSize = (uint)bytesToSend;
            return Error.ErrorType.Ok;
        }

        /// <summary>
        /// Creates an authentication message. **In this moment it is not complete and may change in future updates.**
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="targetMessage"></param>
        /// <returns></returns>
        public static Error.ErrorType AuthenticationMessage(ref NetworkEntity sender, ref User userInfo, out Message targetMessage)
        {
            int bytesToSend = (int)PacketHandler.RawMessageHeaderSize;
            short hashSize;
            targetMessage = null;
            byte[] messageHeaderContent = null;
            byte[] userBuffer = null;
            string stringBuffer;
            if (sender == null)
            {
                return Error.ErrorType.InvalidNetworkEntity;
            }

            stringBuffer = userInfo.Username;
            User.EncodeUsernameToBytes(ref stringBuffer, out userBuffer);

            ///Writing the command.
            sender.rawBuffer[PacketHandler.RawMessageHeaderSize] = (byte)Message.CommandType.Authentication;
            bytesToSend += 1;

            ///Writing the username's byte length.
            sender.rawBuffer[bytesToSend] = (byte)userBuffer.Length;
            bytesToSend += 1;

            ///Writing the username's bytes
            System.Buffer.BlockCopy(userBuffer, 0, sender.rawBuffer, bytesToSend, userBuffer.Length);
            bytesToSend += userBuffer.Length;

            ///Writing the hash's length
            hashSize = (short)userInfo.Hash.Length;
            userBuffer = null;
            userBuffer = System.BitConverter.GetBytes( hashSize );
            System.Buffer.BlockCopy(userBuffer, 0, sender.rawBuffer, bytesToSend, userBuffer.Length);
            bytesToSend += userBuffer.Length;

            ///Writing the user's hash code.
            System.Buffer.BlockCopy(userInfo.Hash, 0, sender.rawBuffer, bytesToSend, hashSize);
            bytesToSend += hashSize;
            
            ///Writing the EndOfMessage command.
            System.Buffer.BlockCopy(Message.EndOfMessageCommand, 0, sender.rawBuffer, bytesToSend, Message.EndOfMessageCommand.Length);
            bytesToSend += EndOfMessageCommand.Length;
            messageHeaderContent = System.BitConverter.GetBytes(bytesToSend);
            System.Buffer.BlockCopy(messageHeaderContent, 0, sender.rawBuffer, 0, messageHeaderContent.Length);
            targetMessage = new Message((CommandType)sender.rawBuffer[PacketHandler.RawMessageHeaderSize], ref sender);
            targetMessage.BytesSize = (uint)bytesToSend;
            return Error.ErrorType.Ok;
        }


    }
}
