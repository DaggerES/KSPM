
using KSPM.Network.Server;

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
    }
}
