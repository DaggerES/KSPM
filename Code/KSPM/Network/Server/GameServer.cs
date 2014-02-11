using System;
using System.Collections.Generic;

using System.Threading;
using System.Net.Sockets;
using System.Net;

using KSPM.Globals;
using KSPM.Network.Common;
using KSPM.Network.Common.Packet;
using KSPM.Network.Server.UserManagement;
using KSPM.Network.Server.UserManagement.Filters;
using KSPM.Game;

namespace KSPM.Network.Server
{
    public class GameServer : IAsyncSender
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

        /// <summary>
        /// Byte buffer attached to the TCP socket.
        /// </summary>
        protected byte[] tcpBuffer;

        #endregion

        /// <summary>
        /// UDP socket used to receive those messages which don't require the confirmation receipt.
        /// </summary>
        protected Socket udpSocket;

        /// <summary>
        /// Settings to operate at low level, like listening ports and the like.
        /// </summary>
        protected ServerSettings lowLevelOperationSettings;

        #region Threading code

        /// <summary>
        /// Holds the local commands to be processed by de server.
        /// </summary>
        public CommandQueue commandsQueue;
        public CommandQueue outgoingMessagesQueue;

        protected Thread connectionsThread;
        protected Thread commandsThread;
        protected Thread outgoingMessagesThread;
        protected Thread clientThread;
        protected Thread localCommandsThread;

        #endregion

        #region USM

        /// <summary>
        /// Default User Management System (UMS) applied by the server.
        /// </summary>
        UserManagementSystem defaultUserManagementSystem;
        List<ServerSideClient> clientsPoll;

