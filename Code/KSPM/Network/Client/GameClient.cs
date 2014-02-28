using System.Net.Sockets;
using System.Net;

using KSPM.Network.Common;
using KSPM.Network.Common.Messages;
using KSPM.Network.Common.Packet;
using KSPM.Game;
using KSPM.Globals;
using KSPM.Network.NAT;

using System.Threading;

namespace KSPM.Network.Client
{
    /// <summary>
    /// Class to represent the remote client.
    /// </summary>
    public class GameClient : NetworkEntity, IAsyncTCPReceiver, IAsyncTCPSender
    {
        public enum ClientStatus : byte { None = 0, Rebind, Handshaking, Authenticating, UDPSettingUp, Awaiting, Connected};

        public ClientSettings workingSettings;

        /// <summary>
        /// ManualResetEvent reference to manage the signaling among the threads and the async methods.
        /// </summary>
        protected readonly ManualResetEvent TCPSignalHandler = new ManualResetEvent(false);

        /// <summary>
        /// Tells the current status of the client.
        /// </summary>
        protected ClientStatus currentStatus;

        /// <summary>
        /// Reference to the game user which is using the multiplayer, and from whom its information would be get. <b>This is not released when client is closed.</b>
        /// </summary>
        protected GameUser clientOwner;

        /// <summary>
        /// Holds information requiered to stablish a connection to the PC who is hosting the game server. <b>This is not released when the client is closed.</b>
        /// </summary>
        protected ServerInformation gameServerInformation;
        
        /// <summary>
        /// Tells if the server needs to reassign a new address to the game. <b>It means to use a free port.</b>
        /// </summary>
        protected bool reassignAddress;

        /// <summary>
        /// Flag to tells if everything has been set up and the client is ready to run.
        /// </summary>
        protected bool ableToRun;

        /// <summary>
        /// Flag to control the client's life cycle.
        /// </summary>
        protected bool aliveFlag;

        /// <summary>
        /// Tells if the socket is already connected after the NAT traversing method.
        /// </summary>
        protected bool holePunched;

        /// <summary>
        /// Thread safe Queue to hold all the commands.
        /// </summary>
        protected CommandQueue commandsQueue;

        /// <summary>
        /// Thread safe Queue to hold all the outgoing messages.
        /// </summary>
        protected CommandQueue outgoingTCPMessages;

        /// <summary>
        /// Thread to handle the main body of the client.
        /// </summary>
        protected Thread mainBodyThread;

        /// <summary>
        /// Thread to handle the outgoing TCP messages.
        /// </summary>
        protected Thread handleOutgoingTCPMessagesThread;

        /// <summary>
        /// Thread to handle the incoming TCP messages.
        /// </summary>
        protected Thread handleIncomingTCPMessagesThread;

        /// <summary>
        /// Server information to connect through UDP
        /// </summary>
        protected ServerInformation udpServerInformation;

        /// <summary>
        /// Pairing code.
        /// </summary>
        protected int pairingCode;

        /// <summary>
        /// Creates a GameClient reference a initialize some properties.
        /// </summary>
        public GameClient() : base()
        {
            this.reassignAddress= false;
            this.clientOwner = null;
            this.gameServerInformation = null;
            this.ableToRun = false;
            this.aliveFlag = false;
            this.holePunched = false;

            this.workingSettings = null;

            this.currentStatus = ClientStatus.None;

            ///Threading code
            this.mainBodyThread = new Thread(new ThreadStart(this.HandleMainBodyThreadMethod));
            this.handleOutgoingTCPMessagesThread = new Thread(new ThreadStart(this.HandleOutgoingTCPMessagesThreadMethod));
            this.handleIncomingTCPMessagesThread = new Thread(new ThreadStart(this.HandleIncomingTCPMessagesThreadMethod));

            this.commandsQueue = new CommandQueue();
            this.outgoingTCPMessages = new CommandQueue();

            ///Initializing the TCP Network
            this.ownerNetworkCollection = new NetworkBaseCollection(ClientSettings.ClientBufferSize);

            ///Network init
            this.udpServerInformation = new ServerInformation();
        }

        /// <summary>
        /// Sets the GameUser reference from where is going to be taken the required information to the authentication process.
        /// </summary>
        /// <param name="gameUserReference"></param>
        public void SetGameUser(GameUser gameUserReference)
        {
            this.clientOwner = gameUserReference;
        }

