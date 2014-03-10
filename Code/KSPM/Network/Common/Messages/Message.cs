
using KSPM.Network.Server;
using KSPM.Network.Common.Packet;
using KSPM.Network.Client;
using KSPM.Game;

namespace KSPM.Network.Common.Messages
{
    public abstract class Message
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

            /// <summary>
            /// Message sent when the server is full and a new client is attempting to connect to the game.
            /// [Header {byte:4}][ Command {byte:1} ][ EndOfMessage {byte:4} ]
            /// </summary>
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

            #region UDPSettingUp
            /// <summary>
            /// Command sent by the server to tell the remote client wich port has been assigned to it also sends the pairing code. Either it works to test the connection.
            /// [Header {byte:4}][ Command {byte:1} ][ PortNumber{byte:4}][ PairingCode {byte:4} ][ EndOfMessage {byte:4} ]
            /// </summary>
            UDPSettingUp,

            /// <summary>
            /// Command send by the remote client to test the UDP connection, and the client establishes the message structure. <b>*It is sent through the UDP socket.*</b>
            /// [Header {byte:4}][ Command {byte:1} ][ PairingNumber{byte:4} ][ EndOfMessage {byte:4} ]
            /// </summary>
            UDPPairing,

            /// <summary>
            /// Command sent by the server to tell the remote client that everything is ok.
            /// [Header {byte:4}][ Command {byte:1} ][ EndOfMessage {byte:4} ]
            /// </summary>
            UDPPairingOk,

            /// <summary>
            /// Command sent by the server to tell the remote client that its message has been received but its pairing code was wrong, anyway the connection works.
            /// [Header {byte:4}][ Command {byte:1} ]
            UDPPairingFail,

            /// <summary>
            /// 
            /// </summary>
            UDPBroadcast,

            #endregion
            /// <summary>
            /// Disconnect command to a nicely way to say goodbye.
            /// [Header {byte:4}][ Command {byte:1} ][ EndOfMessage {byte:4} ]
            /// </summary>
            Disconnect,
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
        /// How many bytes of the buffer are usable, only used when the messages is being sent.
        /// </summary>
        protected uint messageRawLength;

