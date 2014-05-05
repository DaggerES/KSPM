//#define PROFILING

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

        protected SocketAsyncEventArgsPool incomingConnectionsPool;

        #endregion

        /// <summary>
        /// Settings to operate at low level, like listening ports and the like.
        /// </summary>
        protected ServerSettings lowLevelOperationSettings;

        #region Commands code

        /// <summary>
        /// Holds the commands to be processed by de server, like the command chat.
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


        #endregion

        #region Threading code

        protected Thread commandsThread;
        protected Thread outgoingMessagesThread;
        protected Thread localCommandsThread;
        protected Thread priorityOutgoingMessagesThread;

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

        public event UDPMessageArrived UDPMessageArrived;

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

#if PROFILING
            this.profilerOutgoingMessages = new Profiler("OutgoingMessages");
#endif

            this.lowLevelOperationSettings = operationSettings;
            if (this.lowLevelOperationSettings == null)
            {
                this.ableToRun = false;
                return;
            }

            this.tcpBuffer = new byte[ServerSettings.ServerBufferSize];

            ///Creating a new buffered CommandQueue capable to suport upto 1000 messages, each one of 1024 bytes length.
            this.commandsQueue = new BufferedCommandQueue((uint)ServerSettings.ServerBufferSize * 1000);
            ///Creating the local commands queue, capable to hold up to 100 messages, each one of 1024 bytes length.
            this.localCommandsQueue = new BufferedCommandQueue((uint)ServerSettings.ServerBufferSize * 100);
            this.priorityOutgoingMessagesQueue = new CommandQueue();
            this.outgoingMessagesQueue = new CommandQueue();

            this.incomingMessagesPool = new MessagesPool(2000, new BufferedMessage(Message.CommandType.Null, 0, 0));
            this.priorityMessagesPool = new MessagesPool(100, new BufferedMessage(Message.CommandType.Null, 0, 0));

            this.commandsThread = new Thread(new ThreadStart(this.HandleCommandsThreadMethod));
            this.outgoingMessagesThread = new Thread(new ThreadStart(this.HandleOutgoingMessagesThreadMethod));
            this.localCommandsThread = new Thread(new ThreadStart(this.HandleLocalCommandsThreadMethod));
            this.priorityOutgoingMessagesThread = new Thread(new ThreadStart(this.HandleOutgoingPriorityMessagesThreadMethod));

            this.defaultUserManagementSystem = new LowlevelUserManagmentSystem();
            this.clientsHandler = new ClientsHandler();

            ///It still missing the filter
            this.usersAccountManager = new AccountManager();

            this.chatManager = new ChatManager(ChatManager.DefaultStorageMode.NonPersistent);

            this.incomingConnectionsPool = new SocketAsyncEventArgsPool((uint)this.lowLevelOperationSettings.connectionsBackog);

            this.ableToRun = true;
            this.alive = false;
        }

        /// <summary>
        /// Gets 
        /// </summary>
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
                this.commandsThread.Start();
                this.outgoingMessagesThread.Start();
                this.localCommandsThread.Start();
                this.priorityOutgoingMessagesThread.Start();

                this.tcpSocket.Listen(this.lowLevelOperationSettings.connectionsBackog);
                KSPMGlobals.Globals.Log.WriteTo("-Starting to handle conenctions[ " + this.alive + " ]");
                this.StartReceiveConnections();
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
                this.tcpSocket.Shutdown(SocketShutdown.Both);
                this.tcpSocket.Close();
            }
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
            this.commandsQueue.Purge(false);
            this.outgoingMessagesQueue.Purge(false);
            this.localCommandsQueue.Purge(false);
            this.priorityOutgoingMessagesQueue.Purge(false);
            this.commandsQueue = null;
            this.localCommandsQueue = null;
            this.outgoingMessagesQueue = null;
            this.priorityOutgoingMessagesQueue = null;

            this.priorityMessagesPool.Release();
            this.incomingMessagesPool.Release();

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

            ///Returns false if the process was completed synchrounosly.
            if (!this.tcpSocket.AcceptAsync(incomingConnection))
            {
                this.OnAsyncIncomingConnectionComplete(this, incomingConnection);
            }
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
                ///If something fails, we started to receive another conn.
                this.StartReceiveConnections();
            }
            ///Restoring the AsyncEventArgs used to perform the connection process.
            e.Completed -= this.OnAsyncIncomingConnectionComplete;
            this.incomingConnectionsPool.Recycle(e);
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

        public void ProcessPacket(byte[] rawData, uint rawDataOffset, uint fixedLength, NetworkEntity packetOwner)
        {
            Message incomingMessage = null;
            incomingMessage = this.priorityMessagesPool.BorrowMessage;
            ((BufferedMessage)incomingMessage).Load(rawData, rawDataOffset, fixedLength);
            ((BufferedMessage)incomingMessage).SetOwnerMessageNetworkEntity(packetOwner);
            if (!this.localCommandsQueue.EnqueueCommandMessage(ref incomingMessage))
            {
                this.priorityMessagesPool.Recycle(incomingMessage);
            }
        }

        #endregion

        /// <summary>
        /// Handles the those commands send by the client through a TCP socket.
        /// </summary>
        protected void HandleCommandsThreadMethod()
        {
            Message messageToProcess = null;
            ManagedMessage managedMessageReference = null;
            if (!this.ableToRun)
            {
                KSPMGlobals.Globals.Log.WriteTo(Error.ErrorType.ServerUnableToRun.ToString());
            }
            try
            {
                KSPMGlobals.Globals.Log.WriteTo("-Starting to handle commands[ " + this.alive + " ]");
                while (this.alive)
                {
                    this.commandsQueue.DequeueCommandMessage(out messageToProcess);
                    if (messageToProcess != null)
                    {
                        managedMessageReference = (ManagedMessage)messageToProcess;
                        switch (messageToProcess.Command)
                        {
                            case Message.CommandType.Chat:
                                this.clientsHandler.TCPBroadcastTo(this.chatManager.GetChatGroupById(ChatMessage.InflateTargetGroupId(messageToProcess.bodyMessage)).MembersAsList, messageToProcess);
                                /*
                                if (ChatMessage.InflateChatMessage(messageToProcess.bodyMessage, out chatMessage) == Error.ErrorType.Ok)
                                {
                                    //KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}][{1}_{2}]-Says:{3}", managedMessageReference.OwnerNetworkEntity.Id, chatMessage.Time.ToShortTimeString(), chatMessage.sendersUsername, chatMessage.Body));
                                    this.clientsHandler.TCPBroadcastTo(this.chatManager.AttachMessage(chatMessage).MembersAsList, messageToProcess);
                                }
                                */
                                break;
                            case Message.CommandType.KeepAlive:
                                KSPMGlobals.Globals.Log.WriteTo("KeepAlive command: " + messageToProcess.Command.ToString());
                                break;
                            case Message.CommandType.Unknown:
                            default:
                                KSPMGlobals.Globals.Log.WriteTo("Unknown command: " + messageToProcess.Command.ToString());
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
                            //KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}]===Error==={1}.", outgoingMessage.bodyMessage[ 4 ], outgoingMessage.Command));
                            try
                            {
                                ///If it is broadcaste message a different sending procces is performed.
                                if (outgoingMessage.IsBroadcast)
                                {
                                    broadcastReference = (BroadcastMessage)outgoingMessage;
                                    for (entityCounter = 0; entityCounter < broadcastReference.Targets.Length; entityCounter++)
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
                                                //sendingData.Completed += new EventHandler<SocketAsyncEventArgs>(this.OnSendingOutgoingDataComplete);
                                                if (!broadcastReference.Targets[entityCounter].ownerNetworkCollection.socketReference.SendAsync(sendingData))
                                                {
                                                    this.OnSendingOutgoingDataComplete(this, sendingData);
                                                }
                                            }
                                            else
                                            {
                                                KSPMGlobals.Globals.Log.WriteTo("PACKET CRC ERROR, Avoiding it.");
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
                                            //sendingData.Completed += new EventHandler<SocketAsyncEventArgs>(this.OnSendingOutgoingDataComplete);
                                            if (!managedReference.OwnerNetworkEntity.ownerNetworkCollection.socketReference.SendAsync(sendingData))
                                            {
                                                this.OnSendingOutgoingDataComplete(this, sendingData);
                                            }
                                            //managedReference.OwnerNetworkEntity.ownerNetworkCollection.socketReference.BeginSend(outgoingMessage.bodyMessage, 0, (int)outgoingMessage.MessageBytesSize, SocketFlags.None, new AsyncCallback(this.AsyncSenderCallback), managedReference.OwnerNetworkEntity);
                                        }
                                    }
                                    else
                                    {
                                        KSPMGlobals.Globals.Log.WriteTo("PACKET CRC ERROR, Avoiding it.");
                                    }
                                }
                            }
                            catch (System.Exception ex)
                            {
                                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}][\"{1}-{2}\"] Something went wrong with the remote client, performing a removing process on it.", (outgoingMessage.IsBroadcast ? ((BroadcastMessage)outgoingMessage).Targets[entityCounter].Id : ((ManagedMessage)outgoingMessage).OwnerNetworkEntity.Id), "HandleOutgoingMessages", ex.Message));
                                this.DisconnectClient((outgoingMessage.IsBroadcast ? ((BroadcastMessage)outgoingMessage).Targets[entityCounter] : ((ManagedMessage)outgoingMessage).OwnerNetworkEntity));
                            }
                            finally
                            {
                                ///Cleaning up.
                                outgoingMessage.Release();
                                outgoingMessage = null;
                            }
                        }
                        else
                        {
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
            e.Completed -= this.OnSendingOutgoingDataComplete;

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
                            //KSPMGlobals.Globals.Log.WriteTo(messageToProcess.Command.ToString());
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
                                    if (this.usersAccountManager.Query(managedMessageReference.OwnerNetworkEntity))
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
                                    this.DisconnectClient(managedMessageReference.OwnerNetworkEntity);
                                    break;
                                case Message.CommandType.Unknown:
                                default:
                                    KSPMGlobals.Globals.Log.WriteTo("Unknown command: " + messageToProcess.Command.ToString());
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
                KSPMGlobals.Globals.Log.WriteTo("-Starting to handle outgoing messages[ " + this.alive + " ]");
                while (this.alive)
                {
                        this.priorityOutgoingMessagesQueue.DequeueCommandMessage(out outgoingMessage);
                        if (outgoingMessage != null)
                        {
                            //KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}]===Error==={1}.", outgoingMessage.bodyMessage[ 4 ], outgoingMessage.Command));
                            managedReference = (ManagedMessage)outgoingMessage;
                            try
                            {
                                ///Checking if the NetworkEntity is still running.
                                if (managedReference.OwnerNetworkEntity.IsAlive())
                                {
                                    KSPMGlobals.Globals.Log.WriteTo(outgoingMessage.Command.ToString());
                                    managedReference.OwnerNetworkEntity.ownerNetworkCollection.socketReference.BeginSend(outgoingMessage.bodyMessage, 0, (int)outgoingMessage.MessageBytesSize, SocketFlags.None, new AsyncCallback(this.AsyncSenderCallback), managedReference.OwnerNetworkEntity);
                                }
                            }
                            catch (System.Exception ex)
                            {
                                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}][\"{1}-{2}\"] Something went wrong with the remote client, performing a removing process on it.", managedReference.OwnerNetworkEntity.Id, "HandleOutgoingPriorityMessages", ex.Message));
                                this.DisconnectClient(managedReference.OwnerNetworkEntity);
                            }

                            ///Releasing the processed command.
                            outgoingMessage.Release();
                            outgoingMessage = null;
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
                KSPMGlobals.Globals.Log.WriteTo(sentBytes.ToString());
				if( sentBytes > 0 )
				{
                	net.MessageSent(net, null);
				}
            }
            catch (System.Exception)
            {
            }
        }

        protected void HandleUDPSend()
        {
            if (!this.ableToRun)
            {
                KSPMGlobals.Globals.Log.WriteTo(Error.ErrorType.ServerUnableToRun.ToString());
            }
            try
            {
                KSPMGlobals.Globals.Log.WriteTo("-Starting to handle udp send[ " + this.alive + " ]");
                while (this.alive)
                {
                    
                    Thread.Sleep(5);
                }
            }
            catch (ThreadAbortException)
            {
                this.alive = false;
            }
        }

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
        internal void DisconnectClient(NetworkEntity target)
        {
            if (target != null && target.IsAlive())
            {
                this.OnUserDisconnected(target, null);
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

        public ClientsHandler ClientsManager
        {
            get
            {
                return this.clientsHandler;
            }
        }

        #endregion
    }
}
