using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;
using System.Net.Sockets;

namespace KSPM.Network.Server
{
    public class GameServer
    {
        /// <summary>
        /// Controls the life-cycle of the server, also the thread's life-cyle.
        /// </summary>
        protected bool alive;

        /// <summary>
        /// TCP socket used to receive the connections.
        /// </summary>
        protected Socket tcpSocket;

        /// <summary>
        /// UDP socket used to receive those messages which don't require the confirmation receipt.
        /// </summary>
        protected Socket udpSocket;

        protected Thread connectionsThread;
        protected Thread commandsThread;
        protected Thread outgoingMessagesThread;
        protected Thread clientThread;
        protected Thread localCommandsThread;

        public bool IsAlive
        {
            get
            {
                return this.alive;
            }
        }

        public bool StartServer()
        {
            return true;
        }

        /// <summary>
        /// Handles the incoming connections through a TCP socket.
        /// </summary>
        protected void HandleConnectionsThreadMethod()
        {
        }

        /// <summary>
        /// Handles the those commands send by the client through a TCP socket.
        /// </summary>
        protected void HandleCommandsThreadMethod()
        {
        }

        /// <summary>
        /// Handles the TCP socket and the main Queue of messages, uses the a TCP socket to send messages.
        /// </summary>
        protected void HandleOutgoingMessagesThreadMethod()
        {
        }

        /// <summary>
        /// Handles the commands passed by the UI or the console if is it one implemented.
        /// </summary>
        protected void HandleLocalCommandsThread()
        {
        }
    }
}
