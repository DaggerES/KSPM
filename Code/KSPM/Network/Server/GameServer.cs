using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;
using System.Net.Sockets;
using System.Net;

namespace KSPM.Network.Server
{
    public class GameServer
    {
        /// <summary>
        /// Controls the life-cycle of the server, also the thread's life-cyle.
        /// </summary>
        protected bool alive;

        /// <summary>
        /// Controls id the server is set and raeady to run.
        /// </summary>
        protected bool ableToRun;

        #region TCP Variables

        /// <summary>
        /// TCP socket used to receive the connections.
        /// </summary>
        protected Socket tcpSocket;

        /// <summary>
        /// The IP information required to set the TCP socket. IP address and port are required
        /// </summary>
        protected IPEndPoint tcpIpEndPoint;

        #endregion

        /// <summary>
        /// UDP socket used to receive those messages which don't require the confirmation receipt.
        /// </summary>
        protected Socket udpSocket;

        /// <summary>
        /// Settings to operate at low level, like listening ports and the like.
        /// </summary>
        protected ServerSettings lowLevelOperationSettings;

        protected Thread connectionsThread;
        protected Thread commandsThread;
        protected Thread outgoingMessagesThread;
        protected Thread clientThread;
        protected Thread localCommandsThread;

        /// <summary>
        /// Constructor of the server
        /// </summary>
        /// <param name="operationSettings"></param>
        public GameServer(ref ServerSettings operationSettings)///Need to be set the buffer of receiving data
        {
            this.lowLevelOperationSettings = operationSettings;
            if (this.lowLevelOperationSettings == null)
            {
                this.ableToRun = false;
            }
            else
            {
                this.ableToRun = true;
            }
            this.alive = false;
        }

        public bool IsAlive
        {
            get
            {
                return this.alive;
            }
        }

        public bool StartServer()
        {
            if (!this.ableToRun)
                return false;
            this.tcpIpEndPoint = new IPEndPoint(IPAddress.Any, this.lowLevelOperationSettings.tcpPort);
            this.tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.tcpSocket.NoDelay = true;
            try
            {
                this.tcpSocket.Bind(this.tcpIpEndPoint);
            }
            catch (ArgumentException ex)
            {
            }
            catch (SocketException ex)
            {
            }
            catch (ObjectDisposedException ex)
            {
            }
            catch (System.Security.SecurityException ex)
            {
            }
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