        /// <summary>
        /// Constructor, I have to rethink this method.
        /// </summary>
        /// <param name="kindOfMessage">Command kind</param>
        /// <param name="messageOwner">Network entity who is owner of this message.</param>
        public Message(CommandType kindOfMessage)
        {
            this.command = kindOfMessage;
            this.messageRawLength = 0;
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
        public uint MessageBytesSize
        {
            get
            {
                return this.messageRawLength;
            }
            set
            {
                this.messageRawLength = value;
            }
        }

        public abstract void Release();

        #region AuthenticationCode

        /// <summary>
        /// Writes a handshake message in a raw format into the sender's buffer then creates a Message object. <b>The previous content is discarded.</b>
        /// </summary>
        /// <param name="sender">Reference to sender that holds the buffer to write in.</param>
        /// <param name="targetMessage">Out reference to the Message object to be created.</param>
        /// <returns></returns>
        public static Error.ErrorType HandshakeAccetpMessage( NetworkEntity sender, out Message targetMessage)
        {
            int bytesToSend = (int)PacketHandler.RawMessageHeaderSize;
            targetMessage = null;
            byte [] messageHeaderContent = null;
            if (sender == null)
            {
                return Error.ErrorType.InvalidNetworkEntity;
            }

            ///Writing the command.
            sender.ownerNetworkCollection.rawBuffer[PacketHandler.RawMessageHeaderSize] = (byte)Message.CommandType.Handshake;
            bytesToSend += 1;

            ///Writing the EndOfMessageCommand.
            System.Buffer.BlockCopy(Message.EndOfMessageCommand, 0, sender.ownerNetworkCollection.rawBuffer, bytesToSend, Message.EndOfMessageCommand.Length);
            bytesToSend += EndOfMessageCommand.Length;

            ///Writing the message length.
            messageHeaderContent = System.BitConverter.GetBytes( bytesToSend );
            System.Buffer.BlockCopy( messageHeaderContent, 0, sender.ownerNetworkCollection.rawBuffer, 0, messageHeaderContent.Length );

            targetMessage = new ManagedMessage((CommandType)sender.ownerNetworkCollection.rawBuffer[PacketHandler.RawMessageHeaderSize], sender);
            targetMessage.messageRawLength = (uint)bytesToSend;
            return Error.ErrorType.Ok;
        }

        /// <summary>
        /// Writes a NewUser message in a raw format into the sender's buffer then creates a Message object. <b>The previous content is discarded.</b>
        /// </summary>
        /// <param name="sender">Reference to sender that holds the buffer to write in.</param>
        /// <param name="targetMessage">Out reference to the Message object to be created.</param>
        /// <returns></returns>
        public static Error.ErrorType NewUserMessage(NetworkEntity sender, out Message targetMessage)
        {
            int bytesToSend = (int)PacketHandler.RawMessageHeaderSize;
            targetMessage = null;
            byte[] messageHeaderContent = null;
            if (sender == null)
            {
                return Error.ErrorType.InvalidNetworkEntity;
            }

            ///Writing the command.
            sender.ownerNetworkCollection.rawBuffer[PacketHandler.RawMessageHeaderSize] = (byte)Message.CommandType.NewClient;
            bytesToSend += 1;

            ///Writing the EndOfMessageCommand.
            System.Buffer.BlockCopy(Message.EndOfMessageCommand, 0, sender.ownerNetworkCollection.rawBuffer, bytesToSend, Message.EndOfMessageCommand.Length);
            bytesToSend += EndOfMessageCommand.Length;

            ///Writing the message length.
            messageHeaderContent = System.BitConverter.GetBytes(bytesToSend);
            System.Buffer.BlockCopy(messageHeaderContent, 0, sender.ownerNetworkCollection.rawBuffer, 0, messageHeaderContent.Length);
            targetMessage = new ManagedMessage((CommandType)sender.ownerNetworkCollection.rawBuffer[PacketHandler.RawMessageHeaderSize], sender);
            targetMessage.messageRawLength = (uint)bytesToSend;
            return Error.ErrorType.Ok;
        }


        /// <summary>
        /// Writes a handshake message in a raw format into the sender's buffer then creates a Message object. <b>The previous content is discarded.</b>
        /// </summary>
        /// <param name="sender">Reference to sender that holds the buffer to write in.</param>
        /// <param name="targetMessage">Out reference to the Message object to be created.</param>
        /// <returns></returns>
        public static Error.ErrorType ServerFullMessage(NetworkEntity sender, out Message targetMessage)
        {
            int bytesToSend = (int)PacketHandler.RawMessageHeaderSize;
            targetMessage = null;
            byte[] messageHeaderContent = null;
            if (sender == null)
            {
                return Error.ErrorType.InvalidNetworkEntity;
            }
            sender.ownerNetworkCollection.rawBuffer[PacketHandler.RawMessageHeaderSize] = (byte)Message.CommandType.ServerFull;
            bytesToSend += 1;
            System.Buffer.BlockCopy(Message.EndOfMessageCommand, 0, sender.ownerNetworkCollection.rawBuffer, bytesToSend, Message.EndOfMessageCommand.Length);
            bytesToSend += EndOfMessageCommand.Length;
            messageHeaderContent = System.BitConverter.GetBytes(bytesToSend);
            System.Buffer.BlockCopy(messageHeaderContent, 0, sender.ownerNetworkCollection.rawBuffer, 0, messageHeaderContent.Length);
            targetMessage = new ManagedMessage((CommandType)sender.ownerNetworkCollection.rawBuffer[PacketHandler.RawMessageHeaderSize], sender);
            targetMessage.messageRawLength = (uint)bytesToSend;
            return Error.ErrorType.Ok;
        }

        /// <summary>
        /// Creates an authentication message. **In this moment it is not complete and may change in future updates.**
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="targetMessage"></param>
        /// <returns></returns>
        public static Error.ErrorType AuthenticationMessage(NetworkEntity sender, User userInfo, out Message targetMessage)
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
            sender.ownerNetworkCollection.rawBuffer[PacketHandler.RawMessageHeaderSize] = (byte)Message.CommandType.Authentication;
            bytesToSend += 1;

            ///Writing the username's byte length.
            sender.ownerNetworkCollection.rawBuffer[bytesToSend] = (byte)userBuffer.Length;
            bytesToSend += 1;

            ///Writing the username's bytes
            System.Buffer.BlockCopy(userBuffer, 0, sender.ownerNetworkCollection.rawBuffer, bytesToSend, userBuffer.Length);
            bytesToSend += userBuffer.Length;

            ///Writing the hash's length
            hashSize = (short)userInfo.Hash.Length;
            userBuffer = null;
            userBuffer = System.BitConverter.GetBytes(hashSize);
            System.Buffer.BlockCopy(userBuffer, 0, sender.ownerNetworkCollection.rawBuffer, bytesToSend, userBuffer.Length);
            bytesToSend += userBuffer.Length;

            ///Writing the user's hash code.
            System.Buffer.BlockCopy(userInfo.Hash, 0, sender.ownerNetworkCollection.rawBuffer, bytesToSend, hashSize);
            bytesToSend += hashSize;

            ///Writing the EndOfMessage command.
            System.Buffer.BlockCopy(Message.EndOfMessageCommand, 0, sender.ownerNetworkCollection.rawBuffer, bytesToSend, Message.EndOfMessageCommand.Length);
            bytesToSend += EndOfMessageCommand.Length;
            messageHeaderContent = System.BitConverter.GetBytes(bytesToSend);
            System.Buffer.BlockCopy(messageHeaderContent, 0, sender.ownerNetworkCollection.rawBuffer, 0, messageHeaderContent.Length);
            targetMessage = new ManagedMessage((CommandType)sender.ownerNetworkCollection.rawBuffer[PacketHandler.RawMessageHeaderSize], sender);
            targetMessage.messageRawLength = (uint)bytesToSend;
            return Error.ErrorType.Ok;
        }

