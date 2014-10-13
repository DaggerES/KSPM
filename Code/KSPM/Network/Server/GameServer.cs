//#define PROFILING
//#define DEBUGTRACER_L2
//#define DEBUGTRACER_L3

using System;
using System.Collections.Generic;

using System.Threading;
using System.Net.Sockets;
using System.Net;

using KSPM.Globals;
using KSPM.Network.Common;
using KSPM.Network.Common.Packet;
using KSPM.Network.Common.Messages;
using KSPM.Network.Common.MessageHandlers;
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
    /// <summary>
    /// TODO: Create a filter to allow a number of maximun TCP connections.
    /// </summary>
    public class GameServer : IAsyncSender , IOwnedPacketArrived
    {
        #if PROFILING

        Profiler profilerOutgoingMessages;

        #endif

        /// <summary>
        /// Controls the life-cycle of the server, also the thread's life-cyle.
        /// </summary>
        protected bool alive;

        /// <summary>
        /// Controls if the server is set and ready to run.
        /// </summary>
        protected bool ableToRun;

        /// <summary>
        /// Settings to operate at low level, like listening ports and the like.
        /// </summary>
        protected ServerSettings lowLevelOperationSettings;

        /// <summary>
        /// Port manager to handle the UDP ports, ensuring an available port each time it is required.
        /// </summary>
        protected internal IOPortManager ioPortManager;

        /// <summary>
        /// Event raised when an UDP message has arrived to the server.
        /// </summary>
        public event UDPMessageArrived UDPMessageArrived;

        #region TCPProperties

        /// <summary>
        /// TCP socket used to receive the new incoming connections.
        /// </summary>
        protected Socket tcpSocket;

        /// <summary>
        /// The IP information required to set the TCP socket.<b>IP address and port are required.</b>
        /// </summary>
        protected IPEndPoint tcpIpEndPoint;

        /// <summary>
        /// Byte buffer attached to the TCP socket.<b>Used to receive the first commandof a new client.</b>
        /// </summary>
        protected byte[] tcpBuffer;

        /// <summary>
        /// SockeAsyncEventArgs pool to accept connections and to receive the first command.
        /// </summary>
        protected SocketAsyncEventArgsPool incomingConnectionsPool;

        /// <summary>
        /// Amount of time to set when the timer should check the capacity of the referred queue.
        /// </summary>
        internal int tcpPurgeTimeInterval;

        /// <summary>
        /// Tells the amount of messages are allowe to receive messages again.
        /// </summary>
        protected int tcpMinimumMessagesAllowedAfterPurge;

        /// <summary>
        /// Event raised when an User command is received by the server.
        /// </summary>
        public event TCPMessageArrived TCPMessageArrived;

        #endregion

        #region CommandsCode

        /// <summary>
        /// Primary Command manager, manages the commandsQueue reference.
        /// </summary>
        public PriorityQueue2Way primaryCommandQueue;

        /// <summary>
        /// Holds the commands to be processed by de server, like the command chat and other commands not required to the connection process.
        /// </summary>
        public BufferedCommandQueue commandsQueue;

        /// <summary>
        /// Holds those local commands to be processed by the server, such as the connection commands.
        /// </summary>
        public BufferedCommandQueue localCommandsQueue;

        /// <summary>
        /// Outgoing TCP messages.
        /// </summary>
        public CommandQueue outgoingMessagesQueue;

        /// <summary>
        /// Outgoing TCP priority messages like the connection commands and the like.
        /// </summary>
        public CommandQueue priorityOutgoingMessagesQueue;

        /// <summary>
        /// Preallocated incoming messages pool.
        /// </summary>
        public MessagesPool incomingMessagesPool;

        /// <summary>
        /// Preallocated incoming priority messages pool.
        /// </summary>
        public MessagesPool priorityMessagesPool;

        /// <summary>
        /// Preallocated outgoing broadcasting messages pool.
        /// </summary>
        public MessagesPool broadcastMessagesPool;


        #endregion

        #region Threading code

        /// <summary>
        /// Thread used to handle the incoming commands.
        /// </summary>
        protected Thread commandsThread;

        /// <summary>
        /// Thread to handle the sending process of non priority commands.
        /// </summary>
        protected Thread outgoingMessagesThread;

        /// <summary>
        /// Thread to handle those prioritized commands, like those used in the connection process.
        /// </summary>
        protected Thread localCommandsThread;

        /// <summary>
        /// Thread to handle the prioritized outgoing commands.
        /// </summary>
        protected Thread priorityOutgoingMessagesThread;

        #endregion

        #region USM

        /// <summary>
        /// Default User Management System (UMS) applied by the server.
        /// </summary>
        protected UserManagementSystem defaultUserManagementSystem;

        /// <summary>
        /// Provides a basic authentication.<b>At this moment is a by pass method.</b>
        /// </summary>
        protected AccountManager usersAccountManager;

        /// <summary>
        /// Poll of clients connected to the server, it should be used when the broadcasting is required.
        /// </summary>
        protected ClientsHandler clientsHandler;

        /// <summary>
        /// Event raised when a new user is connected.
        /// </summary>
        public event UserConnectedEventHandler UserConnected;

        /// <summary>
        /// Event raised when an user is disconnected from the user.
        /// </summary>
        public event UserDisconnectedEventHandler UserDisconnected;

        #endregion

        #region Chat

        /// <summary>
        /// Handles the KSPM Chat system, either UDP and TCP chating system.
        /// </summary>
        public ChatManager chatManager;

        #endregion

        #region CreationAndInitialization

        /// <summary>
        /// Constructor of the server
        /// </summary>
        /// <param name="operationSettings"></param>
        public GameServer(ref ServerSettings operationSettings)///Need to be set the buffer of receiving data
        {

#if PROFILING
            this.profilerOutgoingMessages = new Profiler("OutgoingMessages");
#endif
            ///Assigning settings to work.
            this.lowLevelOperationSettings = operationSettings;
            if (this.lowLevelOperationSettings == null)
            {
                this.ableToRun = false;
                return;
            }

            ///Used to receive the first command.
            this.tcpBuffer = new byte[ServerSettings.ServerBufferSize];

            ///Creating a new buffered CommandQueue capable to suport up to 1000 messages, each one of 1024 bytes length.
            this.commandsQueue = new BufferedCommandQueue((uint)ServerSettings.ServerBufferSize * 1000);
            ///Creating the local commands queue, capable to hold up to 100 messages, each one of 1024 bytes length.
            this.localCommandsQueue = new BufferedCommandQueue((uint)ServerSettings.ServerBufferSize * 100);
            ///Pool of pre-allocated messages with an initial capacity if 2000 messages.
            this.incomingMessagesPool = new MessagesPool(2000, new BufferedMessage(Message.CommandType.Null, 0, 0));
            ///Pool of pre-allocated messages used in the connection process. Up to 100 messages.
            this.priorityMessagesPool = new MessagesPool(100, new BufferedMessage(Message.CommandType.Null, 0, 0));
            ///Pool of pre-allocated messages used to broadcast messages. Up to to 2000 messages.
            this.broadcastMessagesPool = new MessagesPool(2000, new BroadcastMessage(Message.CommandType.Null, null));

            ///Creating the set of queues to be used by the system.
            this.primaryCommandQueue = new PriorityQueue2Way(this.commandsQueue, this.incomingMessagesPool);

            this.priorityOutgoingMessagesQueue = new CommandQueue();
            this.outgoingMessagesQueue = new CommandQueue();

            this.commandsThread = new Thread(new ThreadStart(this.HandleCommandsThreadMethod));
            this.outgoingMessagesThread = new Thread(new ThreadStart(this.HandleOutgoingMessagesThreadMethod));
            this.localCommandsThread = new Thread(new ThreadStart(this.HandleLocalCommandsThreadMethod));
            this.priorityOutgoingMessagesThread = new Thread(new ThreadStart(this.HandleOutgoingPriorityMessagesThreadMethod));

            this.defaultUserManagementSystem = new LowlevelUserManagmentSystem();
            this.clientsHandler = new ClientsHandler( (byte)this.lowLevelOperationSettings.maxConnectedClients );

            ///It still missing the filter
            this.usersAccountManager = new AccountManager();

            ///Creating a chat manager in non-persistent mode. It means that none of the chat messages will be stored.
            this.chatManager = new ChatManager(ChatManager.DefaultStorageMode.NonPersistent);

            this.incomingConnectionsPool = new SocketAsyncEventArgsPool((uint)this.lowLevelOperationSettings.connectionsBackog);

            ///TCP Purge Timer
            ///this.tcpPurgeTimer = new Timer(this.HandleTCPPurgeTimerCallback);
            this.tcpPurgeTimeInterval = (int)ServerSettings.PurgeTimeIterval;
            this.tcpMinimumMessagesAllowedAfterPurge = (int)(this.commandsQueue.MaxCommandAllowed * (1.0f - ServerSettings.AvailablePercentAfterPurge));

            this.ioPortManager = new IOPortManager(this.lowLevelOperationSettings.udpPortRange.assignablePortStart, this.lowLevelOperationSettings.udpPortRange.assignablePortEnd);

            this.ableToRun = true;
            this.alive = false;
        }

        #endregion

        #region Management

        /// <summary>
        /// Starts the server making it able to work.
        /// </summary>
        /// <returns>True if everything goes well, False otherwise.</returns>
        public bool StartServer()
        {
            bool result = false;
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

                ///Starting working threads.
                this.commandsThread.Start();
                this.outgoingMessagesThread.Start();
                this.localCommandsThread.Start();
                this.priorityOutgoingMessagesThread.Start();

                ///Starting to listen for connections.
                this.tcpSocket.Listen(this.lowLevelOperationSettings.connectionsBackog);
                KSPMGlobals.Globals.Log.WriteTo("-Starting to handle conenctions[ " + this.alive + " ]");
                this.StartReceiveConnections();
                result = true;
            }
            catch (Exception ex)
            {
                ///If there is some exception, the server must shutdown itself and its threads.
                KSPMGlobals.Globals.Log.WriteTo(ex.Message);
                KSPMGlobals.Globals.Log.WriteTo(ex.StackTrace);
                KSPMGlobals.Globals.Log.WriteTo(ex.GetType().FullName);
                this.ShutdownServer();
                this.alive = false;
            }
            return result;
        }

        /// <summary>
        /// Stops the server, making it unable it to work. So if you must create a new instance of the server if you want to run it again.
        /// </summary>
        public void ShutdownServer()
        {
            KSPMGlobals.Globals.Log.WriteTo("Shuttingdown the KSPM server .");

            ///*************************Killing threads code
            this.alive = false;
            this.commandsThread.Abort();
            this.outgoingMessagesThread.Abort();
            this.localCommandsThread.Abort();
            this.priorityOutgoingMessagesThread.Abort();
            
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
                ///If it is connected, it must call shutdown method.
                this.tcpSocket.Shutdown(SocketShutdown.Both);
            }
            ///Either it is connected or not it must be closed.
            
            this.tcpSocket.Close();
            this.tcpBuffer = null;
            this.tcpIpEndPoint = null;

            KSPMGlobals.Globals.Log.WriteTo("Killing chat system!!!");
            this.chatManager.Release();
            this.chatManager = null;

            KSPMGlobals.Globals.Log.WriteTo("Killing conected clients!!!");
            this.clientsHandler.Release();
            this.clientsHandler = null;

            ///*********************Killing server itself
            this.ableToRun = false;

            ///Releasing command queues.
            this.primaryCommandQueue.Release();
            this.primaryCommandQueue = null;

            ///Required release all those commands.
            this.localCommandsQueue.Purge(false);
            this.localCommandsQueue = null;

            this.outgoingMessagesQueue.Purge(false);
            this.priorityOutgoingMessagesQueue.Purge(false);
            
            this.commandsQueue = null;
            this.localCommandsQueue = null;
            this.outgoingMessagesQueue = null;
            this.priorityOutgoingMessagesQueue = null;

            ///Releasing messages pools.
            this.priorityMessagesPool.Release();
            this.priorityMessagesPool = null;
            this.incomingMessagesPool = null;
            this.broadcastMessagesPool.Release();
            this.broadcastMessagesPool = null;

            ///Releasing SAEA pool.
            this.incomingConnectionsPool.Release(false);
            this.incomingConnectionsPool = null;

            this.usersAccountManager = null;
            this.lowLevelOperationSettings = null;

            ///Releasing the TCP purge timer.
            //this.tcpPurgeTimer.Dispose();
            //this.tcpPurgeTimer = null;

            this.ioPortManager.Release();
            this.ioPortManager = null;

            KSPMGlobals.Globals.Log.WriteTo(string.Format("Server KSPM killed after {0} miliseconds alive!!!", RealTimer.Timer.ElapsedMilliseconds));

