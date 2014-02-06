
using KSPM.Network.Server;
using KSPM.Network.Common.Packet;

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
            StopServer,
            RestartServer,
            Handshake,
            NewClient,
            RefuseConnection,
            ServerFull,
            Disconnect,
        }

        /// <summary>
        /// 4 bytes to mark the end of the message, is kind of the differential manchester encoding plus 1.
        /// </summary>
        protected static readonly byte[] EndOfMessageCommand = new byte[] { 127, 255, 127, 0 };

        /// <summary>
        /// Command type
        /// </summary>
        protected CommandType command;

        /// <summary>
        /// A network entity which is owner of the message.
        /// </summary>
        protected NetworkEntity messageOwner;

        /// <summary>
        /// Constructor, I have to rethink this method.
        /// </summary>
        /// <param name="kindOfMessage">Command kind</param>
        /// <param name="messageOwner">Network entity who is owner of this message.</param>
        public Message(CommandType kindOfMessage, ref NetworkEntity messageOwner)
        {
            this.command = kindOfMessage;
            this.messageOwner= messageOwner;
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
        /// Sets a new NetworkEntity owner for this message.
        /// </summary>
        /// <param name="messageOwner"></param>
        public void SetOwnerMessageNetworkEntity(ref NetworkRawEntity messageOwner)
        {
            this.messageOwner = (NetworkEntity)messageOwner;
        }

        /// <summary>
        /// Returnr the current NetworkEntity owner of this message.
        /// </summary>
        public NetworkEntity OwnerNetworkEntity
        {
            get
            {
                return this.messageOwner;
            }
        }

        /// <summary>
        /// Writes a handshake message in a raw format into the sender's buffer, the previous content is discarded.
        /// </summary>
        /// <param name="sender">Reference to sender that holds the buffer to write in.</param>
        /// <returns></returns>
        public static Error.ErrorType HandshakeAccetpMessage(ref NetworkEntity sender)
        {
            int bytesToSend = (int)PacketHandler.RawMessageHeaderSize;
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
            return Error.ErrorType.Ok;
        }
    }
}