        /// <summary>
        /// Writes a AuthenticationFail message in a raw format into the sender's buffer then creates a Message object. <b>The previous content is discarded.</b>
        /// </summary>
        /// <param name="sender">Reference to sender that holds the buffer to write in.</param>
        /// <param name="targetMessage">Out reference to the Message object to be created.</param>
        /// <returns></returns>
        public static Error.ErrorType AuthenticationFailMessage(NetworkEntity sender, out Message targetMessage)
        {
            int bytesToSend = (int)PacketHandler.RawMessageHeaderSize;
            targetMessage = null;
            byte[] messageHeaderContent = null;
            if (sender == null)
            {
                return Error.ErrorType.InvalidNetworkEntity;
            }
            sender.ownerNetworkCollection.rawBuffer[PacketHandler.RawMessageHeaderSize] = (byte)Message.CommandType.AuthenticationFail;
            bytesToSend += 1;
            System.Buffer.BlockCopy(Message.EndOfMessageCommand, 0, sender.ownerNetworkCollection.rawBuffer, bytesToSend, Message.EndOfMessageCommand.Length);
            bytesToSend += EndOfMessageCommand.Length;
            messageHeaderContent = System.BitConverter.GetBytes(bytesToSend);
            System.Buffer.BlockCopy(messageHeaderContent, 0, sender.ownerNetworkCollection.rawBuffer, 0, messageHeaderContent.Length);
            targetMessage = new ManagedMessage((CommandType)sender.ownerNetworkCollection.rawBuffer[PacketHandler.RawMessageHeaderSize], sender);
            targetMessage.messageRawLength = (uint)bytesToSend;
            return Error.ErrorType.Ok;
        }