        /// <summary>
        /// Sets the server information from where is going to be taken the required information to the connect process.
        /// </summary>
        /// <param name="hostInformation"></param>
        public void SetServerHostInformation(ServerInformation hostInformation)
        {
            if (!hostInformation.Equals(this.gameServerInformation))
            {
                this.gameServerInformation = hostInformation;
            }
        }

        /// <summary>
        /// Initializes everything needed to work, as threads.
        /// </summary>
        /// <returns>Ok if everything went fine, and ClientUnableToRun if an error ocurred.</returns>
        public Error.ErrorType InitializeClient()
        {
            Error.ErrorType result = Error.ErrorType.Ok;
            try
            {
                this.aliveFlag = true;
                this.ableToRun = true;
                this.holePunched = false;

                this.currentStatus = ClientStatus.None;

                ///If Settings could not be loaded properly, a default one is created and tried to write it.
                ///If the writer could not be able to write down the settings, a message is showed into the log, but this not break the game because the settings references
                ///is created.
                result = ClientSettings.ReadSettings(out this.workingSettings);

                this.mainBodyThread.Start();
                this.handleOutgoingTCPMessagesThread.Start();
                this.handleIncomingTCPMessagesThread.Start();
            }
            catch (System.Exception ex)
            {
                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Something went wrong, shutting down...", this.id));
                KSPMGlobals.Globals.Log.WriteTo(ex.Message);
                this.ShutdownClient();
                this.aliveFlag = false;
                result = Error.ErrorType.ClientUnableToRun;
            }
            return result;
        }

        /// <summary>
        /// Tries a connection with the specified server and the given gameuser.<b>The hole punching process runs in other thread.</b>.
        /// </summary>
        /// <returns></returns>
        public Error.ErrorType Connect()
        {
            Thread connectThread;
            if (!this.ableToRun)
            {
                KSPMGlobals.Globals.Log.WriteTo(Error.ErrorType.ClientUnableToRun.ToString());
                return Error.ErrorType.ClientUnableToRun;
            }

            if (this.currentStatus != ClientStatus.None)
            {
                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Client already connected...", this.id));
                return Error.ErrorType.Ok;
            }

            ///Checking if the required information is already provided.
            if (this.clientOwner == null)
            {
                KSPMGlobals.Globals.Log.WriteTo(Error.ErrorType.ClientInvalidGameUser.ToString());
                return Error.ErrorType.ClientInvalidGameUser;
            }
            if (this.gameServerInformation == null)
            {
                KSPMGlobals.Globals.Log.WriteTo(Error.ErrorType.ClientInvalidServerInformation.ToString());
                return Error.ErrorType.ClientInvalidServerInformation;
            }

            ///Creating sockets and connecting and setting up everything
            try
            {
                this.ownerNetworkCollection.socketReference = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                this.ownerNetworkCollection.socketReference.LingerState = new LingerOption(false, 0);
                this.ownerNetworkCollection.socketReference.NoDelay = true;

                ///Avoiding to bind the same Address and port.
                if (!this.reassignAddress)
                {
                    this.ownerNetworkCollection.socketReference.Bind(new IPEndPoint(IPAddress.Any, (int)this.workingSettings.tcpPort));
                }
                connectThread = new Thread(new ThreadStart(this.HandleConnectThreadMethod));
                this.currentStatus = ClientStatus.Handshaking;
                this.udpServerInformation.Dispose();
                this.outgoingTCPMessages.Purge(true);
                this.commandsQueue.Purge(true);
                connectThread.Start();
                if (!connectThread.Join((int)this.workingSettings.connectionTimeout))
                {
                    ///Killing the thread because it has taken too much.
                    connectThread.Abort();
                    KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Connection timeout...", this.id));
                    this.BreakConnections(this, null);
                }
                if (!this.holePunched)
                {
                    return Error.ErrorType.ClientUnableToConnect;
                }
            }
            catch (System.Exception ex)
            {
                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Something went wrong, shutting down...", this.id));
                KSPMGlobals.Globals.Log.WriteTo(ex.Message);
                this.ShutdownClient();
                this.aliveFlag = false;
                return Error.ErrorType.ClientUnableToRun;
            }
            return Error.ErrorType.Ok;
        }

