using System;
using System.Collections.Generic;

using System.Threading;
using System.Net.Sockets;
using System.Net;

using KSPM.Globals;
using KSPM.Network.Common;
using KSPM.Network.Common.Packet;
using KSPM.Network.Common.Messages;
using KSPM.Network.Common.Events;
using KSPM.Network.Server.UserManagement;
using KSPM.Network.Server.UserManagement.Filters;
using KSPM.Game;

using KSPM.Diagnostics;

using KSPM.Network.Chat.Managers;
using KSPM.Network.Chat.Group;
using KSPM.Network.Chat.Messages;

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
        /// Settings to operate at low level, like listening ports and the like.
        /// </summary>
        protected ServerSettings lowLevelOperationSettings;

        #region Threading code

        /// <summary>
        /// Holds the local commands to be processed by de server.
        /// </summary>
        public CommandQueue commandsQueue;
        public CommandQueue outgoingMessagesQueue;
        public CommandQueue localCommandsQueue;
        public CommandQueue priorityOutgoingMessagesQueue;

        protected Thread connectionsThread;
        protected Thread commandsThread;
        protected Thread outgoingMessagesThread;
        protected Thread localCommandsThread;
        protected Thread priorityOutgoingMessagesThread;

        /// <summary>
        /// ManualResetEvent reference to manage the signaling among the threads and the async methods.
        /// </summary>
        protected static readonly ManualResetEvent SignalHandler = new ManualResetEvent(false);

        #endregion

        #region USM

        /// <summary>
        /// Default User Management System (UMS) applied by the server.
        /// </summary>
        protected UserManagementSystem defaultUserManagementSystem;

        /// <summary>
        /// Provides a basic authentication.
        /// </summary>
        protected AccountManager usersAccountManager;

        /// <summary>
        /// Poll of clients connected to the server, it should be used when the broadcasting is required.
        /// </summary>
        protected ClientsHandler clientsHandler;

        public event UserConnectedEventHandler UserConnected;

        public event UserDisconnectedEventHandler UserDisconnected;

        #endregion

        #region Chat

        /// <summary>
        /// Handles the KSPM Chat system.
        /// </summary>
        public ChatManager chatManager;

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
            this.localCommandsQueue = new CommandQueue();
            this.priorityOutgoingMessagesQueue = new CommandQueue();

            this.connectionsThread = new Thread(new ThreadStart(this.HandleConnectionsThreadMethod));
            this.commandsThread = new Thread(new ThreadStart(this.HandleCommandsThreadMethod));
            this.outgoingMessagesThread = new Thread(new ThreadStart(this.HandleOutgoingMessagesThreadMethod));
            this.localCommandsThread = new Thread(new ThreadStart(this.HandleLocalCommandsThreadMethod));
            this.priorityOutgoingMessagesThread = new Thread(new ThreadStart(this.HandleOutgoingPriorityMessagesThreadMethod));

            this.defaultUserManagementSystem = new LowlevelUserManagmentSystem();
            this.clientsHandler = new ClientsHandler();

            ///It still missing the filter
            this.usersAccountManager = new AccountManager();

            this.chatManager = new ChatManager(ChatManager.DefaultStorageMode.NonPersistent);

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

        #region Management

        public bool StartServer()
        {
            KSPMGlobals.Globals.Log.WriteTo("Starting KSPM server.");
            if (!this.ableToRun)
            {
                KSPMGlobals.Globals.Log.WriteTo(Error.ErrorType.ServerUnableToRun.ToString());
                return false;
            }
            this.tcpIpEndPoint = new IPEndPoint(IPAddress.Any, this.lowLevelOperationSettings.tcpPort);
            this.tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.tcpSocket.NoDelay = true;

            try
            {
                this.tcpSocket.Bind(this.tcpIpEndPoint);
                this.alive = true;
                this.connectionsThread.Start();
                this.commandsThread.Start();
                this.outgoingMessagesThread.Start();
                this.localCommandsThread.Start();
                this.priorityOutgoingMessagesThread.Start();
            }
            catch (Exception ex)
            {
                ///If there is some exception, the server must shutdown itself and its threads.
                KSPMGlobals.Globals.Log.WriteTo(ex.Message);
                this.ShutdownServer();
                this.alive = false;
            }
            return true;  
        }

        public void ShutdownServer()
        {
            KSPMGlobals.Globals.Log.WriteTo("Shuttingdown the KSPM server .");

            ///*************************Killing threads code
            this.alive = false;
            this.commandsThread.Abort();
            this.connectionsThread.Abort();
            this.outgoingMessagesThread.Abort();
            this.localCommandsThread.Abort();
            this.priorityOutgoingMessagesThread.Abort();

            this.connectionsThread.Join();
            KSPMGlobals.Globals.Log.WriteTo("Killed connectionsThread .");
            this.commandsThread.Join();
            KSPMGlobals.Globals.Log.WriteTo("Killed commandsTread .");
            this.localCommandsThread.Join();
            KSPMGlobals.Globals.Log.WriteTo("Killed localCommandsTread .");
            this.outgoingMessagesThread.Join();
            KSPMGlobals.Globals.Log.WriteTo("Killed outgoingMessagesTread .");
            this.priorityOutgoingMessagesThread.Join();
            KSPMGlobals.Globals.Log.WriteTo("Killed priorityOutgoingMessagesTread .");


            ///*************************Killing TCP socket code
            if (this.tcpSocket.Connected)
            {
                this.tcpSocket.Shutdown(SocketShutdown.Both);
                this.tcpSocket.Close();
            }
            this.tcpBuffer = null;
            this.tcpIpEndPoint = null;

            KSPMGlobals.Globals.Log.WriteTo("Killing conected clients!!!");
            this.clientsHandler.Release();
            this.clientsHandler = null;

            KSPMGlobals.Globals.Log.WriteTo("Killing chat system!!!");
            this.chatManager.Release();
            this.chatManager = null;

            ///*********************Killing server itself
            this.ableToRun = false;
            this.commandsQueue.Purge(false);
            this.outgoingMessagesQueue.Purge(false);
            this.localCommandsQueue.Purge(false);
            this.priorityOutgoingMessagesQueue.Purge(false);
            this.commandsQueue = null;
            this.localCommandsQueue = null;
            this.outgoingMessagesQueue = null;
            this.priorityOutgoingMessagesQueue = null;

            KSPMGlobals.Globals.Log.WriteTo(string.Format("Server KSPM killed after {0} miliseconds alive!!!", RealTimer.Timer.ElapsedMilliseconds));
        }

        #endregion

        #region IncomingConnections

        /// <summary>
        /// Handles the incoming connections through a TCP socket.
        /// </summary>
        protected void HandleConnectionsThreadMethod()
        {
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
                    GameServer.SignalHandler.Reset();
                    this.tcpSocket.BeginAccept(new AsyncCallback(this.OnAsyncAcceptIncomingConnection), this.tcpSocket);
                    GameServer.SignalHandler.WaitOne();
                    Thread.Sleep(11);
                }
            }
            catch (ThreadAbortException)
            {
                if (this.tcpSocket.Connected)///Avoids exceptions
                {
                    this.tcpSocket.Shutdown(SocketShutdown.Both);
                    this.tcpSocket.Close();
                }
                this.alive = false;
            }
            catch (Exception ex)
            {
                KSPMGlobals.Globals.Log.WriteTo(ex.Message);
            }
        }

        /// <summary>
        /// Method called in asynchronously or synchronously  each time that a new connection is attempted.
        /// </summary>
        /// <param name="result"></param>
        protected void OnAsyncAcceptIncomingConnection(IAsyncResult result)
        {
            GameServer.SignalHandler.Set();
            Socket callingSocket, incomingConnectionSocket;
            NetworkRawEntity newNetworkEntity;
            callingSocket = (Socket)result.AsyncState;
            incomingConnectionSocket = callingSocket.EndAccept(result);
            newNetworkEntity = new NetworkEntity(ref incomingConnectionSocket);
            incomingConnectionSocket.BeginReceive(newNetworkEntity.ownerNetworkCollection.secondaryRawBuffer, 0, newNetworkEntity.ownerNetworkCollection.secondaryRawBuffer.Length, SocketFlags.None, this.ReceiveCallback, newNetworkEntity);
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
            Queue<byte[]> packets = new Queue<byte[]>();
            try
            {
                readBytes = callingEntity.ownerNetworkCollection.socketReference.EndReceive(result);
                if (readBytes > 0)
                {
                    if (PacketHandler.DecodeRawPacket(ref callingEntity.ownerNetworkCollection.secondaryRawBuffer) == Error.ErrorType.Ok)
                    {
                        if (PacketHandler.Packetize(callingEntity.ownerNetworkCollection.secondaryRawBuffer, readBytes, packets) == Error.ErrorType.Ok)
                        {
                            while (packets.Count > 0)
                            {
                                if (PacketHandler.InflateManagedMessageAlt(packets.Dequeue(), callingEntity, out incomingMessage) == Error.ErrorType.Ok)
                                {
                                    ///Adding to the local queue.
                                    this.localCommandsQueue.EnqueueCommandMessage(ref incomingMessage);
                                    KSPMGlobals.Globals.Log.WriteTo("First command!!!");
                                }
                            }
                        }
                    }
                }
            }
            catch (System.Exception)
            {
            }
        }

        #endregion

        /// <summary>
        /// Handles the those commands send by the client through a TCP socket.
        /// </summary>
        protected void HandleCommandsThreadMethod()
        {
            Message messageToProcess = null;
            Message responseMessage = null;
            ManagedMessage managedMessageReference = null;
            NetworkEntity messageOwner = null;
            ServerSideClient newClientAttempt = null;
            ServerSideClient serverSideClientReference = null;
            GameUser referredUser = null;
            ChatMessage chatMessage = null;
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
                        managedMessageReference = (ManagedMessage)messageToProcess;
                        if (messageToProcess != null)
                        {
                            
                            switch (messageToProcess.Command)
                            {
                                case Message.CommandType.NewClient:
                                    messageOwner = managedMessageReference.OwnerNetworkEntity;
                                    if (this.clientsHandler.ConnectedClients < this.lowLevelOperationSettings.maxConnectedClients)
                                    {
                                        if (this.defaultUserManagementSystem.Query(ref messageOwner))
                                        {
                                            if (ServerSideClient.CreateFromNetworkEntity(ref messageOwner, out newClientAttempt) == Error.ErrorType.Ok)
                                            {
                                                newClientAttempt.RegisterUserConnectedEvent(this.UserConnected);
                                                if (newClientAttempt.StartClient())
                                                {
                                                    this.clientsHandler.AddNewClient(newClientAttempt);
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        //Creates the reject message and set the callback to the RejectMessageToClient. Performed in that way because it is needed to send the reject message before to proceed to disconnect the client.
                                        Message.ServerFullMessage(messageOwner, out responseMessage);
                                        PacketHandler.EncodeRawPacket(ref responseMessage.bodyMessage);
                                        ((ManagedMessage)responseMessage).OwnerNetworkEntity.SetMessageSentCallback(this.RejectMessageToClient);
                                        this.outgoingMessagesQueue.EnqueueCommandMessage(ref responseMessage);
                                    }
                                    break;
                                case Message.CommandType.StopServer:
                                    this.ShutdownServer();
                                    break;
                                case Message.CommandType.Authentication:
                                    //User.InflateUserFromBytes(ref managedMessageReference.OwnerNetworkEntity.ownerNetworkCollection.secondaryRawBuffer, out referredUser);
                                    User.InflateUserFromBytes(ref messageToProcess.bodyMessage, out referredUser);
                                    serverSideClientReference = (ServerSideClient)managedMessageReference.OwnerNetworkEntity;
                                    serverSideClientReference.gameUser = referredUser;
                                    messageOwner = serverSideClientReference;
                                    if (this.usersAccountManager.Query(ref messageOwner))
                                    {
                                        /*
                                        Message.AuthenticationSuccessMessage(messageOwner, out responseMessage);
                                        this.outgoingMessagesQueue.EnqueueCommandMessage(ref responseMessage);
                                         */
                                        serverSideClientReference.RemoveAwaitingState(ServerSideClient.ClientStatus.Authenticated);
                                    }
                                    else
                                    {
                                        ///Need to improve this code for a only one if.
                                        ///And to check if is it needed to send a disconnect message before release the socket.
                                        if ((serverSideClientReference.gameUser.AuthencticationAttempts++) < this.lowLevelOperationSettings.maxAuthenticationAttempts)
                                        {
                                            ///There is still a chance to authenticate again.
                                            Message.AuthenticationFailMessage(messageOwner, out responseMessage);
                                            PacketHandler.EncodeRawPacket(ref responseMessage.bodyMessage);
                                        }
                                        else
                                        {
                                            ///There is no chance to try it again.
                                            ((ManagedMessage)responseMessage).OwnerNetworkEntity.SetMessageSentCallback(this.RejectMessageToClient);
                                        }
                                        this.outgoingMessagesQueue.EnqueueCommandMessage(ref responseMessage);
                                    }
                                    break;
                                case Message.CommandType.Disconnect:
                                    ///Disconnects either a NetworkEntity or a ServerSideClient.
                                    this.OnUserDisconnected(managedMessageReference.OwnerNetworkEntity, null);
                                    this.chatManager.UnregisterUser(managedMessageReference.OwnerNetworkEntity);
                                    this.clientsHandler.RemoveClient(managedMessageReference.OwnerNetworkEntity);                                    
                                    break;

                                case Message.CommandType.Chat:
                                    if (ChatMessage.InflateChatMessage(messageToProcess.bodyMessage, out chatMessage) == Error.ErrorType.Ok)
                                    {
                                        KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}][{1}_{2}]-Says:{3}", managedMessageReference.OwnerNetworkEntity.Id, chatMessage.Time.ToShortTimeString(), chatMessage.sendersUsername, chatMessage.Body));
                                        //this.clientsHandler.TCPBroadcastTo(this.chatManager.AttachMessage(chatMessage).MembersAsList, messageToProcess);
                                    }
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
            ManagedMessage managedReference = null;
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
                        managedReference = (ManagedMessage)outgoingMessage;
                        if (outgoingMessage != null)
                        {
                            //KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}]===Error==={1}.", outgoingMessage.bodyMessage[ 4 ], outgoingMessage.Command));
                            try
                            {
                                ///Checking if the NetworkEntity is still running.
                                if (managedReference.OwnerNetworkEntity.IsAlive())
                                {
                                    managedReference.OwnerNetworkEntity.ownerNetworkCollection.socketReference.BeginSend(outgoingMessage.bodyMessage, 0, (int)outgoingMessage.MessageBytesSize, SocketFlags.None, new AsyncCallback(this.AsyncSenderCallback), managedReference.OwnerNetworkEntity);
                                }
                            }
                            catch (System.Exception ex)
                            {
                                Message killMessage = null;
                                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}][\"{1}-{2}\"] Something went wrong with the remote client, performing a removing process on it.", managedReference.OwnerNetworkEntity.Id, "HandleOutgoingMessages", ex.Message));
                                Message.DisconnectMessage(managedReference.OwnerNetworkEntity, out killMessage);
                                KSPMGlobals.Globals.KSPMServer.localCommandsQueue.EnqueueCommandMessage(ref killMessage);
                            }
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
            Message messageToProcess = null;
            Message responseMessage = null;
            ManagedMessage managedMessageReference = null;
            NetworkEntity messageOwner = null;
            ServerSideClient newClientAttempt = null;
            ServerSideClient serverSideClientReference = null;
            GameUser referredUser = null;
            ChatMessage chatMessage = null;
            if (!this.ableToRun)
            {
                KSPMGlobals.Globals.Log.WriteTo(Error.ErrorType.ServerUnableToRun.ToString());
            }
            try
            {
                KSPMGlobals.Globals.Log.WriteTo("-Starting to handle commands[ " + this.alive + " ]");
                while (this.alive)
                {
                    if (!this.localCommandsQueue.IsEmpty())
                    {
                        this.localCommandsQueue.DequeueCommandMessage(out messageToProcess);
                        managedMessageReference = (ManagedMessage)messageToProcess;
                        if (messageToProcess != null)
                        {
                            KSPMGlobals.Globals.Log.WriteTo(messageToProcess.Command.ToString());
                            switch (messageToProcess.Command)
                            {
                                case Message.CommandType.NewClient:
                                    messageOwner = managedMessageReference.OwnerNetworkEntity;
                                    if (this.clientsHandler.ConnectedClients < this.lowLevelOperationSettings.maxConnectedClients)
                                    {
                                        if (this.defaultUserManagementSystem.Query(ref messageOwner))
                                        {
                                            if (ServerSideClient.CreateFromNetworkEntity(ref messageOwner, out newClientAttempt) == Error.ErrorType.Ok)
                                            {
                                                newClientAttempt.RegisterUserConnectedEvent(this.UserConnected);
                                                if (newClientAttempt.StartClient())
                                                {
                                                    this.clientsHandler.AddNewClient(newClientAttempt);
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        //Creates the reject message and set the callback to the RejectMessageToClient. Performed in that way because it is needed to send the reject message before to proceed to disconnect the client.
                                        Message.ServerFullMessage(messageOwner, out responseMessage);
                                        PacketHandler.EncodeRawPacket(ref responseMessage.bodyMessage);
                                        ((ManagedMessage)responseMessage).OwnerNetworkEntity.SetMessageSentCallback(this.RejectMessageToClient);
                                        this.priorityOutgoingMessagesQueue.EnqueueCommandMessage(ref responseMessage);
                                    }
                                    break;
                                case Message.CommandType.StopServer:
                                    this.ShutdownServer();
                                    break;
                                case Message.CommandType.Authentication:
                                    //User.InflateUserFromBytes(ref managedMessageReference.OwnerNetworkEntity.ownerNetworkCollection.secondaryRawBuffer, out referredUser);
                                    User.InflateUserFromBytes(ref messageToProcess.bodyMessage, out referredUser);
                                    serverSideClientReference = (ServerSideClient)managedMessageReference.OwnerNetworkEntity;
                                    serverSideClientReference.gameUser = referredUser;
                                    messageOwner = serverSideClientReference;
                                    if (this.usersAccountManager.Query(ref messageOwner))
                                    {
                                        /*
                                        Message.AuthenticationSuccessMessage(messageOwner, out responseMessage);
                                        this.outgoingMessagesQueue.EnqueueCommandMessage(ref responseMessage);
                                         */
                                        serverSideClientReference.RemoveAwaitingState(ServerSideClient.ClientStatus.Authenticated);
                                    }
                                    else
                                    {
                                        ///Need to improve this code for a only one if.
                                        ///And to check if is it needed to send a disconnect message before release the socket.
                                        if ((serverSideClientReference.gameUser.AuthencticationAttempts++) < this.lowLevelOperationSettings.maxAuthenticationAttempts)
                                        {
                                            ///There is still a chance to authenticate again.
                                            Message.AuthenticationFailMessage(messageOwner, out responseMessage);
                                            PacketHandler.EncodeRawPacket(ref responseMessage.bodyMessage);
                                        }
                                        else
                                        {
                                            ///There is no chance to try it again.
                                            ((ManagedMessage)responseMessage).OwnerNetworkEntity.SetMessageSentCallback(this.RejectMessageToClient);
                                        }
                                        this.priorityOutgoingMessagesQueue.EnqueueCommandMessage(ref responseMessage);
                                    }
                                    break;
                                case Message.CommandType.Disconnect:
                                    ///Disconnects either a NetworkEntity or a ServerSideClient.
                                    this.OnUserDisconnected(managedMessageReference.OwnerNetworkEntity, null);
                                    this.chatManager.UnregisterUser(managedMessageReference.OwnerNetworkEntity);
                                    this.clientsHandler.RemoveClient(managedMessageReference.OwnerNetworkEntity);
                                    break;
                                    /*
                                case Message.CommandType.Chat:
                                    if (ChatMessage.InflateChatMessage(messageToProcess.bodyMessage, out chatMessage) == Error.ErrorType.Ok)
                                    {
                                        //chatMessage.From = ((ServerSideClient)managedMessageReference.OwnerNetworkEntity).gameUser.Username;
                                        this.clientsHandler.TCPBroadcastTo(this.chatManager.AttachMessage(chatMessage).MembersAsList, messageToProcess);
                                    }
                                    break;
                                    */
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
        protected void HandleOutgoingPriorityMessagesThreadMethod()
        {
            Message outgoingMessage = null;
            ManagedMessage managedReference = null;
            if (!this.ableToRun)
            {
                KSPMGlobals.Globals.Log.WriteTo(Error.ErrorType.ServerUnableToRun.ToString());
            }
            try
            {
                KSPMGlobals.Globals.Log.WriteTo("-Starting to handle outgoing messages[ " + this.alive + " ]");
                while (this.alive)
                {
                    if (!this.priorityOutgoingMessagesQueue.IsEmpty())
                    {
                        this.priorityOutgoingMessagesQueue.DequeueCommandMessage(out outgoingMessage);
                        managedReference = (ManagedMessage)outgoingMessage;
                        if (outgoingMessage != null)
                        {
                            //KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}]===Error==={1}.", outgoingMessage.bodyMessage[ 4 ], outgoingMessage.Command));
                            try
                            {
                                ///Checking if the NetworkEntity is still running.
                                if (managedReference.OwnerNetworkEntity.IsAlive())
                                {
                                    managedReference.OwnerNetworkEntity.ownerNetworkCollection.socketReference.BeginSend(outgoingMessage.bodyMessage, 0, (int)outgoingMessage.MessageBytesSize, SocketFlags.None, new AsyncCallback(this.AsyncSenderCallback), managedReference.OwnerNetworkEntity);
                                }
                            }
                            catch (System.Exception ex)
                            {
                                Message killMessage = null;
                                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}][\"{1}-{2}\"] Something went wrong with the remote client, performing a removing process on it.", managedReference.OwnerNetworkEntity.Id, "HandleOutgoingPriorityMessages", ex.Message));
                                Message.DisconnectMessage(managedReference.OwnerNetworkEntity, out killMessage);
                                KSPMGlobals.Globals.KSPMServer.localCommandsQueue.EnqueueCommandMessage(ref killMessage);
                            }
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
        /// Method used to send asynchronously a message. This method calls the MessageSentCallbak once the send task is completed, but do not get afraid of this callback call, if you have not set a method to the callback it will not be invoked at all.
        /// </summary>
        /// <param name="result"></param>
        public void AsyncSenderCallback(System.IAsyncResult result)
        {
            int sentBytes;
            NetworkEntity net = null;
            try
            {
                net = (NetworkEntity)result.AsyncState;
                sentBytes = net.ownerNetworkCollection.socketReference.EndSend(result);
				if( sentBytes > 0 )
				{
                	net.MessageSent(net, null);
				}
            }
            catch (System.Exception)
            {
            }
        }

        #region UserManagement

        protected void OnUserDisconnected( NetworkEntity sender, KSPMEventArgs e)
        {
            if (this.UserDisconnected != null)
            {
                this.UserDisconnected(sender, e);
            }
        }

        /// <summary>
        /// Method which removes a client, and is used to set the callback inside the networkentity.
        /// </summary>
        /// <param name="caller"></param>
        /// <param name="arg"></param>
        protected void RejectMessageToClient(NetworkEntity caller, object arg)
        {
            this.clientsHandler.RemoveClient(caller);
        }

        public NetworkEntity[] ConnectedClients
        {
            get
            {
                return this.clientsHandler.RemoteClients.ToArray();
            }
        }

        #endregion
    }
}