        /// <summary>
        /// Writes a AuthenticationSuccess message in a raw format into the sender's buffer then creates a Message object. <b>The previous content is discarded.</b>
        /// </summary>
        /// <param name="sender">Reference to sender that holds the buffer to write in.</param>
        /// <param name="targetMessage">Out reference to the Message object to be created.</param>
        /// <returns></returns>
        public static Error.ErrorType AuthenticationSuccessMessage(NetworkEntity sender, out Message targetMessage)
        {
            int bytesToSend = (int)PacketHandler.RawMessageHeaderSize;
            targetMessage = null;
            byte[] messageHeaderContent = null;
            if (sender == null)
            {
                return Error.ErrorType.InvalidNetworkEntity;
            }
            sender.ownerNetworkCollection.rawBuffer[PacketHandler.RawMessageHeaderSize] = (byte)Message.CommandType.AuthenticationSuccess;
            bytesToSend += 1;
            System.Buffer.BlockCopy(Message.EndOfMessageCommand, 0, sender.ownerNetworkCollection.rawBuffer, bytesToSend, Message.EndOfMessageCommand.Length);
            bytesToSend += EndOfMessageCommand.Length;
            messageHeaderContent = System.BitConverter.GetBytes(bytesToSend);
            System.Buffer.BlockCopy(messageHeaderContent, 0, sender.ownerNetworkCollection.rawBuffer, 0, messageHeaderContent.Length);
            targetMessage = new ManagedMessage((CommandType)sender.ownerNetworkCollection.rawBuffer[PacketHandler.RawMessageHeaderSize], sender);
            targetMessage.messageRawLength = (uint)bytesToSend;
            return Error.ErrorType.Ok;
        }

        #endregion

        #region UserInteractionCode
        /// <summary>
        /// Writes a disconnect message into de buffer.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="targetMessage"></param>
        /// <returns></returns>
        public static Error.ErrorType DisconnectMessage(NetworkEntity sender, out Message targetMessage)
        {
            int bytesToSend = (int)PacketHandler.RawMessageHeaderSize;
            targetMessage = null;
            byte[] messageHeaderContent = null;
            if (sender == null)
            {
                return Error.ErrorType.InvalidNetworkEntity;
            }
            sender.ownerNetworkCollection.rawBuffer[PacketHandler.RawMessageHeaderSize] = (byte)Message.CommandType.Disconnect;
            bytesToSend += 1;
            System.Buffer.BlockCopy(Message.EndOfMessageCommand, 0, sender.ownerNetworkCollection.rawBuffer, bytesToSend, Message.EndOfMessageCommand.Length);
            bytesToSend += EndOfMessageCommand.Length;
            messageHeaderContent = System.BitConverter.GetBytes(bytesToSend);
            System.Buffer.BlockCopy(messageHeaderContent, 0, sender.ownerNetworkCollection.rawBuffer, 0, messageHeaderContent.Length);
            targetMessage = new ManagedMessage((CommandType)sender.ownerNetworkCollection.rawBuffer[PacketHandler.RawMessageHeaderSize], sender);
            targetMessage.messageRawLength = (uint)bytesToSend;
            return Error.ErrorType.Ok;
        }

        #endregion

        #region UDPCommands

