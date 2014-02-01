using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            Handshake,
            RefuseConnection,
            Disconnect,
            StopServer,
            RestartServer
        }

        /// <summary>
        /// Command type
        /// </summary>
        protected CommandType command;

        /// <summary>
        /// The entity who sends the message.
        /// </summary>
        protected NetworkEntity senderEntity;

        /// <summary>
        /// The entity who would recive the message.
        /// </summary>
        protected NetworkEntity recipiententity;

        /// <summary>
        /// Raw message
        /// </summary>
        protected byte[] rawMessage;

        /// <summary>
        /// Constructor, I have to rethink this method.
        /// </summary>
        /// <param name="kindOfMessage"></param>
        /// <param name="sender"></param>
        /// <param name="recipient"></param>
        /// <param name="rawMessage"></param>
        public Message(CommandType kindOfMessage, ref NetworkEntity sender, ref NetworkEntity recipient, ref byte[] rawMessage)
        {
            this.command = kindOfMessage;
            this.senderEntity = sender;
            this.recipiententity = recipient;
            this.rawMessage = new byte[rawMessage.Length];
            Buffer.BlockCopy(rawMessage, 0, this.rawMessage, 0, rawMessage.Length);
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

    }
}