        /// <summary>
        /// Threaded method which handles the connect process.
        /// </summary>
        protected void HandleConnectThreadMethod()
        {
            bool connected = false;
            Message outgoingMessage = null;
            ManagedMessage managedMessageReference = null;
            Error.ErrorType connectingError = Error.ErrorType.Ok;
            this.holePunched = false;
            try
            {
                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Starting to connect...", this.id));
                connectingError = KSPMGlobals.Globals.NAT.Punch(ref this.ownerNetworkCollection.socketReference, this.gameServerInformation.ip, this.gameServerInformation.port);
                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] {1}...", this.id, KSPMGlobals.Globals.NAT.Status.ToString()));
                this.holePunched = KSPMGlobals.Globals.NAT.Status == NATTraversal.NATStatus.Connected;
                if (!this.holePunched)
                {
                    KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Hole punching can not be done, reassigning connection... Try again", this.id));
                    this.reassignAddress = true;
                    this.BreakConnections(this, null);
                    return;
                }
                this.reassignAddress = false;
                while (!connected)
                {
                    switch (this.currentStatus)
                    {
                        case ClientStatus.Handshaking:
                            Message.NewUserMessage(this, out outgoingMessage);
                            managedMessageReference = (ManagedMessage)outgoingMessage;
                            PacketHandler.EncodeRawPacket(ref managedMessageReference.OwnerNetworkEntity.ownerNetworkCollection.rawBuffer);
                            this.outgoingTCPMessages.EnqueueCommandMessage(ref outgoingMessage);
                            this.currentStatus = ClientStatus.Awaiting;
                            break;
                        case ClientStatus.Authenticating:
                            this.AuthenticateClient(this, null);
                            this.currentStatus = ClientStatus.Awaiting;
                            break;
                        case ClientStatus.UDPSettingUp:
                            connected = true;
                            this.currentStatus = ClientStatus.Connected;
                            break;
                        case ClientStatus.Awaiting:
                            Thread.Sleep(11);
                            break;
                    }
                    Thread.Sleep(11);
                }
            }
            catch (ThreadAbortException)
            {
                connected = true;
            }
        }

        /// <summary>
        /// Disconnects from the current server.
        /// </summary>
        public void Disconnect()
        {
            if (this.holePunched)
            {
                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Disconnecting...", this.id));
                Message disconnectMessage = null;
                this.outgoingTCPMessages.Purge(true);
                this.commandsQueue.Purge(true);

                Message.DisconnectMessage(this, out disconnectMessage);
                PacketHandler.EncodeRawPacket(ref this.ownerNetworkCollection.rawBuffer);
                this.SetMessageSentCallback(this.BreakConnections);
                this.outgoingTCPMessages.EnqueueCommandMessage(ref disconnectMessage);
            }
        }

        /// <summary>
        /// Threaded method to handle the commands on the main queue.
        /// </summary>
        protected void HandleMainBodyThreadMethod()
        {
            Message command = null;
            ManagedMessage managedMessageReference = null;
            if (!this.ableToRun)
            {
                KSPMGlobals.Globals.Log.WriteTo(Error.ErrorType.ClientUnableToRun.ToString());
                return;
            }
            try
            {
                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Starting to handle the main body...", this.id));
                while (this.aliveFlag)
                {
                    if (!this.commandsQueue.IsEmpty())
                    {
                        this.commandsQueue.DequeueCommandMessage(out command);
                        if (command != null)
                        {
                            switch (command.Command)
                            {
                                case Message.CommandType.Handshake:///NewClient command accepted, proceed to authenticate.
                                    this.currentStatus = ClientStatus.Authenticating;
                                    break;
                                case Message.CommandType.AuthenticationSuccess:
                                    ///Does nothing.
                                    break;
                                case Message.CommandType.AuthenticationFail:///Running the autentication procces again.
                                    this.currentStatus = ClientStatus.Authenticating;
                                    break;
                                case Message.CommandType.UDPSettingUp:///Create the UDP conn.
                                                                      ///Reads the information sent by the server and starts the UDP setting up process.
                                    managedMessageReference = (ManagedMessage)command;
                                    this.udpServerInformation.port = System.BitConverter.ToInt32(managedMessageReference.OwnerNetworkEntity.ownerNetworkCollection.secondaryRawBuffer, 5);
                                    this.udpServerInformation.ip = ((IPEndPoint)managedMessageReference.OwnerNetworkEntity.ownerNetworkCollection.socketReference.RemoteEndPoint).Address.ToString();
                                    this.pairingCode = System.BitConverter.ToInt32(managedMessageReference.OwnerNetworkEntity.ownerNetworkCollection.secondaryRawBuffer, 9);
                                    this.currentStatus = ClientStatus.UDPSettingUp;
                                    break;
                            }
                        }

                        ///Cleaning up.
                        command.Release();
                        command = null;
                    }
                    Thread.Sleep(5);
                }
            }
            catch (ThreadAbortException)
            {
                this.aliveFlag = false;
            }
        }