        public static Error.ErrorType UDPSettingUpMessage(NetworkEntity sender, out Message targetMessage)
        {
            int bytesToSend = (int)PacketHandler.RawMessageHeaderSize;
            int intBuffer;
            ServerSideClient ssClientReference = (ServerSideClient)sender;
            targetMessage = null;
            byte[] messageHeaderContent = null;
            byte[] byteBuffer;
            if (sender == null)
            {
                return Error.ErrorType.InvalidNetworkEntity;
            }

            ///Writing the Command byte.
            sender.ownerNetworkCollection.rawBuffer[PacketHandler.RawMessageHeaderSize] = (byte)Message.CommandType.UDPSettingUp;
            bytesToSend += 1;

            ///Writing the port number.
            intBuffer = ((System.Net.IPEndPoint)ssClientReference.udpCollection.socketReference.LocalEndPoint).Port;
            byteBuffer = System.BitConverter.GetBytes(intBuffer);
            System.Buffer.BlockCopy(byteBuffer, 0, sender.ownerNetworkCollection.rawBuffer, bytesToSend, byteBuffer.Length);
            bytesToSend += byteBuffer.Length;

            ///Writintg the paring code.
            byteBuffer = System.BitConverter.GetBytes(ssClientReference.CreatePairingCode());
            System.Buffer.BlockCopy(byteBuffer, 0, ssClientReference.ownerNetworkCollection.rawBuffer, bytesToSend, byteBuffer.Length);
            bytesToSend += byteBuffer.Length;
            
            ///Writint the EndOfMessageCommand.
            System.Buffer.BlockCopy(Message.EndOfMessageCommand, 0, sender.ownerNetworkCollection.rawBuffer, bytesToSend, Message.EndOfMessageCommand.Length);
            bytesToSend += EndOfMessageCommand.Length;
            messageHeaderContent = System.BitConverter.GetBytes(bytesToSend);
            System.Buffer.BlockCopy(messageHeaderContent, 0, sender.ownerNetworkCollection.rawBuffer, 0, messageHeaderContent.Length);
            targetMessage = new ManagedMessage((CommandType)sender.ownerNetworkCollection.rawBuffer[PacketHandler.RawMessageHeaderSize], sender);
            targetMessage.messageRawLength = (uint)bytesToSend;
            return Error.ErrorType.Ok;
        }

        /// <summary>
        /// Writes an UDPParingMessage message in a raw format into the sender's udp buffer then creates a Message object. <b>The previous content is discarded.</b>
        /// </summary>
        /// <param name="sender">Reference to sender that holds the buffer to write in.</param>
        /// <param name="targetMessage">Out reference to the Message object to be created.</param>
        /// <returns></returns>
        public static Error.ErrorType UDPPairingMessage(NetworkEntity sender, out Message targetMessage)
        {
            int bytesToSend = (int)PacketHandler.RawMessageHeaderSize;
            GameClient gameClientReference = (GameClient)sender;
            targetMessage = null;
            byte[] messageHeaderContent = null;
            byte[] byteBuffer;
            if (sender == null)
            {
                return Error.ErrorType.InvalidNetworkEntity;
            }

            ///Writing the Command byte.
            gameClientReference.udpNetworkCollection.rawBuffer[PacketHandler.RawMessageHeaderSize] = (byte)Message.CommandType.UDPPairing;
            bytesToSend += 1;

            ///Writing the pairing number.
            byteBuffer = System.BitConverter.GetBytes(gameClientReference.PairingCode);
            System.Buffer.BlockCopy(byteBuffer, 0, gameClientReference.udpNetworkCollection.rawBuffer, bytesToSend, byteBuffer.Length);
            bytesToSend += byteBuffer.Length;

            ///Writint the EndOfMessageCommand.
            System.Buffer.BlockCopy(Message.EndOfMessageCommand, 0, gameClientReference.udpNetworkCollection.rawBuffer, bytesToSend, Message.EndOfMessageCommand.Length);
            bytesToSend += EndOfMessageCommand.Length;
            messageHeaderContent = System.BitConverter.GetBytes(bytesToSend);
            System.Buffer.BlockCopy(messageHeaderContent, 0, gameClientReference.udpNetworkCollection.rawBuffer, 0, messageHeaderContent.Length);
            targetMessage = new RawMessage((CommandType)gameClientReference.udpNetworkCollection.rawBuffer[PacketHandler.RawMessageHeaderSize], gameClientReference.udpNetworkCollection.rawBuffer, (uint)bytesToSend);
            return Error.ErrorType.Ok;
        }