#if PROFILING
            this.profilerOutgoingMessages.Dispose();
#endif
        }

        #endregion

        #region IncomingConnections

        /// <summary>
        /// Starts to receive incoming connections asynchronously.
        /// </summary>
        protected void StartReceiveConnections()
        {
            ///Taking out a SocketAsyncEventArgs from the pool.
            SocketAsyncEventArgs incomingConnection = this.incomingConnectionsPool.NextSlot;
            incomingConnection.Completed += new EventHandler<SocketAsyncEventArgs>(this.OnAsyncIncomingConnectionComplete);

            try
            {
                ///Returns false if the process was completed synchrounosly.
                if (!this.tcpSocket.AcceptAsync(incomingConnection))
                {
                    this.OnAsyncIncomingConnectionComplete(this, incomingConnection);
                }
            }
            catch (System.Exception)
            { }
        }

        /// <summary>
        /// Method called when a connection has been made through the socket. <b>Need to think in some antispam filter.</b>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">The SocketAsyncEventArgs used to perform the connection process.</param>
        protected void OnAsyncIncomingConnectionComplete(object sender, SocketAsyncEventArgs e)
        {
            SocketAsyncEventArgs acceptedConnection;
            if (e.SocketError == SocketError.Success)
            {
                ///Taking out another SocketAsyncEventArgs from the pool to receive the first command.
                acceptedConnection = this.incomingConnectionsPool.NextSlot;
                acceptedConnection.AcceptSocket = e.AcceptSocket;
                acceptedConnection.Completed += new EventHandler<SocketAsyncEventArgs>(this.OnAsyncFirstDataIncomingComplete);
                acceptedConnection.SetBuffer(this.tcpBuffer, 0, this.tcpBuffer.Length);

                ///If the method was processed synchronously.
                if (!acceptedConnection.AcceptSocket.ReceiveAsync(acceptedConnection))
                {
                    this.OnAsyncFirstDataIncomingComplete(this, acceptedConnection);
                }
            }
            else
            {
                KSPMGlobals.Globals.Log.WriteTo(string.Format("Something happened while the accepting process: {0}", e.SocketError.ToString()));
                ///If something fails, we started to receive another conn.
                this.StartReceiveConnections();
            }
            ///Restoring the AsyncEventArgs used to perform the connection process.
            e.Completed -= this.OnAsyncIncomingConnectionComplete;
            if (this.incomingConnectionsPool != null)
            {
                this.incomingConnectionsPool.Recycle(e);
            }
            else
            {
                e.Dispose();
                e = null;
            }
        }

        /// <summary>
        /// Method called once the first command was received by the server.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">SocketAsyncEventArgs used to receive the first command.</param>
        protected void OnAsyncFirstDataIncomingComplete(object sender, SocketAsyncEventArgs e)
        {
            NetworkEntity newNetworkEntity;
            Socket acceptedSocket = null;
            if (e.SocketError == SocketError.Success)
            {
                if (e.BytesTransferred > 0)
                {
                    acceptedSocket = e.AcceptSocket;
                    newNetworkEntity = new NetworkEntity(ref acceptedSocket);
                    PacketHandler.PacketizeToOwner(e.Buffer, e.BytesTransferred, newNetworkEntity, this);
                }
            }
            ///If there were a success process or not we need to receive another
            this.StartReceiveConnections();

            ///Restoring the SocketAsyncEventArgs used to perform the receive process.
            e.Completed -= OnAsyncFirstDataIncomingComplete;
            this.incomingConnectionsPool.Recycle(e);
        }

        /// <summary>
        /// Used to process an incoming stream.
        /// </summary>
        /// <param name="rawData"></param>
        /// <param name="rawDataOffset"></param>
        /// <param name="fixedLength"></param>
        /// <param name="packetOwner"></param>
        public void ProcessPacket(byte[] rawData, uint rawDataOffset, uint fixedLength, NetworkEntity packetOwner)
        {
            Message incomingMessage = null;
            incomingMessage = this.priorityMessagesPool.BorrowMessage;
            ((BufferedMessage)incomingMessage).Load(rawData, rawDataOffset, fixedLength);
            ((BufferedMessage)incomingMessage).SetOwnerMessageNetworkEntity(packetOwner);

#if DEBUGTRACER_L3
            KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo(string.Format("GameServer.ProcessPacket(byte[] rawData, uint rawDataOffset, uint fixedLength, NetworkEntity packetOwner) -> {0}", incomingMessage.ToString()));
#endif

            if (incomingMessage.Priority == KSPMSystem.PriorityLevel.Critical)
            {
                if (!this.localCommandsQueue.EnqueueCommandMessage(ref incomingMessage))
                {
                    this.priorityMessagesPool.Recycle(incomingMessage);
                }
            }
        }

        /// <summary>
        /// Event raised each time an User command is received.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        protected internal void OnTCPMessageArrived(NetworkEntity sender, ManagedMessage message)
        {
            if (this.TCPMessageArrived != null)
            {
                this.TCPMessageArrived(sender, message);
            }
        }

        #endregion

        #region Non-prioritizedCommandHandle

        /// <summary>
        /// Handles the those commands send by the client through a TCP socket.
        /// </summary>
        protected void HandleCommandsThreadMethod()
        {
            Message messageToProcess = null;
            ManagedMessage managedMessageReference = null;
            KSPMSystem.PriorityLevel userCommandPriority;
            if (!this.ableToRun)
            {
                KSPMGlobals.Globals.Log.WriteTo(Error.ErrorType.ServerUnableToRun.ToString());
            }
            try
            {
                KSPMGlobals.Globals.Log.WriteTo("-Starting to handle commands[ " + this.alive + " ]");
                while (this.alive)
                {
                    this.primaryCommandQueue.WorkingQueue.DequeueCommandMessage(out messageToProcess);
                    if (messageToProcess != null)
                    {
#if DEBUGTRACER_L2
                        KSPMGlobals.Globals.Log.WriteTo(string.Format("GameServer.HandleCommandsThreadMethod -> {0}", messageToProcess.ToString()));
#endif
                        managedMessageReference = (ManagedMessage)messageToProcess;
                        switch (messageToProcess.Command)
                        {
                            case Message.CommandType.Chat:
                                ///This if means that if the level warning is less than KSPM.System.Easy value as integer the message will be processed.
                                if ( this.primaryCommandQueue.WarningFlagLevel < 2)
                                {
                                    this.clientsHandler.TCPBroadcastTo(this.chatManager.GetChatGroupById(ChatMessage.InflateTargetGroupId(messageToProcess.bodyMessage)).MembersAsList, messageToProcess);
                                }
                                break;
                            case Message.CommandType.KeepAlive:
                                ///This is only to show something.
                                KSPMGlobals.Globals.Log.WriteTo("KeepAlive command: " + messageToProcess.Command.ToString());
                                break;
                            case Message.CommandType.User:
                                messageToProcess.UserDefinedCommand = messageToProcess.bodyMessage[13];
                                userCommandPriority = (KSPMSystem.PriorityLevel)Message.CommandPriority(messageToProcess.UserDefinedCommand);
                                ///Checking the priority level from the UserDefinedCommand because it defines if the message is bypassed or not.
                                switch( this.primaryCommandQueue.WarningFlagLevel )
                                {
                                    case (int)KSPMSystem.WarningLevel.Warning:
                                        ///Only Critical commands are delivered.
                                        if( userCommandPriority == KSPMSystem.PriorityLevel.Critical)
                                        {
                                            ///Rising the TCP event.
                                            this.OnTCPMessageArrived(managedMessageReference.OwnerNetworkEntity, managedMessageReference);
                                        }
                                        break;
                                    case (int)KSPMSystem.WarningLevel.Carefull:
                                        ///Only those commands: High and Critical are delivered.
                                        if( userCommandPriority <= KSPMSystem.PriorityLevel.High)
                                        {
                                            ///Rising the TCP event.
                                            this.OnTCPMessageArrived(managedMessageReference.OwnerNetworkEntity, managedMessageReference);
                                        }
                                        break;
                                    default:
                                        ///Another warning level, every message is delivered.
                                        ///Rising the TCP event.
                                        this.OnTCPMessageArrived(managedMessageReference.OwnerNetworkEntity, managedMessageReference);
                                        break;
                                }
                                break;
                            case Message.CommandType.Unknown:
                            default:
                                KSPMGlobals.Globals.Log.WriteTo("Non-Critical Unknown command: " + messageToProcess.Command.ToString());
                                break;
                        }
                        ///Releasing and recycling the message.
                        this.incomingMessagesPool.Recycle(messageToProcess);
                    }
                    else
                    {
                        ///Yielding the process.
                        Thread.Sleep(0);
                    }
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
            BroadcastMessage broadcastReference = null;
            SocketAsyncEventArgs sendingData = null;
            int entityCounter = 0;
            int blockSize;
            if (!this.ableToRun)
            {
                KSPMGlobals.Globals.Log.WriteTo(Error.ErrorType.ServerUnableToRun.ToString());
            }
            try
            {
                KSPMGlobals.Globals.Log.WriteTo("-Starting to handle outgoing messages[ " + this.alive + " ]");
                while (this.alive)
                {
#if PROFILING
                        this.profilerOutgoingMessages.Set();
#endif
                        this.outgoingMessagesQueue.DequeueCommandMessage(out outgoingMessage);
                        if (outgoingMessage != null)
                        {
                            ///Writing the message id into the message's buffer.
                            outgoingMessage.MessageId = (uint)System.Threading.Interlocked.Increment(ref Message.MessageCounter);
                            System.Buffer.BlockCopy(System.BitConverter.GetBytes(outgoingMessage.MessageId), 0, outgoingMessage.bodyMessage, (int)PacketHandler.PrefixSize, 4);
#if DEBUGTRACER_L2
                            KSPMGlobals.Globals.Log.WriteTo(string.Format("GameServer.HandleOutgoingMessagesThreadMethod -> {0}", outgoingMessage.ToString()));
#endif
                            try
                            {
                                ///If it is broadcaste message a different sending procces is performed.
                                if (outgoingMessage.IsBroadcast)
                                {
                                    broadcastReference = (BroadcastMessage)outgoingMessage;
                                    for (entityCounter = 0; entityCounter < broadcastReference.Targets.Count; entityCounter++)
                                    {
                                        if (broadcastReference.Targets[entityCounter] != null && broadcastReference.Targets[entityCounter].IsAlive())
                                        {
                                            blockSize = System.BitConverter.ToInt32(outgoingMessage.bodyMessage, 4);
                                            if (blockSize == outgoingMessage.MessageBytesSize)
                                            {
                                                sendingData = ((ServerSideClient)broadcastReference.Targets[entityCounter]).TCPOutSocketAsyncEventArgsPool.NextSlot;
                                                sendingData.AcceptSocket = broadcastReference.Targets[entityCounter].ownerNetworkCollection.socketReference;
                                                sendingData.UserToken = broadcastReference.Targets[entityCounter];
                                                sendingData.SetBuffer(outgoingMessage.bodyMessage, 0, (int)outgoingMessage.MessageBytesSize);
                                                if (!broadcastReference.Targets[entityCounter].ownerNetworkCollection.socketReference.SendAsync(sendingData))
                                                {
                                                    this.OnSendingOutgoingDataComplete(this, sendingData);
                                                }
                                            }
                                            else
                                            {
                                                KSPMGlobals.Globals.Log.WriteTo("Non-Priortized----PACKET CRC ERROR, Avoiding it.");
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    managedReference = (ManagedMessage)outgoingMessage;
                                    blockSize = System.BitConverter.ToInt32(outgoingMessage.bodyMessage, 4);
                                    if (blockSize == outgoingMessage.MessageBytesSize)
                                    {
                                        ///Checking if the NetworkEntity is still running and it has not been released.
                                        if (managedReference.OwnerNetworkEntity != null && managedReference.OwnerNetworkEntity.IsAlive())
                                        {
                                            sendingData = ((ServerSideClient)managedReference.OwnerNetworkEntity).TCPOutSocketAsyncEventArgsPool.NextSlot;
                                            sendingData.AcceptSocket = managedReference.OwnerNetworkEntity.ownerNetworkCollection.socketReference;
                                            sendingData.UserToken = managedReference.OwnerNetworkEntity;
                                            sendingData.SetBuffer(outgoingMessage.bodyMessage, 0, (int)outgoingMessage.MessageBytesSize);
                                            if (!managedReference.OwnerNetworkEntity.ownerNetworkCollection.socketReference.SendAsync(sendingData))
                                            {
                                                this.OnSendingOutgoingDataComplete(this, sendingData);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        KSPMGlobals.Globals.Log.WriteTo("Non-Prioritized----PACKET CRC ERROR, Avoiding it.");
                                    }
                                }
                            }
                            catch (System.Exception ex)
                            {
                                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}][\"{1}-{2}\"] Something went wrong with the remote client, performing a removing process on it.", (outgoingMessage.IsBroadcast ? ((BroadcastMessage)outgoingMessage).Targets[entityCounter].Id : ((ManagedMessage)outgoingMessage).OwnerNetworkEntity.Id), "HandleOutgoingMessages", ex.Message));
                                this.DisconnectClient((outgoingMessage.IsBroadcast ? ((BroadcastMessage)outgoingMessage).Targets[entityCounter] : ((ManagedMessage)outgoingMessage).OwnerNetworkEntity), new KSPMEventArgs(KSPMEventArgs.EventType.RuntimeError, KSPMEventArgs.EventCause.ErrorByException));
                            }
                            finally
                            {
                                ///Cleaning up.
                                if( outgoingMessage.IsBroadcast)
                                {
                                    this.broadcastMessagesPool.Recycle(outgoingMessage);
                                }
                                else
                                {
                                    outgoingMessage.Release();
                                }
                                outgoingMessage = null;
                            }
                        }
                        else
                        {
                            ///Yielding the process.
                            Thread.Sleep(0);
                        }
#if PROFILING
                        this.profilerOutgoingMessages.Mark();
#endif
                    }
            }
            catch (ThreadAbortException)
            {
                this.alive = false;
            }
        }

        /// <summary>
        /// Method called when a asynchronous sending  is complete.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">SocketAsyncEventArgs used to perform the sending stuff.</param>
        protected internal void OnSendingOutgoingDataComplete(object sender, SocketAsyncEventArgs e)
        {
            ServerSideClient networkEntitySender = (ServerSideClient)e.UserToken;
            if (e.SocketError == SocketError.Success)
            {
                if (e.BytesTransferred > 0)
                {
                    networkEntitySender.MessageSent(networkEntitySender, null);
                }
            }

            ///Checking if the SocketAsyncEventArgs pool has not been released and set to null.
            ///If the situtation mentioned above we have to dispose the SocketAsyncEventArgs by hand.
            if (networkEntitySender.TCPOutSocketAsyncEventArgsPool == null)
            {
                e.Dispose();
                e = null;
            }
            else
            {
                ///Recycling the SocketAsyncEventArgs used by this process.
                networkEntitySender.TCPOutSocketAsyncEventArgsPool.Recycle(e);
            }
        }

        #endregion

        #region PrioritizedCommandsHandle
        /// <summary>
        /// Handles the commands passed by the UI or the console if is it one implemented.
        /// </summary>
        protected void HandleLocalCommandsThreadMethod()
        {
            Message messageToProcess = null;
            Message responseMessage = null;
            ManagedMessage managedMessageReference = null;
            ServerSideClient newClientAttempt = null;
            ServerSideClient serverSideClientReference = null;
            GameUser referredUser = null;
            if (!this.ableToRun)
            {
                KSPMGlobals.Globals.Log.WriteTo(Error.ErrorType.ServerUnableToRun.ToString());
            }
            try
            {
                KSPMGlobals.Globals.Log.WriteTo("-Starting to handle local commands[ " + this.alive + " ]");
                while (this.alive)
                {
                        this.localCommandsQueue.DequeueCommandMessage(out messageToProcess);                        
                        if (messageToProcess != null)
                        {
#if DEBUGTRACER_L2
                            KSPMGlobals.Globals.Log.WriteTo(string.Format("GameServer.HandleLocalCommandsThreadMethod -> {0}", messageToProcess.Command.ToString()));
#endif
                            managedMessageReference = (ManagedMessage)messageToProcess;
                            switch (messageToProcess.Command)
                            {
                                case Message.CommandType.NewClient:
                                    if (this.clientsHandler.ConnectedClients < this.lowLevelOperationSettings.maxConnectedClients)
                                    {
                                        if (this.defaultUserManagementSystem.Query(managedMessageReference.OwnerNetworkEntity))
                                        {
                                            if (ServerSideClient.CreateFromNetworkEntity(managedMessageReference.OwnerNetworkEntity, out newClientAttempt) == Error.ErrorType.Ok)
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
                                        Message.ServerFullMessage(managedMessageReference.OwnerNetworkEntity, out responseMessage);
                                        PacketHandler.EncodeRawPacket(ref responseMessage.bodyMessage);
                                        ((ManagedMessage)responseMessage).OwnerNetworkEntity.SetMessageSentCallback(this.RejectMessageToClient);
                                        this.priorityOutgoingMessagesQueue.EnqueueCommandMessage(ref responseMessage);
                                    }
                                    break;
                                case Message.CommandType.StopServer:
                                    this.ShutdownServer();
                                    break;
                                case Message.CommandType.Authentication:
                                    User.InflateUserFromBytes(messageToProcess.bodyMessage, ((BufferedMessage)messageToProcess).StartsAt, messageToProcess.MessageBytesSize, out referredUser);
                                    serverSideClientReference = (ServerSideClient)managedMessageReference.OwnerNetworkEntity;
                                    serverSideClientReference.gameUser = referredUser;

                                    ///Setting the NetworkEntity of this GameUser.
                                    referredUser.Parent = serverSideClientReference;
                                    if (this.usersAccountManager.Query(managedMessageReference.OwnerNetworkEntity))
                                    {
                                        /*
                                        Message.AuthenticationSuccessMessage(messageOwner, out responseMessage);
                                        this.outgoingMessagesQueue.EnqueueCommandMessage(ref responseMessage);
                                         */
                                        ///Setting the new id, this one is generated by the system itself.
                                        serverSideClientReference.gameUser.SetCustomId(this.clientsHandler.NextUserId());
                                        serverSideClientReference.RemoveAwaitingState(ServerSideClient.ClientStatus.Authenticated);
                                    }
                                    else
                                    {
                                        ///Need to improve this code for a only one if.
                                        ///And to check if is it needed to send a disconnect message before release the socket.
                                        if ((serverSideClientReference.gameUser.AuthencticationAttempts++) < this.lowLevelOperationSettings.maxAuthenticationAttempts)
                                        {
                                            ///There is still a chance to authenticate again.
                                            Message.AuthenticationFailMessage(managedMessageReference.OwnerNetworkEntity, out responseMessage);
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
                                    this.DisconnectClient(managedMessageReference.OwnerNetworkEntity, new KSPMEventArgs(KSPMEventArgs.EventType.Disconnect, KSPMEventArgs.EventCause.NiceDisconnect));
                                    break;
                                case Message.CommandType.User:
                                    ///Raising the TCP message event.
                                    this.OnTCPMessageArrived(managedMessageReference.OwnerNetworkEntity, managedMessageReference);
                                    break;
                                case Message.CommandType.Unknown:
                                default:
                                    KSPMGlobals.Globals.Log.WriteTo("Prioritized Queue - Unknown command: " + messageToProcess.Command.ToString());
                                    break;
                            }

                            ///Recyles and releases the message.
                            this.priorityMessagesPool.Recycle(messageToProcess);
                        }

                    Thread.Sleep(1);
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
                KSPMGlobals.Globals.Log.WriteTo("-Starting to handle priority outgoing messages[ " + this.alive + " ]");
                while (this.alive)
                {
                        this.priorityOutgoingMessagesQueue.DequeueCommandMessage(out outgoingMessage);
                        if (outgoingMessage != null)
                        {
                            ///Writing the message Id to the message itself.
                            outgoingMessage.MessageId = (uint)System.Threading.Interlocked.Increment(ref Message.MessageCounter);
                            System.Buffer.BlockCopy(System.BitConverter.GetBytes(outgoingMessage.MessageId), 0, outgoingMessage.bodyMessage, (int)PacketHandler.PrefixSize, 4);

#if DEBUGTRACER_L2
                            KSPMGlobals.Globals.Log.WriteTo(string.Format("GameServer.HandleOutgoingPriorityMessagesThreadMethod -> {0}", outgoingMessage.ToString()));
#endif
                            managedReference = (ManagedMessage)outgoingMessage;
                            try
                            {
                                ///Checking if the NetworkEntity is still running.
                                if (managedReference.OwnerNetworkEntity.IsAlive())
                                {
                                    ///Writing the outgoing message command.
                                    managedReference.OwnerNetworkEntity.ownerNetworkCollection.socketReference.BeginSend(outgoingMessage.bodyMessage, 0, (int)outgoingMessage.MessageBytesSize, SocketFlags.None, new AsyncCallback(this.AsyncSenderCallback), managedReference.OwnerNetworkEntity);
                                }
                            }
                            catch (System.Exception ex)
                            {
                                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}][\"{1}-{2}\"] Something went wrong with the remote client, performing a removing process on it.", managedReference.OwnerNetworkEntity.Id, "HandleOutgoingPriorityMessages", ex.Message));
                                this.DisconnectClient(managedReference.OwnerNetworkEntity, new KSPMEventArgs(KSPMEventArgs.EventType.RuntimeError, KSPMEventArgs.EventCause.ErrorByException));
                            }
                            finally
                            {
                                ///Releasing the processed command.
                                outgoingMessage.Release();
                                outgoingMessage = null;
                            }
                        }
                    Thread.Sleep(1);
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

        #endregion

        #region UserManagement

        /// <summary>
        /// Event raised when an user has connected to the server.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void OnUserDisconnected( NetworkEntity sender, KSPMEventArgs e)
        {
            if (this.UserDisconnected != null)
            {
                this.UserDisconnected(sender, e);
            }
        }

        /// <summary>
        /// Method called each time an UDP message is received, if there is no attached method  to the event, the message is recycled.
        /// </summary>
        /// <param name="sender">Network entity who has received the message.</param>
        /// <param name="message">Reference to the received message.</param>
        protected internal void OnUDPMessageArrived(NetworkEntity sender, RawMessage message)
        {
            if (this.UDPMessageArrived != null)
            {
                this.UDPMessageArrived(sender, message);
            }
        }

        /// <summary>
        /// Internal method used to disconnect clients, also raises the OnUserDisconnected event.
        /// </summary>
        /// <param name="target">NetworkEntity to whom is going be applyed the ban hammer.</param>
        /// <param name="cause">Information about what was the cause of the disconnection.</param>
        internal void DisconnectClient(NetworkEntity target, KSPMEventArgs cause)
        {
            if (target != null && target.IsAlive() && !target.markedToDie)
            {
                target.markedToDie = true;
#if DEBUGTRACER_L2
                KSPMGlobals.Globals.Log.WriteTo(string.Format("GameServer.DisconnectClient(NetworkEntity target, KSPMEventArgs cause) -> {0}", target.Id));
#endif
                this.OnUserDisconnected(target, cause);
                this.chatManager.UnregisterUser(target);
                this.clientsHandler.RemoveClient(target);
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

        /// <summary>
        /// Returns an array of NetworkEntities connected to the server.
        /// </summary>
        public NetworkEntity[] ConnectedClients
        {
            get
            {
                return this.clientsHandler.RemoteClients.ToArray();
            }
        }

        /// <summary>
        /// Returns the ClientManager reference used by the server.
        /// </summary>
        public ClientsHandler ClientsManager
        {
            get
            {
                return this.clientsHandler;
            }
        }

        /// <summary>
        /// Disconnect every connected user.<b>This method will not fire the Disconnect event attached to each member.</b>
        /// </summary>
        public void DisconnectAll()
        {
            this.clientsHandler.DisconnectAll();
            this.chatManager.UnregisterAllUsers();
        }

        #endregion

        #region Setters/Getters

        /// <summary>
        /// Tells if the server is still running.
        /// </summary>
        public bool IsAlive
        {
            get
            {
                return this.alive;
            }
        }

        /// <summary>
        /// Gets the warning level flag on what the system is working.
        /// </summary>
        public KSPMSystem.WarningLevel WarningLevel
        {
            get
            {
                if (this.primaryCommandQueue != null)
                    return (KSPMSystem.WarningLevel)this.primaryCommandQueue.WarningFlagLevel;
                else
                    return KSPMSystem.WarningLevel.None;
            }
        }

        #endregion
    }
}