        /// <summary>
        /// Handles the outgoing messages through the TCP connection.
        /// </summary>
        protected void HandleOutgoingTCPMessagesThreadMethod()
        {
            Message outgoingMessage = null;
            if (!this.ableToRun)
            {
                KSPMGlobals.Globals.Log.WriteTo(Error.ErrorType.ClientUnableToRun.ToString());
                return;
            }
            try
            {
                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Starting to handle outgoing messages...", this.id));
                while (this.aliveFlag)
                {
                    if (this.holePunched)
                    {
                        if (!this.outgoingTCPMessages.IsEmpty())
                        {
                            this.outgoingTCPMessages.DequeueCommandMessage(out outgoingMessage);
                            if (outgoingMessage != null)
                            {
                                this.ownerNetworkCollection.socketReference.BeginSend(this.ownerNetworkCollection.rawBuffer, 0, (int)outgoingMessage.MessageBytesSize, SocketFlags.None, this.AsyncTCPSender, this);
                            }
                            ///Cleaning up
                            outgoingMessage.Release();
                            outgoingMessage = null;
                        }
                    }
                    Thread.Sleep(7);
                }
            }
            catch (ThreadAbortException)
            {
                this.aliveFlag = false;
            }
        }

        /// <summary>
        /// Handles the incoming messages coming from the TCP connection.
        /// </summary>
        protected void HandleIncomingTCPMessagesThreadMethod()
        {
            if (!this.ableToRun)
            {
                KSPMGlobals.Globals.Log.WriteTo(Error.ErrorType.ClientUnableToRun.ToString());
                return;
            }
            try
            {
                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Starting to handle incoming TCP messages...", this.id));
                while (this.aliveFlag)
                {
                    if (this.holePunched)
                    {
                        this.TCPSignalHandler.Reset();
                        this.ownerNetworkCollection.socketReference.BeginReceive(this.ownerNetworkCollection.secondaryRawBuffer, 0, this.ownerNetworkCollection.secondaryRawBuffer.Length, SocketFlags.None, this.AsyncTCPReceiver, this);
                        this.TCPSignalHandler.WaitOne();
                    }
                    Thread.Sleep(11);
                }
            }
            catch (ThreadAbortException)
            {
                this.aliveFlag = false;
            }
        }

        /// <summary>
        /// Receives all the TCP packets and then create a message to enqueue into the commands queue.
        /// </summary>
        /// <param name="result"></param>
        public void AsyncTCPReceiver(System.IAsyncResult result)
        {
            this.TCPSignalHandler.Set();
            int readBytes;
            Message incomingMessage = null;
            try
            {
                NetworkEntity callingEntity = (NetworkEntity)result.AsyncState;
                readBytes = callingEntity.ownerNetworkCollection.socketReference.EndReceive(result);
                if (readBytes > 0)
                {
                    if (PacketHandler.DecodeRawPacket(ref callingEntity.ownerNetworkCollection.secondaryRawBuffer) == Error.ErrorType.Ok)
                    {
                        if (PacketHandler.InflateManagedMessage(callingEntity, out incomingMessage) == Error.ErrorType.Ok)
                        {
                            this.commandsQueue.EnqueueCommandMessage(ref incomingMessage);
                        }
                    }
                }
            }
            catch (System.Exception)///Catch any exception thrown by the Socket.EndReceive method, mostly the ObjectDisposedException which is thrown when the thread is aborted and the socket is closed.
            {
            }
        }

