using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using KSPM.Network.Server;

namespace KSPM.Network.Common
{
    class Message
    {
        /// <summary>
        /// An enum representing what kind of commands could be handled by the server and the client.
        /// </summary>
        public enum CommandType : byte
        {
            Null = 0,
            Handshake,
            RefuseConnection,
            Disconnect
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

    }
}