        #endregion

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
                return;
            }

            this.tcpBuffer = new byte[ServerSettings.ServerBufferSize];
            this.commandsQueue = new CommandQueue();
            this.outgoingMessagesQueue = new CommandQueue();

            this.connectionsThread = new Thread(new ThreadStart(this.HandleConnectionsThreadMethod));
            this.commandsThread = new Thread(new ThreadStart(this.HandleCommandsThreadMethod));
            this.outgoingMessagesThread = new Thread(new ThreadStart(this.HandleOutgoingMessagesThreadMethod));
            this.clientThread = null;
            this.localCommandsThread = null;

            this.defaultUserManagementSystem = new LowlevelUserManagmentSystem();
            this.clientsPoll = new List<ServerSideClient>();

            this.ableToRun = true;
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
            KSPMGlobals.Globals.Log.WriteTo("Starting KSPM server...");
            if (!this.ableToRun)
            {
                KSPMGlobals.Globals.Log.WriteTo(Error.ErrorType.ServerUnableToRun.ToString());
                return false;
            }
            this.tcpIpEndPoint = new IPEndPoint(IPAddress.Any, this.lowLevelOperationSettings.tcpPort);
            this.tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.tcpSocket.NoDelay = true;

            this.udpSocket = null;
            try
            {
                this.tcpSocket.Bind(this.tcpIpEndPoint);
                this.alive = true;
                this.connectionsThread.Start();
                this.commandsThread.Start();
                this.outgoingMessagesThread.Start();
            }
            catch (Exception ex)
            {
                ///If there is some exception, the server must shutdown itself and its threads.
                KSPMGlobals.Globals.Log.WriteTo(ex.Message);
                this.alive = false;
            }
            return true;  
        }

        /// <summary>
        /// Handles the incoming connections through a TCP socket.
        /// </summary>
        protected void HandleConnectionsThreadMethod()
        {
            Socket attemptingConnectionSocket = null;
            NetworkEntity newConnectionEntity = null;
            Message incomingMessage = null;
            if (!this.ableToRun)
            {
                KSPMGlobals.Globals.Log.WriteTo(Error.ErrorType.ServerUnableToRun.ToString());
            }
            try
            {
                KSPMGlobals.Globals.Log.WriteTo("-Starting to handle conenctions[ " + this.alive + " ]");
                this.tcpSocket.Listen(this.lowLevelOperationSettings.connectionsBackog);
                while (this.alive)
                {
                    this.tcpSocket.BeginAccept(new AsyncCallback(this.OnAsyncAcceptIncomingConnection), this.tcpSocket);
                    //KSPMGlobals.Globals.Log.WriteTo("Receiving conn");
                    Thread.Sleep(11);
                }
            }
            catch (ThreadAbortException)
            {
                this.tcpSocket.Shutdown(SocketShutdown.Both);
                this.tcpSocket.Close();
                this.alive = false;
            }
            catch (Exception ex)
            {
                KSPMGlobals.Globals.Log.WriteTo(ex.Message);
            }
        }

        /// <summary>
        /// Handles the those commands send by the client through a TCP socket.
        /// </summary>
        protected void HandleCommandsThreadMethod()
        {
            Message messageToProcess = null;
            NetworkEntity messageOwner = null;
            ServerSideClient newClientAttempt = null;
            if (!this.ableToRun)
            {
                KSPMGlobals.Globals.Log.WriteTo(Error.ErrorType.ServerUnableToRun.ToString());
            }
            try
            {
                KSPMGlobals.Globals.Log.WriteTo("-Starting to handle commands[ " + this.alive + " ]");
                while (this.alive)
                {
                    if (!this.commandsQueue.IsEmpty())
                    {
                        this.commandsQueue.DequeueCommandMessage(out messageToProcess);
                        if (messageToProcess != null)
                        {
                            switch (messageToProcess.Command)
                            {
                                case Message.CommandType.NewClient:
                                    messageOwner = messageToProcess.OwnerNetworkEntity;
                                    if (this.defaultUserManagementSystem.Query(ref messageOwner))
                                    {
                                        if (ServerSideClient.CreateFromNetworkEntity(ref messageOwner, out newClientAttempt) == Error.ErrorType.Ok)
                                        {
                                            if (newClientAttempt.StartClient())
                                            {
                                                this.clientsPoll.Add(newClientAttempt);
                                                KSPMGlobals.Globals.Log.WriteTo(this.tcpSocket.Connected.ToString());
                                            }
                                        }
                                    }
                                    break;
                                case Message.CommandType.StopServer:
                                    this.ShutdownServer();
                                    break;
                                case Message.CommandType.Authentication:
                                    string username;
                                    User.DecodeUsernameFromBytes(ref messageToProcess.OwnerNetworkEntity.secondaryRawBuffer, 6, messageToProcess.OwnerNetworkEntity.secondaryRawBuffer[5], out username);
                                    break;
                                case Message.CommandType.Disconnect:/// Need to implement some user search
                                    KSPMGlobals.Globals.Log.WriteTo("Disconnecet command: " + messageToProcess.Command.ToString());
                                    break;
                                case Message.CommandType.Unknown:
                                default:
                                    KSPMGlobals.Globals.Log.WriteTo("Unknown command: " + messageToProcess.Command.ToString());
                                    break;
                            }
                        }
                    }
                    Thread.Sleep(9);
                }
            }
            catch (ThreadAbortException)
            {
                this.alive = false;
            }
        }

        /// <summary>
        /// Handles the TCP socket and the main Queue of messages, uses the a TCP socket to send messages.
        /// </summary>
        protected void HandleOutgoingMessagesThreadMethod()
        {
            Message outgoingMessage = null;
            if (!this.ableToRun)
            {
                KSPMGlobals.Globals.Log.WriteTo(Error.ErrorType.ServerUnableToRun.ToString());
            }
            try
            {
                KSPMGlobals.Globals.Log.WriteTo("-Starting to handle outgoing messages[ " + this.alive + " ]");
                while (this.alive)
                {
                    if (!this.outgoingMessagesQueue.IsEmpty())
                    {
                        this.outgoingMessagesQueue.DequeueCommandMessage(out outgoingMessage);
                        if (outgoingMessage != null)
                        {
                            outgoingMessage.OwnerNetworkEntity.ownerSocket.BeginSend(outgoingMessage.OwnerNetworkEntity.rawBuffer, 0, (int)outgoingMessage.BytesSize, SocketFlags.None, new AsyncCallback(this.AsyncSenderCallback), outgoingMessage.OwnerNetworkEntity);
                        }
                        outgoingMessage = null;
                    }
                    Thread.Sleep(5);
                }
            }
            catch (ThreadAbortException)
            {
                this.alive = false;
            }
        }

        /// <summary>
        /// Handles the commands passed by the UI or the console if is it one implemented.
        /// </summary>
        protected void HandleLocalCommandsThreadMethod()
        {
        }

        public void ShutdownServer()
        {
            KSPMGlobals.Globals.Log.WriteTo("Shuttingdown the KSPM server ...");

            ///*************************Killing threads code
            this.alive = false;
            this.commandsThread.Abort();
            this.connectionsThread.Abort();
            this.outgoingMessagesThread.Abort();

            this.connectionsThread.Join();
            KSPMGlobals.Globals.Log.WriteTo("Killed connectionsThread ...");
            this.commandsThread.Join();
            KSPMGlobals.Globals.Log.WriteTo("Killed localCommandsTread ...");
            this.outgoingMessagesThread.Join();
            KSPMGlobals.Globals.Log.WriteTo("Killed outgoingMessagesTread ...");


            ///*************************Killing TCP socket code
            if (this.tcpSocket.Connected)
            {
                this.tcpSocket.Shutdown(SocketShutdown.Both);
                this.tcpSocket.Close();
            }
            this.tcpBuffer = null;
            this.tcpIpEndPoint = null;

            ///*********************Killing server itself
            this.ableToRun = false;
            this.commandsQueue.Purge(false);
            this.commandsQueue = null;

            KSPMGlobals.Globals.Log.WriteTo("Killing conected clients!!!");

            for (int i = 0; i < this.clientsPoll.Count; i++)
            {
                this.clientsPoll[i].ShutdownClient();
            }
            this.clientsPoll = null;

            KSPMGlobals.Globals.Log.WriteTo("Server KSPM killed!!!");

        }

        /// <summary>
        /// Method called in asynchronously or synchronously  each time that a new connection is attempted.
        /// </summary>
        /// <param name="result"></param>
        protected void OnAsyncAcceptIncomingConnection(IAsyncResult result)
        {
            Socket callingSocket, incomingConnectionSocket;
            NetworkRawEntity newNetworkEntity;
            callingSocket = (Socket)result.AsyncState;
            incomingConnectionSocket = callingSocket.EndAccept(result);
            KSPMGlobals.Globals.Log.WriteTo("Hola!!!");
            newNetworkEntity = new NetworkEntity( ref incomingConnectionSocket );
            incomingConnectionSocket.BeginReceive(newNetworkEntity.secondaryRawBuffer, 0, newNetworkEntity.secondaryRawBuffer.Length, SocketFlags.None, this.ReceiveCallback, newNetworkEntity);
        }

        /// <summary>
        /// Method called each time the socket wants to read data.
        /// </summary>
        /// <param name="result"></param>
        protected void ReceiveCallback(IAsyncResult result)
        {
            int readBytes;
            Message incomingMessage = null;
            NetworkEntity callingEntity = (NetworkEntity)result.AsyncState;
            readBytes = callingEntity.ownerSocket.EndReceive(result);
            if (readBytes > 0)
            {
                if (PacketHandler.DecodeRawPacket(ref callingEntity.secondaryRawBuffer) == Error.ErrorType.Ok)
                {
                    if (PacketHandler.InflateMessage(ref callingEntity, out incomingMessage) == Error.ErrorType.Ok)
                    {
                        incomingMessage.SetOwnerMessageNetworkEntity(ref callingEntity);
                        this.commandsQueue.EnqueueCommandMessage(ref incomingMessage);
                        KSPMGlobals.Globals.Log.WriteTo("First command!!!");
                    }
                }
            }
        }


        public void AsyncSenderCallback(System.IAsyncResult result)
        {
            int sentBytes;
            NetworkEntity net = null;
            Socket callingSocket = null;
            try
            {
                net = (NetworkEntity)result.AsyncState;
                //callingSocket = (Socket)result.AsyncState;
                sentBytes = net.ownerSocket.EndSend(result);
                net.MessageSent();
            }
            catch (System.Exception)
            {
            }
        }
    }
}