        /// <summary>
        /// Sends asynchronously TCP packets.
        /// </summary>
        /// <param name="result"></param>
        public void AsyncTCPSender(System.IAsyncResult result)
        {
            int sentBytes;
            NetworkEntity networkReference = null;
            try
            {
                networkReference = (NetworkEntity)result.AsyncState;
                sentBytes = networkReference.ownerNetworkCollection.socketReference.EndSend(result);
                networkReference.MessageSent(networkReference, null);
            }
            catch (System.Exception)///Catch any exception thrown by the Socket.EndReceive method, mostly the ObjectDisposedException which is thrown when the thread is aborted and the socket is closed.
            {
            }
        }

        /// <summary>
        /// Overrides Release method inherited from NetworkEntity, this method shutdowns the client.<b>Making it unable to run again, unless you create a new instance.</b>
        /// </summary>
        public override void Release()
        {
            this.ShutdownClient();
        }

        /// <summary>
        /// Shutdowns everything on the client.
        /// </summary>
        protected void ShutdownClient()
        {
            ///Avoiding to shutdown the client twice or more.
            if (!this.aliveFlag)
                return;
            this.ableToRun = false;
            this.aliveFlag = false;
            this.holePunched = false;

            this.currentStatus = ClientStatus.None;

            ///*****************Killing threads.
            this.mainBodyThread.Abort();
            this.mainBodyThread.Join();
            KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Killed mainthread...", this.id));

            this.handleOutgoingTCPMessagesThread.Abort();
            this.handleOutgoingTCPMessagesThread.Join();
            KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Killed handledOutgoingTCPThread...", this.id));

            this.handleIncomingTCPMessagesThread.Abort();
            this.handleIncomingTCPMessagesThread.Join();
            KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Killed handledIncomingTCPThread...", this.id));

            this.mainBodyThread = null;
            this.handleOutgoingTCPMessagesThread = null;
            this.handleIncomingTCPMessagesThread = null;

            ///***********************Sockets code
            if (this.ownerNetworkCollection.socketReference != null && this.ownerNetworkCollection.socketReference.Connected)
            {
                this.ownerNetworkCollection.socketReference.Disconnect(true);
                this.ownerNetworkCollection.socketReference.Close();
            }
            this.ownerNetworkCollection.Dispose();
            this.ownerNetworkCollection = null;

            this.clientOwner = null;

            ///*********************Releasing server information objects.
            this.udpServerInformation.Dispose();
            this.udpServerInformation = null;

            ///**********************Cleaning up the messages's Queues.
            this.commandsQueue.Purge(false);
            this.outgoingTCPMessages.Purge(false);

            KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Client killed!!!", this.id));
        }

        protected void BreakConnections(NetworkEntity caller, object arg)
        {
            ///***********************Sockets code
            this.holePunched = false;
            if (this.ownerNetworkCollection.socketReference != null && this.ownerNetworkCollection.socketReference.Connected)
            {
                this.ownerNetworkCollection.socketReference.Shutdown(SocketShutdown.Both);
                this.ownerNetworkCollection.socketReference.Disconnect(true);
            }

            ///Closing the socket either it is connected or not.
            this.ownerNetworkCollection.socketReference.Close();
            this.ownerNetworkCollection.socketReference = null;

            this.clientOwner = null;

            //this.gameServerInformation = null;

            ///*********************Releasing server information objects.
            this.udpServerInformation.Dispose();

            ///**********************Cleaning up the messages's Queues.
            this.commandsQueue.Purge(true);
            this.outgoingTCPMessages.Purge(true);

            this.currentStatus = ClientStatus.None;
            KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Disconnected...", this.id));
        }

        /// <summary>
        /// Sends Authentication message to the server.
        /// </summary>
        /// <param name="caller"></param>
        /// <param name="arg"></param>
        protected void AuthenticateClient(NetworkEntity caller, object arg)
        {
            Message authenticationMessage = null;
            ManagedMessage managedMessageReference = null;

            Message.AuthenticationMessage(caller, this.clientOwner, out authenticationMessage);
            managedMessageReference = (ManagedMessage)authenticationMessage;
            PacketHandler.EncodeRawPacket(ref managedMessageReference.OwnerNetworkEntity.ownerNetworkCollection.rawBuffer);
            this.outgoingTCPMessages.EnqueueCommandMessage(ref authenticationMessage);
        }
    }
}