        /// <summary>
        /// Writes an UDPParingOkMessage message in a raw format into the sender's udp buffer then creates a Message object. <b>The previous content is discarded.</b>
        /// </summary>
        /// <param name="sender">Reference to sender that holds the buffer to write in.</param>
        /// <param name="targetMessage">Out reference to the Message object to be created.</param>
        /// <returns></returns>
        public static Error.ErrorType UDPPairingOkMessage(NetworkEntity sender, out Message targetMessage)
        {
            int bytesToSend = (int)PacketHandler.RawMessageHeaderSize;
            ServerSideClient ssClientReference = (ServerSideClient)sender;
            targetMessage = null;
            byte[] messageHeaderContent = null;
            if (sender == null)
            {
                return Error.ErrorType.InvalidNetworkEntity;
            }

            ///Writing the Command byte.
            ssClientReference.udpCollection.rawBuffer[PacketHandler.RawMessageHeaderSize] = (byte)Message.CommandType.UDPPairingOk;
            bytesToSend += 1;

            ///Writint the EndOfMessageCommand.
            System.Buffer.BlockCopy(Message.EndOfMessageCommand, 0, ssClientReference.udpCollection.rawBuffer, bytesToSend, Message.EndOfMessageCommand.Length);
            bytesToSend += EndOfMessageCommand.Length;
            messageHeaderContent = System.BitConverter.GetBytes(bytesToSend);
            System.Buffer.BlockCopy(messageHeaderContent, 0, ssClientReference.udpCollection.rawBuffer, 0, messageHeaderContent.Length);
            targetMessage = new RawMessage((CommandType)ssClientReference.udpCollection.rawBuffer[PacketHandler.RawMessageHeaderSize], ssClientReference.udpCollection.rawBuffer, (uint)bytesToSend);
            return Error.ErrorType.Ok;
        }

        /// <summary>
        /// Writes an UDPParingFailMessage message in a raw format into the sender's udp buffer then creates a Message object. <b>The previous content is discarded.</b>
        /// </summary>
        /// <param name="sender">Reference to sender that holds the buffer to write in.</param>
        /// <param name="targetMessage">Out reference to the Message object to be created.</param>
        /// <returns></returns>
        public static Error.ErrorType UDPPairingFailMessage(NetworkEntity sender, out Message targetMessage)
        {
            int bytesToSend = (int)PacketHandler.RawMessageHeaderSize;
            ServerSideClient ssClientReference = (ServerSideClient)sender;
            targetMessage = null;
            byte[] messageHeaderContent = null;
            if (sender == null)
            {
                return Error.ErrorType.InvalidNetworkEntity;
            }

            ///Writing the Command byte.
            ssClientReference.udpCollection.rawBuffer[PacketHandler.RawMessageHeaderSize] = (byte)Message.CommandType.UDPPairingFail;
            bytesToSend += 1;

            ///Writint the EndOfMessageCommand.
            System.Buffer.BlockCopy(Message.EndOfMessageCommand, 0, ssClientReference.udpCollection.rawBuffer, bytesToSend, Message.EndOfMessageCommand.Length);
            bytesToSend += EndOfMessageCommand.Length;
            messageHeaderContent = System.BitConverter.GetBytes(bytesToSend);
            System.Buffer.BlockCopy(messageHeaderContent, 0, ssClientReference.udpCollection.rawBuffer, 0, messageHeaderContent.Length);
            targetMessage = new RawMessage((CommandType)ssClientReference.udpCollection.rawBuffer[PacketHandler.RawMessageHeaderSize], ssClientReference.udpCollection.rawBuffer, (uint)bytesToSend);
            return Error.ErrorType.Ok;
        }

        #endregion
    }
}
