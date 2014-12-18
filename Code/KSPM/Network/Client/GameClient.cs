//#define DEBUG_PRINT
using System.Net.Sockets;
using System.Net;

using KSPM.Network.Common;
using KSPM.Network.Common.MessageHandlers;
using KSPM.Network.Common.Messages;
using KSPM.Network.Common.Packet;
using KSPM.Game;
using KSPM.Globals;
using KSPM.Network.NAT;
using KSPM.Network.Common.Events;
using KSPM.Network.Client.RemoteServer;
using KSPM.Network.Chat.Managers;
using KSPM.Network.Chat.Messages;
using KSPM.Network.Chat.Group;

using System.Threading;

namespace KSPM.Network.Client
{
    /// <summary>
    /// Class to represent the remote client.
    /// </summary>
    public class GameClient : NetworkEntity, IPacketArrived, IUDPPacketArrived
    {
        /// <summary>
        /// Set of available states in which the GameClient could be.
        /// </summary>
        public enum ClientStatus : byte 
        { 
            /// <summary>
            /// Default state. Means that it is an invalid entity.
            /// </summary>
            None = 0, 

            /// <summary>
            /// Means that the client is trying to rebind a new port to one of the communication channels.
            /// </summary>
            Rebind,

            /// <summary>
            /// A handshake has been sent to the server.
            /// </summary>
            Handshaking,

            /// <summary>
            /// GameClient is sending the required information to be authenticated on the server.
            /// </summary>
            Authenticating,

            /// <summary>
            /// Server has sent the UDP information so the GameClient is trying to stablish communication using the UDP channel.
            /// </summary>
            UDPSettingUp,

            /// <summary>
            /// Idle state.
            /// </summary>
            Awaiting,

            /// <summary>
            /// The client has been succesfuly connected to the server.
            /// </summary>
            Connected,

            /// <summary>
            /// Is set when the client breaks connections.
            /// </summary>
            Disconnecting,
        };

        /// <summary>
        /// Client settings to be used to work.
        /// </summary>
        public ClientSettings workingSettings;

        /// <summary>
        /// Tells the current status of the client.<b>This is a volatile property.</b>
        /// </summary>
        volatile protected ClientStatus currentStatus;

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

        #region Events

        /// <summary>
        /// Event raised when a TCP message arrives to the system and it is marked as User command.
        /// </summary>
        public event TCPMessageArrived TCPMessageArrived;

        /// <summary>
        /// Event raised when an UDP message arrives to the system and it is marked as User or Chat command.
        /// </summary>
        public event UDPMessageArrived UDPMessageArrived;

        #endregion

        #region UserManagement

        /// <summary>
        /// Event definition to the event raised when an user is disconnected.
        /// </summary>
        public event UserDisconnectedEventHandler UserDisconnected;

        #endregion

        #region TCP_Buffering

        /// <summary>
        /// Buffer used to store all the incoming messages.
        /// </summary>
        protected KSPM.IO.Memory.CyclicalMemoryBuffer tcpBuffer;

        /// <summary>
        /// Converts all incoming bytes into proper information packets.
        /// </summary>
        protected PacketHandler packetizer;

        /// <summary>
        /// Pool of SocketAsyncEventArgs used to send TCP streams.
        /// </summary>
        SocketAsyncEventArgsPool tcpOutEventsPool;

        /// <summary>
        /// Pool of SocketAsyncEventArgs used to receive TCP streams.
        /// </summary>
        SocketAsyncEventArgsPool tcpInEventsPool;

        #endregion

        #region TCPProperties

        /// <summary>
        /// Thread safe Queue to hold all the commands.
        /// </summary>
        protected CommandQueue commandsQueue;

        /// <summary>
        /// Thread safe Queue to hold all the outgoing messages.
        /// </summary>
        protected CommandQueue outgoingTCPMessages;

        /// <summary>
        /// Timer to schedule the sending keep alive streams through the TCP connection.
        /// </summary>
        protected System.Threading.Timer tcpKeepAliveTimer;

        /// <summary>
        /// Callback to be called each time the timer rises its event.
        /// </summary>
        protected System.Threading.TimerCallback tcpKeepAliveCallback;

        /// <summary>
        /// Amount of time to sends and keep alive stream.
        /// </summary>
        protected long tcpKeepAliveInterval;

        /// <summary>
        /// Timer to handle when the incoming udp queue is full, giving some time to the system to process the current messages until their number decreases and make the system be able to operate at 100%.
        /// </summary>
        protected System.Threading.Timer tcpPurgeTimer;

        /// <summary>
        /// Amount of time to set when the timer should check the capacity of the referred queue.
        /// </summary>
        protected int tcpPurgeTimeInterval;

        /// <summary>
        /// Tells when the system is purging an UDP queue.
        /// </summary>
        protected int tcpPurgeFlag;

        /// <summary>
        /// Tells the amount of messages are allowe to receive messages again.
        /// </summary>
        protected int tcpMinimumMessagesAllowedAfterPurge;


        #endregion

        #region UDPProperties

        /// <summary>
        /// Server information to connect through UDP
        /// </summary>
        protected ServerInformation udpServerInformation;

        /// <summary>
        /// Holds the requiered properties to stablish a UDP connection with the Server.
        /// </summary>
        public NetworkBaseCollection udpNetworkCollection;

        /// <summary>
        /// Holds the incoming messages through an UDP connection.
        /// </summary>
        protected CommandQueue incomingUDPMessages;

        /// <summary>
        /// Holds the outgoing messages through an UDP connection.
        /// </summary>
        protected CommandQueue outgoingUDPMessages;

        /// <summary>
        /// Tells if the UDP connection is already stablished.
        /// </summary>
        protected bool usingUDP;

        /// <summary>
        /// Tells if the UDP hole has been punched.
        /// </summary>
        protected bool udpHolePunched;

        /// <summary>
        /// Pairing code.
        /// </summary>
        protected int pairingCode;

        /// <summary>
        /// Timer to handle when the incoming udp queue is full, giving some time to the system to process the current messages until their number decreases and make the system be able to operate at 100%.
        /// </summary>
        protected System.Threading.Timer udpPurgeTimer;

        /// <summary>
        /// Amount of time to set when the timer should check the capacity of the referred queue.
        /// </summary>
        protected int udpPurgeTimeInterval;

        /// <summary>
        /// Tells when the system is purging an UDP queue.
        /// </summary>
        protected int udpPurgeFlag;

        /// <summary>
        /// Tells the amount of messages are allowe to receive messages again.
        /// </summary>
        protected int udpMinimumMessagesAllowedAfterPurge;

        #endregion

        #region UDP_Buffering

        /// <summary>
        /// Buffer used to store all the incoming messages.
        /// </summary>
        protected KSPM.IO.Memory.CyclicalMemoryBuffer udpBuffer;

        /// <summary>
        /// Converts all incoming bytes into proper information packets.
        /// </summary>
        protected PacketHandler udpPacketizer;

        /// <summary>
        /// Pool of SocketAsyncEventArgs used to receive udp datagrams.
        /// </summary>
        protected SharedBufferSAEAPool udpInputSAEAPool;

        /// <summary>
        /// Pool of SocketAsyncEventArgs used to send udp datagrams.
        /// </summary>
        protected SocketAsyncEventArgsPool udpOutSAEAPool;

        /// <summary>
        /// Pool of raw messages to use them to receive/send datagrams.
        /// </summary>
        protected MessagesPool udpIOMessagesPool;

        #endregion

        #region ThreadPooling

        /// <summary>
        /// This thread will handle both UDP and TCP messages.
        /// Avoiding many changing contexts among the other threads.
        /// </summary>
        Thread handleIncomingMessagesThread;

        /// <summary>
        /// This thread will handle both UDP and TCP messages.
        /// Avoiding as many as possible context changes among other threads.
        /// </summary>
        Thread handleOutgoingMessagesThread;

        #endregion

        #region Chat

        /// <summary>
        /// Handles the KSPM Chat system.
        /// </summary>
        protected ChatManager chatSystem;

        #endregion

        #region ErrorHandling

        /// <summary>
        /// Will hold ocurred errors during the runtime.
        /// </summary>
        System.Collections.Generic.Queue<System.Exception> runtimeErrors;

        /// <summary>
        /// Thread to hangle errors and take the proper operations.
        /// </summary>
        Thread errorHandlingThread;

        #endregion

        #region CreationAndInitializationCode

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
            this.udpHolePunched = false;

            this.workingSettings = null;

            this.chatSystem = null;

            this.currentStatus = ClientStatus.None;

            ///Pooling ThreadingCode
            this.handleIncomingMessagesThread = new Thread(new ThreadStart(this.HandleIncomingMessagesThreadMethod));
            this.handleOutgoingMessagesThread = new Thread(new ThreadStart(this.HandleOutgoingMessagesThreadMethod));

            ///TCP queues.
            this.commandsQueue = new CommandQueue();
            this.outgoingTCPMessages = new CommandQueue();

            ///UDP queues.
            this.incomingUDPMessages = new CommandQueue();
            this.outgoingUDPMessages = new CommandQueue();

            ///Initializing the TCP Network
            this.ownerNetworkCollection = new NetworkBaseCollection(ClientSettings.ClientBufferSize);
            ///Initializing the UDP Network
            this.udpNetworkCollection = new NetworkBaseCollection(ClientSettings.ClientBufferSize);

            ///Network init
            this.udpServerInformation = new ServerInformation();
            this.usingUDP = false;

            ///Error handling
            this.runtimeErrors = new System.Collections.Generic.Queue<System.Exception>();
            this.errorHandlingThread = new Thread(new ThreadStart(this.HandleErrorsThreadMethod));

            ///TCP Buffering
            this.tcpBuffer = new IO.Memory.CyclicalMemoryBuffer(KSPM.Network.Server.ServerSettings.PoolingCacheSize, 1024);
            this.packetizer = new PacketHandler(this.tcpBuffer);
            this.tcpOutEventsPool = new SocketAsyncEventArgsPool(KSPM.Network.Server.ServerSettings.PoolingCacheSize / 2, this.OnSendingOutgoingDataComplete);
            this.tcpInEventsPool = new SocketAsyncEventArgsPool(KSPM.Network.Server.ServerSettings.PoolingCacheSize / 2, this.OnTCPIncomingDataComplete);

            ///TCP timers
            ///3600000 ms = 1 hour
            this.tcpKeepAliveInterval = ClientSettings.TCPKeepAliveInterval;
            //this.tcpKeepAliveInterval = 5000;
            this.tcpKeepAliveCallback = new TimerCallback( this.SendTCPKeepAliveCommand );
            this.tcpKeepAliveTimer = new System.Threading.Timer(this.tcpKeepAliveCallback, null, System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);

            ///UDP Buffers
            this.udpBuffer = new IO.Memory.CyclicalMemoryBuffer(KSPM.Network.Server.ServerSettings.PoolingCacheSize, (uint)KSPM.Network.Server.ServerSettings.ServerBufferSize);
            this.udpPacketizer = new PacketHandler(this.udpBuffer);
            this.udpInputSAEAPool = new SharedBufferSAEAPool(KSPM.Network.Server.ServerSettings.PoolingCacheSize, this.udpNetworkCollection.secondaryRawBuffer, this.OnUDPIncomingDataComplete);
            this.udpOutSAEAPool = new SocketAsyncEventArgsPool(KSPM.Network.Server.ServerSettings.PoolingCacheSize, this.OnUDPSendingDataComplete);
            this.udpIOMessagesPool = new MessagesPool(KSPM.Network.Server.ServerSettings.PoolingCacheSize * 1000, new RawMessage(Message.CommandType.Null, null, 0));

            ///UDP Purge Timer
            this.udpPurgeTimer = new Timer(this.HandleUDPPurgeTimerCallback);
            this.udpPurgeTimeInterval = (int)ClientSettings.PurgeTimeIterval;
            this.udpMinimumMessagesAllowedAfterPurge = (int)(this.incomingUDPMessages.MaxCommandAllowed * (1.0f - ClientSettings.AvailablePercentAfterPurge));
            this.udpPurgeFlag = 0;

            ///TCP Purge Timer
            this.tcpPurgeTimer = new Timer(this.HandleTCPPurgeTimerCallback);
            this.tcpPurgeTimeInterval = (int)ClientSettings.PurgeTimeIterval;
            this.tcpMinimumMessagesAllowedAfterPurge = (int)(this.commandsQueue.MaxCommandAllowed * (1.0f - ClientSettings.AvailablePercentAfterPurge));
            this.tcpPurgeFlag = 0;

            ///Setting the events to null.
            this.UDPMessageArrived = null;
            this.TCPMessageArrived = null;
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
                this.udpHolePunched = false;
                this.usingUDP = false;

                this.currentStatus = ClientStatus.None;

                ///If Settings could not be loaded properly, a default one is created and tried to write it.
                ///If the writer could not be able to write down the settings, a message is showed into the log, but this not break the game because the settings references
                ///is created.
                result = ClientSettings.ReadSettings(out this.workingSettings);

                this.errorHandlingThread.Start();

                ///Starting the pooling threads.
                this.handleIncomingMessagesThread.Start();
                this.handleOutgoingMessagesThread.Start();
            }
            catch (System.Exception ex)
            {
                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Something went wrong, shutting down.", this.id));
                KSPMGlobals.Globals.Log.WriteTo(ex.Message);
                this.ShutdownClient();
                this.aliveFlag = false;
                result = Error.ErrorType.ClientUnableToRun;
            }
            return result;
        }

        #endregion

        #region Connection

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
                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Client already connected.", this.id));
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

            ///Creating sockets, connecting and setting up everything
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
                this.outgoingTCPMessages.Purge(true);///Checking about
                this.commandsQueue.Purge(true);
                connectThread.Start();
                if (!connectThread.Join((int)this.workingSettings.connectionTimeout))
                {
                    ///Killing the thread because it has taken too much.
                    connectThread.Abort();
                    KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Connection timeout.", this.id));

                    ///Disposing used resources inside the connection process.
                    this.BreakConnections(this, new KSPMEventArgs(KSPMEventArgs.EventType.Connect, KSPMEventArgs.EventCause.ConnectionTimeOut));
                }
                if (!this.holePunched || !this.udpHolePunched)
                {
                    return Error.ErrorType.ClientUnableToConnect;
                }
            }
            catch (System.Exception ex)
            {
                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Something went wrong, shutting down.", this.id));
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
            this.holePunched = false;
            this.udpHolePunched = false;
            try
            {
                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Starting to connect.", this.id));
                KSPMGlobals.Globals.NAT.Punch(ref this.ownerNetworkCollection.socketReference, this.gameServerInformation.ip, this.gameServerInformation.port);
                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] TCP hole status: {1}.", this.id, KSPMGlobals.Globals.NAT.Status.ToString()));
                this.holePunched = KSPMGlobals.Globals.NAT.Status == NATTraversal.NATStatus.Connected;
                if (!this.holePunched)
                {
                    KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Hole punching can not be done, reassigning connection. Try again", this.id));
                    this.reassignAddress = true;
                    this.BreakConnections(this, new KSPMEventArgs(KSPMEventArgs.EventType.Connect, KSPMEventArgs.EventCause.TCPHolePunchingCannotBeDone) );
                    return;
                }
                this.ReceiveTCPStream();
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
                            try
                            {
                                this.udpNetworkCollection.socketReference = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                                this.udpNetworkCollection.socketReference.Bind(new IPEndPoint( ((IPEndPoint)this.ownerNetworkCollection.socketReference.LocalEndPoint).Address, this.workingSettings.udpPort));
                                //this.udpNetworkCollection.socketReference.Bind(new IPEndPoint(IPAddress.Any, this.workingSettings.udpPort));
                                KSPMGlobals.Globals.NAT.Punch(ref this.udpNetworkCollection.socketReference, this.udpServerInformation.ip, this.udpServerInformation.port);
                                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] UDP hole status: {1}.", this.id, KSPMGlobals.Globals.NAT.Status.ToString()));
                                this.udpHolePunched = KSPMGlobals.Globals.NAT.Status == NATTraversal.NATStatus.Connected;
                                if (this.udpHolePunched)
                                {
                                    outgoingMessage = this.udpIOMessagesPool.BorrowMessage;
                                    //Message.LoadUDPPairingMessage(this, ref outgoingMessage);
                                    Message.LoadUDPInfoAndPairingMessage(this, ref outgoingMessage);
                                    //rawMessageReference = (RawMessage)outgoingMessage;
                                    //PacketHandler.EncodeRawPacket(ref rawMessageReference.bodyMessage);
                                    this.outgoingUDPMessages.EnqueueCommandMessage(ref outgoingMessage);
                                    this.ReceiveUDPDatagram();
                                    this.currentStatus = ClientStatus.Awaiting;
                                }
                                else
                                {
                                    this.udpNetworkCollection.socketReference.Close();
                                    this.udpNetworkCollection.socketReference = null;
                                }
                            }
                            catch (SocketException)
                            {
                            }
                            break;
                        ///Already received the UDPPairingOK message.
                        case ClientStatus.Connected:
                            KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Id[{1}] Connected to [{2}].", this.id, this.clientOwner.Id, this.udpServerInformation.NetworkEndPoint.ToString()));
                            connected = true;
                            ///Activating the keepalive timer.
                            this.tcpKeepAliveTimer.Change(this.tcpKeepAliveInterval, this.tcpKeepAliveInterval);
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

        #endregion

        #region PoolingCode

        /// <summary>
        /// Handles both UDP and TCP incoming Queues, one after the other.
        /// </summary>
        protected void HandleIncomingMessagesThreadMethod()
        {
            Message command = null;
            ManagedMessage managedMessageReference = null;
            ChatMessage incomingChatMessage = null;
            ServerInformation udpServerInformationFromNetwork = null;
            int receivedPairingCode = -1;
            if (!this.ableToRun)
            {
                KSPMGlobals.Globals.Log.WriteTo(Error.ErrorType.ClientUnableToRun.ToString());
                return;
            }
            try
            {
                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Starting to handle incoming messages body.", this.id));
                while (this.aliveFlag)
                {
                    ///TCP processing.
                    this.commandsQueue.DequeueCommandMessage(out command);
                    if (command != null)
                    {
#if DEBUGTRACER_L2
                        KSPMGlobals.Globals.Log.WriteTo(string.Format("GameClient.HandleIncomingMessagesThreadMethod.TCP -> {0}", command.ToString()));
#endif
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
                            case Message.CommandType.ServerFull:
                                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Serverfull.", this.id));
                                this.BreakConnections(this, new KSPMEventArgs(KSPMEventArgs.EventType.Connect, KSPMEventArgs.EventCause.ServerFull) );
                                break;
                            case Message.CommandType.UDPSettingUp:///Create the UDP conn.
                                ///Reads the information sent by the server and starts the UDP setting up process.
                                managedMessageReference = (ManagedMessage)command;
                                udpServerInformationFromNetwork = new ServerInformation();
                                udpServerInformationFromNetwork.port = System.BitConverter.ToInt32(command.bodyMessage, 13);
                                udpServerInformationFromNetwork.ip = ((IPEndPoint)managedMessageReference.OwnerNetworkEntity.ownerNetworkCollection.socketReference.RemoteEndPoint).Address.ToString();
                                receivedPairingCode = System.BitConverter.ToInt32(command.bodyMessage, 17);
                                this.clientOwner.SetCustomId(System.BitConverter.ToInt32(command.bodyMessage, 21));

                                if (!this.udpServerInformation.Equals(udpServerInformationFromNetwork) && this.pairingCode != receivedPairingCode)
                                {
                                    udpServerInformationFromNetwork.Clone(ref this.udpServerInformation);
                                    this.pairingCode = ~receivedPairingCode;
                                    this.currentStatus = ClientStatus.UDPSettingUp;
                                    KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] UDP pairing code. {1}", this.id, System.Convert.ToString(this.pairingCode, 2)));
                                }
                                udpServerInformationFromNetwork.Dispose();
                                udpServerInformationFromNetwork = null;
                                break;
                            case Message.CommandType.ChatSettingUp:
                                if (ChatManager.CreateChatManagerFromMessage(command.bodyMessage, ChatManager.DefaultStorageMode.Persistent, out this.chatSystem) == Error.ErrorType.Ok)
                                {
                                    this.chatSystem.Owner = this;
                                    KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Chat system is online, {1} groups registered.", this.id, this.chatSystem.RegisteredGroups));
                                }
                                break;
                            case Message.CommandType.Chat:
                                if (this.chatSystem != null)///Checking if the chat system is already set up.
                                {
                                    if (ChatMessage.InflateChatMessage(command.bodyMessage, out incomingChatMessage) == Error.ErrorType.Ok)
                                    {
                                        ///Checking if the message should be filtered or not.
                                        if (!this.chatSystem.ApplyFilters(incomingChatMessage, ChatManager.FilteringMode.And))
                                        {
                                            this.chatSystem.AttachMessage(incomingChatMessage);
                                            KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}][{1}_{2}]-Says:{3}", this.id, incomingChatMessage.Time.ToShortTimeString(), incomingChatMessage.sendersUsername, incomingChatMessage.Body));
                                        }
                                    }
                                }
                                break;
                            case Message.CommandType.User:
                                this.OnTCPMessageArrived(this, (ManagedMessage)command);
                                break;
                            case Message.CommandType.Unknown:
                            default:
                                KSPMGlobals.Globals.Log.WriteTo("Non-Critical Unknown command: " + command.Command.ToString());
                                break;
                        }
                        ///Cleaning up.
                        command.Release();
                        command = null;
                    }
                    ///UDP Processing.
                    this.incomingUDPMessages.DequeueCommandMessage(out command);
                    if (command != null)
                    {
#if DEBUGTRACER_L2
                        KSPMGlobals.Globals.Log.WriteTo(string.Format("GameClient.HandleIncomingMessagesThreadMethod.UDP -> {0}", command.ToString()));
#endif
                        switch (command.Command)
                        {
                            ///Means that everything works fine, so you are able to send/receive data through the UDP connection.
                            case Message.CommandType.UDPPairingOk:
                                this.currentStatus = ClientStatus.Connected;
                                break;
                            ///Means that the message was received by the remote server, but something were wrong, anyway the communication is stablished.
                            ///At this the Connected status is going to be set, but a Warning must be raised.
                            case Message.CommandType.UDPPairingFail:
                                this.currentStatus = ClientStatus.Connected;
                                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] '{1}' Connection stablished, but something were wrong.", this.id, command.Command.ToString()));
                                break;
                            case Message.CommandType.UDPChat:
                                if (this.chatSystem != null)///Checking if the chat system is already set up.
                                {
                                    if (ChatMessage.InflateChatMessage(command.bodyMessage, out incomingChatMessage) == Error.ErrorType.Ok)
                                    {
                                        ///Checking if the message should be filtered or not.
                                        if (!this.chatSystem.ApplyFilters(incomingChatMessage, ChatManager.FilteringMode.And))
                                        {
                                            this.chatSystem.AttachMessage(incomingChatMessage);
                                            KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}]-UDP-[{1}_{2}]-Says:{3}", this.id, incomingChatMessage.Time.ToShortTimeString(), incomingChatMessage.sendersUsername, incomingChatMessage.Body));
                                        }
                                    }
                                }
                                break;
                            case Message.CommandType.User:
                                this.OnUDPMessageArrived(this, (RawMessage)command);
                                break;
                            case Message.CommandType.Unknown:
                            default:
                                KSPMGlobals.Globals.Log.WriteTo("Non-Critical Unknown command: " + command.Command.ToString());
                                break;
                        }

                        ///Cleaning up.
                        this.udpIOMessagesPool.Recycle(command);
                    }
                    //Thread.Sleep(3);
                }
            }
            catch (ThreadAbortException)
            {
                this.aliveFlag = false;
            }
        }

        /// <summary>
        /// Threaded method to send commands.
        /// </summary>
        protected void HandleOutgoingMessagesThreadMethod()
        {
            Message outgoingMessage = null;
            SocketAsyncEventArgs sendingData = null;
            SocketAsyncEventArgs outgoingData = null;
            if (!this.ableToRun)
            {
                KSPMGlobals.Globals.Log.WriteTo(Error.ErrorType.ClientUnableToRun.ToString());
                return;
            }
            try
            {
                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Starting to handle outgoing messages.", this.id));
                while (this.aliveFlag)
                {
                    ///TCP outgoing
                    if (this.holePunched)
                    {
                        this.outgoingTCPMessages.DequeueCommandMessage(out outgoingMessage);
                        if (outgoingMessage != null)
                        {
                            outgoingMessage.MessageId = (uint)System.Threading.Interlocked.Increment(ref Message.MessageCounter);
                            System.Buffer.BlockCopy(System.BitConverter.GetBytes(outgoingMessage.MessageId), 0, outgoingMessage.bodyMessage, (int)PacketHandler.PrefixSize, 4);

#if DEBUGTRACER_L2
                            KSPMGlobals.Globals.Log.WriteTo(string.Format("GameClient.HandleOutgoingMessagesThreadMethod.TCP -> {0}", outgoingMessage.ToString()));
#endif
                            try
                            {
                                if (this.ownerNetworkCollection != null && this.ownerNetworkCollection.socketReference != null)
                                {
                                    sendingData = this.tcpOutEventsPool.NextSlot;
                                    sendingData.AcceptSocket = this.ownerNetworkCollection.socketReference;
                                    sendingData.SetBuffer(outgoingMessage.bodyMessage, 0, (int)outgoingMessage.MessageBytesSize);
                                    this.ownerNetworkCollection.socketReference.SendAsync(sendingData);
                                }
                            }
                            catch (System.Exception ex)///Catching exceptions and adding them to the queue to their proper handling.
                            {
                                ///If something happens we ensure that the SockeAsyncEventArg is recycled.
                                if (this.tcpOutEventsPool != null)
                                {
                                    this.tcpOutEventsPool.Recycle(sendingData);
                                }
                                else
                                {
                                    sendingData.Dispose();
                                    sendingData = null;
                                }
                                this.runtimeErrors.Enqueue(ex);
                            }

                            ///Cleaning up
                            outgoingMessage.Release();
                            outgoingMessage = null;
                        }
                    }
                    ///UDP outgoing
                    ///Means that it is already connected.
                    if (this.udpHolePunched)
                    {
                        this.outgoingUDPMessages.DequeueCommandMessage(out outgoingMessage);
                        if (outgoingMessage != null)
                        {
                            //KSPMGlobals.Globals.Log.WriteTo(outgoingMessage.ToString());
                            ///Setting up the MessageId
                            outgoingMessage.MessageId = (uint)System.Threading.Interlocked.Increment(ref Message.MessageCounter);
                            System.Buffer.BlockCopy(System.BitConverter.GetBytes(outgoingMessage.MessageId), 0, outgoingMessage.bodyMessage, (int)PacketHandler.PrefixSize, 4);

#if DEBUGTRACER_L2
                            KSPMGlobals.Globals.Log.WriteTo(string.Format("GameClient.HandleOutgoingMessagesThreadMethod.UDP -> {0}", outgoingMessage.ToString()));
#endif

                            outgoingData = this.udpOutSAEAPool.NextSlot;
                            outgoingData.AcceptSocket = this.udpNetworkCollection.socketReference;
                            outgoingData.RemoteEndPoint = this.udpServerInformation.NetworkEndPoint;
                            outgoingData.SetBuffer(outgoingMessage.bodyMessage, 0, (int)outgoingMessage.MessageBytesSize);
                            ///Setting the message to the usertoken allowing to recycle it after.
                            outgoingData.UserToken = outgoingMessage;
                            this.udpNetworkCollection.socketReference.SendToAsync(outgoingData);
                        }
                    }
                }
            }
            catch (ThreadAbortException)
            {
                this.aliveFlag = false;
                this.udpHolePunched = false;
            }
        }

        #endregion

        #region TCPCode

        /// <summary>
        /// Method called when a TCP asynchronous sending  is complete.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">SocketAsyncEventArgs used to perform the sending stuff.</param>
        protected void OnSendingOutgoingDataComplete(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                if (e.BytesTransferred > 0)
                {
                    //KSPMGlobals.Globals.Log.WriteTo(e.BytesTransferred.ToString());
                    this.MessageSent(this, null);
                }
            }
            //e.Completed -= this.OnSendingOutgoingDataComplete;

            ///Checking if the SocketAsyncEventArgs pool has not been released and set to null.
            ///If the situtation mentioned above we have to dispose the SocketAsyncEventArgs by hand.
            if (this.tcpOutEventsPool == null)
            {
                e.Dispose();
                e = null;
            }
            else
            {
                ///Recycling the SocketAsyncEventArgs used by this process.
                this.tcpOutEventsPool.Recycle(e);
            }
        }

        /// <summary>
        /// Sends a TCP KeepAlive command, because after 8 hours of inactivity the TCP socket is closed by the system.
        /// </summary>
        /// <param name="stateInfo"></param>
        protected void SendTCPKeepAliveCommand(object stateInfo)
        {
            Message keepAliveCommand = null;
            if (Message.KeepAlive(this, out keepAliveCommand) == Error.ErrorType.Ok)
            {
                if (!this.outgoingTCPMessages.EnqueueCommandMessage(ref keepAliveCommand))
                {
                    keepAliveCommand.Release();
                    keepAliveCommand = null;
                }
            }
        }

        /// <summary>
        /// Method called each amount of time specified by tcpPurgeTimeInterval property.
        /// Checks if the queue is able to receive new messages.
        /// </summary>
        /// <param name="state"></param>
        protected void HandleTCPPurgeTimerCallback(object state)
        {
            if (this.commandsQueue.DirtyCount <= this.tcpMinimumMessagesAllowedAfterPurge)///The system has consumd all the messages.
            {
                ///Disabling the timer.
                this.tcpPurgeTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);

                ///Disabling the purge flag.
                Interlocked.Exchange(ref this.tcpPurgeFlag, 0);
                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] TCPPurge finished.", this.id));
            }
        }

        #region TCP_Processing

        /// <summary>
        /// Asynchronous method used to receive TCP streams.
        /// </summary>
        public void ReceiveTCPStream()
        {
            SocketAsyncEventArgs incomingData = this.tcpInEventsPool.NextSlot;
            incomingData.AcceptSocket = this.ownerNetworkCollection.socketReference;
            incomingData.SetBuffer(this.ownerNetworkCollection.secondaryRawBuffer, 0, this.ownerNetworkCollection.secondaryRawBuffer.Length);
            //incomingData.Completed += new System.EventHandler<SocketAsyncEventArgs>(this.OnTCPIncomingDataComplete);
            try
            {
                if (!this.ownerNetworkCollection.socketReference.ReceiveAsync(incomingData))
                {
                    this.OnTCPIncomingDataComplete(this, incomingData);
                }
            }
            catch (System.Exception ex)///Something happened to the remote client, so it is required to this ServerSideClient to kill itself.
            {
                this.runtimeErrors.Enqueue(ex);
            }
        }

        /// <summary>
        /// Method called each time an stream was received through the network.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void OnTCPIncomingDataComplete(object sender, SocketAsyncEventArgs e)
        {
            int readBytes = 0;
            if (e.SocketError == SocketError.Success)
            {
                readBytes = e.BytesTransferred;
                //KSPMGlobals.Globals.Log.WriteTo(readBytes.ToString());
                if (readBytes > 0)
                {
                    this.tcpBuffer.Write(e.Buffer, (uint)readBytes);
                    ///We can change it to another method that no allocates memory.
                    this.packetizer.PacketizeCRCCreateMemory(this);
                    this.ReceiveTCPStream();

                    ///Either we have success reading the incoming data or not we need to recycle the SocketAsyncEventArgs used to perform this reading process.
                    //e.Completed -= this.OnTCPIncomingDataComplete;
                    if (this.tcpInEventsPool == null)///Means that the reference has been killed. So we have to release this SocketAsyncEventArgs by hand.
                    {
                        e.Dispose();
                        e = null;
                    }
                    else
                    {
                        this.tcpInEventsPool.Recycle(e);
                    }
                }
                else
                {
                    if (this.currentStatus == ClientStatus.Disconnecting || this.currentStatus == ClientStatus.None)
                    {
                        KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}][\"{1}\"] Nicely disconnection TCP.", this.id, "OnTCPIncomingDataComplete"));
                    }
                    else
                    {
                        ///If BytesTransferred is 0, it means that there is no more bytes to be read, so the remote socket was
                        ///disconnected.
                        KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}][\"{1}\"] Remote client disconnected, performing a removing process on it.", this.id, "OnTCPIncomingDataComplete"));
                        this.BreakConnections(this, new KSPMEventArgs(KSPMEventArgs.EventType.RuntimeError, KSPMEventArgs.EventCause.ServerDisconnected));
                    }
                }
            }
            else
            {
                if (this.currentStatus == ClientStatus.Disconnecting || this.currentStatus == ClientStatus.None)
                {
                    KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}][\"{1}\"] Nicely disconnection TCP.", this.id, "OnTCPIncomingDataComplete"));
                }
                else
                {
                    KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}][\"{1}:{2}\"] Something went wrong with the remote client, performing a removing process on it.", this.id, "OnTCPIncomingDataComplete", e.SocketError));
                    this.BreakConnections(this, new KSPMEventArgs(KSPMEventArgs.EventType.RuntimeError, KSPMEventArgs.EventCause.ServerDisconnected));
                }
            }
        }

        /// <summary>
        /// Called each time an stream is converted to bytes and is sent to be processed.
        /// </summary>
        /// <param name="rawData">Byte array with the information.</param>
        /// <param name="fixedLegth">Number of usable bytes inside the byte array.</param>
        public void ProcessPacket(byte[] rawData, uint fixedLegth)
        {
            Message incomingMessage = null;
            if (Interlocked.CompareExchange(ref this.tcpPurgeFlag, 0, 0) == 0)
            {
                //KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo(fixedLegth.ToString());
                if (PacketHandler.InflateManagedMessageAlt(rawData, this, out incomingMessage) == Error.ErrorType.Ok)
                {
#if DEBUG_PRINT
                    KSPMGlobals.Globals.Log.WriteTo("Received: " + fixedLegth.ToString());
#endif
                    if (!this.commandsQueue.EnqueueCommandMessage(ref incomingMessage))
                    {
                        incomingMessage.Release();
                        incomingMessage = null;

                        ///Must be invoked only one time.
                        Interlocked.Exchange(ref this.tcpPurgeFlag, 1);
                        this.tcpPurgeTimer.Change(this.tcpPurgeTimeInterval, this.tcpPurgeTimeInterval);
                    }
                }
            }
        }

        /// <summary>
        /// Does nothing at all.
        /// </summary>
        /// <param name="rawData"></param>
        /// <param name="rawDataOffset"></param>
        /// <param name="fixedLength"></param>
        public void ProcessPacket(byte[] rawData, uint rawDataOffset, uint fixedLength)
        {
            /*
            Message incomingMessage = null;
            //KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo(fixedLength.ToString());
            if (PacketHandler.InflateManagedMessageAlt(rawData, this, out incomingMessage) == Error.ErrorType.Ok)
            {
                this.commandsQueue.EnqueueCommandMessage(ref incomingMessage);
            }
            */
        }

        /// <summary>
        /// Raises the TCPMessageArrived event.
        /// </summary>
        /// <param name="sender">Underlaying NetworkEntity who raises the event.</param>
        /// <param name="message">Arrived message.</param>
        protected void OnTCPMessageArrived(NetworkEntity sender, ManagedMessage message)
        {
#if DEBUG_PRINT
            KSPMGlobals.Globals.Log.WriteTo(message.ToString());
#endif
            if (this.TCPMessageArrived != null)
            {
                this.TCPMessageArrived(sender, message);
            }
        }

        #endregion

        #endregion

        #region UDPCode

        /// <summary>
        /// Asynchrounouse method used to receive datagrams.
        /// </summary>
        protected void ReceiveUDPDatagram()
        {
#if PROFILING

            this.profilerOutgoingMessages.Set();
#endif
            ///Checking if the reference is still running and the sockets are working.
            if (!this.aliveFlag)
                return;
            SocketAsyncEventArgs incomingData = this.udpInputSAEAPool.NextSlot;
            incomingData.AcceptSocket = this.udpNetworkCollection.socketReference;
            incomingData.RemoteEndPoint = this.udpServerInformation.NetworkEndPoint;

            ///Setting the buffer offset and count, keep in mind that we are no assigning a new buffer, we are only setting working paremeters.
            ///Because at high speeds if you set the buffear inside each call, it is thrown a SocketError.Fault error.
            incomingData.SetBuffer(0, (int)this.udpInputSAEAPool.BufferSize);
            try
            {
                if (!this.udpNetworkCollection.socketReference.ReceiveFromAsync(incomingData))
                {
                    this.OnUDPIncomingDataComplete(this, incomingData);
                }
            }
            catch (System.ObjectDisposedException ex)
            {
                ///REcycling the SAEA object.
                this.udpInputSAEAPool.Recycle(incomingData);
                this.runtimeErrors.Enqueue(ex);
            }
            catch (SocketException ex)
            {
                ///REcycling the SAEA object.
                this.udpInputSAEAPool.Recycle(incomingData);
                this.runtimeErrors.Enqueue(ex);
            }
            catch (System.NullReferenceException ex)
            {
                ///REcycling the SAEA object.
                this.udpInputSAEAPool.Recycle(incomingData);
                this.runtimeErrors.Enqueue(ex);
            }
        }

        /// <summary>
        /// Method called each time an asynchrounous datagran reception is completed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void OnUDPIncomingDataComplete(object sender, SocketAsyncEventArgs e)
        {
#if PROFILING
            if (this.profilerOutgoingMessages != null)
            {
                this.profilerOutgoingMessages.Mark();
            }
#endif
            int readBytes = 0;
            if (e.SocketError == SocketError.Success)
            {
                readBytes = e.BytesTransferred;
                if (readBytes > 0 && this.aliveFlag)
                {
                    this.udpBuffer.Write(e.Buffer, (uint)readBytes);
#if PROFILING
                    this.profilerPacketizer.Set();
#endif
                    this.udpPacketizer.UDPPacketizeCRCLoadIntoMessage(this, this.udpIOMessagesPool);
#if PROFILING
                    if (this.profilerPacketizer != null)
                    {
                        this.profilerPacketizer.Mark();
                    }
#endif
                    ///We need to recycle the SocketAsyncEventArgs used to perform this reading process.
                    if (this.udpInputSAEAPool == null)///Means that the reference has been killed. So we have to release this SocketAsyncEventArgs by hand.
                    {
                        e.Dispose();
                        e = null;
                    }
                    else
                    {
                        this.udpInputSAEAPool.Recycle(e);
                    }

                    this.ReceiveUDPDatagram();
                }
                else
                {
                    ///If BytesTransferred is 0, it means that there is no more bytes to be read, so the remote socket was
                    ///disconnected.
                    this.udpInputSAEAPool.Recycle(e);
                    KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}][\"{1}\"] Remote client disconnected, proceed to disconnect.", this.id, "OnUDPIncomingDataComplete"));
                    this.BreakConnections(this, new KSPMEventArgs(KSPMEventArgs.EventType.RuntimeError, KSPMEventArgs.EventCause.ServerDisconnected));
                }
            }
            else
            {
                this.udpInputSAEAPool.Recycle(e);
                if (this.currentStatus == ClientStatus.Disconnecting || this.currentStatus == ClientStatus.None)
                {
                    ///The socket has been closed so there is no need to break connections.
                    KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}][\"{1}\"] Nicely disconnection UDP.", this.id, "OnUDPIncomingDataComplete"));
                }
                else
                {
                    KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}][\"{1}:{2}\"] Something went wrong with the UDP remote client, proceed to disconnect.", this.id, "OnUDPIncomingDataComplete", e.SocketError));
                    this.BreakConnections(this, new KSPMEventArgs(KSPMEventArgs.EventType.RuntimeError, KSPMEventArgs.EventCause.ServerDisconnected));
                }
            }
        }

        /// <summary>
        /// Not used at this moment.
        /// </summary>
        /// <param name="rawData"></param>
        /// <param name="fixedLegth"></param>
        public void ProcessUDPPacket(byte[] rawData, uint fixedLegth)
        {
            /*
            Message incomingMessage;
            KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo(fixedLegth.ToString());
            if (PacketHandler.InflateRawMessage(rawData, out incomingMessage) == Error.ErrorType.Ok)
            {
                ///Puting the incoming RawMessage into the queue to be processed.
                //this.incomingPackets.EnqueueCommandMessage(ref incomingMessage);
                //this.ProcessUDPCommand();
            }
            */
        }

        /// <summary>
        /// Process the incoming Message. At this moment only adds it into the Queue.
        /// </summary>
        /// <param name="incomingMessage"></param>
        public void ProcessUDPMessage(Message incomingMessage)
        {
            if (Interlocked.CompareExchange(ref this.udpPurgeFlag, 0, 0) == 0)
            {
                if (!this.incomingUDPMessages.EnqueueCommandMessage(ref incomingMessage))
                {
                    ///If this code is reached means the incoming queue is full, so we need to recycle the incoming message by hand.
                    this.udpIOMessagesPool.Recycle(incomingMessage);

                    ///This must be invoked only one time.
                    Interlocked.Exchange(ref this.udpPurgeFlag, 1);
                    this.udpPurgeTimer.Change(this.udpPurgeTimeInterval, this.udpPurgeTimeInterval);
                }
            }
            else///Means that the system is full.
            {
                ///So we need to recycle the incoming message to avoid memory exhaustion.
                this.udpIOMessagesPool.Recycle(incomingMessage);
            }
        }

        /// <summary>
        /// Raised when a UDP sending process is completed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void OnUDPSendingDataComplete(object sender, SocketAsyncEventArgs e)
        {
            int sentBytes = 0;
            if (e.SocketError == SocketError.Success)
            {
                sentBytes = e.BytesTransferred;
                if (sentBytes > 0)
                {
                    //KSPMGlobals.Globals.Log.WriteTo("UDP_ " + sentBytes.ToString());
                }
                ///Either we have have sucess sending the data, it's required to recycle the outgoing message.
                this.udpIOMessagesPool.Recycle((Message)e.UserToken);
                ///Either we have success sending the incoming data or not we need to recycle the SocketAsyncEventArgs used to perform this reading process.
                if (this.udpOutSAEAPool == null)///Means that the reference has been killed. So we have to release this SocketAsyncEventArgs by hand.
                {
                    e.Dispose();
                    e = null;
                }
                else
                {
                    this.udpOutSAEAPool.Recycle(e);
                }
            }
            else
            {
                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}][\"{1}:{2}\"] Something went wrong sending the the datagram to the remote client, performing a disconnection process.", this.id, "OnUDPSendingDataComplete", e.SocketError));
                ///Either we have have sucess sending the data, it's required to recycle the outgoing message.
                this.udpIOMessagesPool.Recycle((Message)e.UserToken);
                ///Recycling the SAEA object used to perform the send process.
                this.udpOutSAEAPool.Recycle(e);
                this.BreakConnections(this, new KSPMEventArgs(KSPMEventArgs.EventType.RuntimeError, KSPMEventArgs.EventCause.ServerDisconnected));
            }
        }

        /// <summary>
        /// Method called each amount of time specified by udpPurgeTimeInterval property.
        /// Checks if the queue is able to receive new messages.
        /// </summary>
        /// <param name="state"></param>
        protected void HandleUDPPurgeTimerCallback(object state)
        {
            if (this.incomingUDPMessages.DirtyCount <= this.udpMinimumMessagesAllowedAfterPurge)///The system has consumd all the messages.
            {
                ///Disabling the timer.
                this.udpPurgeTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);

                ///Disabling the purge flag.
                Interlocked.Exchange(ref this.udpPurgeFlag, 0);
                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] UDPPurge finished.", this.id));
            }
        }

        /// <summary>
        /// Method called raised each time a datagram is received and it is marked as User command.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        protected void OnUDPMessageArrived(NetworkEntity sender, RawMessage message)
        {
#if DEBUG_PRINT

            KSPMGlobals.Globals.Log.WriteTo(message.ToString());
#endif
            if (this.UDPMessageArrived != null)
            {
                this.UDPMessageArrived(sender, message);
            }
            else
            {
                ///If there is not a event assigned, we must proceed to clean up the given message.
                this.udpIOMessagesPool.Recycle(message);
            }
        }

        #endregion

        #region ClosingConnections

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
            Message disposingMessage = null;
            ///Avoiding to shutdown the client twice or more.
            if (!this.aliveFlag)
                return;
            this.ableToRun = false;
            this.aliveFlag = false;
            this.holePunched = false;
            this.udpHolePunched = false;

            this.currentStatus = ClientStatus.None;

            ///Releasing TCP keep alive timer.
            this.tcpKeepAliveTimer.Dispose();
            this.tcpKeepAliveTimer = null;


            ///*****************Killing threads.

            this.handleIncomingMessagesThread.Abort();
            this.handleIncomingMessagesThread.Join();
            KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Killed handleIncomingMessagesThread.", this.id));

            this.handleOutgoingMessagesThread.Abort();
            this.handleOutgoingMessagesThread.Join();
            KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Killed handledOutgoingMessagesThread.", this.id));


            this.errorHandlingThread.Abort();
            this.errorHandlingThread.Join();
            KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Killed errorHandlingThread.", this.id));
            this.errorHandlingThread = null;


            ///***********************Sockets code
            if (this.ownerNetworkCollection.socketReference != null && this.ownerNetworkCollection.socketReference.Connected)
            {
                this.ownerNetworkCollection.socketReference.Disconnect(true);
                this.ownerNetworkCollection.socketReference.Close();
            }
            this.ownerNetworkCollection.Dispose();
            this.ownerNetworkCollection = null;

            ///UDP Socket code.
            if (this.udpNetworkCollection.socketReference != null)
            {
                this.udpNetworkCollection.socketReference.Close();
            }
            this.udpNetworkCollection.Dispose();
            this.udpNetworkCollection = null;

            ///Setting to null the GameUser reference.
            this.clientOwner = null;

            ///*********************Releasing server information objects.
            this.udpServerInformation.Dispose();
            this.udpServerInformation = null;

            ///**********************Cleaning up the messages's Queues.
            this.commandsQueue.Purge(false);
            this.outgoingTCPMessages.Purge(false);
            this.commandsQueue = null;
            this.outgoingTCPMessages = null;

            ///Cleaning the UDP queues.
            this.incomingUDPMessages.DequeueCommandMessage(out disposingMessage);
            while (disposingMessage != null)
            {
                this.udpIOMessagesPool.Recycle(disposingMessage);
                this.incomingUDPMessages.DequeueCommandMessage(out disposingMessage);
            }
            this.outgoingUDPMessages.DequeueCommandMessage(out disposingMessage);
            while (disposingMessage != null)
            {
                this.udpIOMessagesPool.Recycle(disposingMessage);
                this.outgoingUDPMessages.DequeueCommandMessage(out disposingMessage);
            }
            this.incomingUDPMessages.Purge(false);
            this.outgoingUDPMessages.Purge(false);
            this.incomingUDPMessages = null;
            this.outgoingUDPMessages = null;

            ///Cleaning UDP buffers.
            this.udpBuffer.Release();
            this.udpBuffer = null;
            this.udpPacketizer.Release();
            this.udpPacketizer = null;

            ///Cleaning UDP SAEAs
            this.udpInputSAEAPool.Release(false);
            this.udpOutSAEAPool.Release(false);
            this.udpInputSAEAPool = null;
            this.udpOutSAEAPool = null;

            this.timer.Reset();

            this.tcpBuffer.Release();
            this.tcpBuffer = null;

            ///Cleaning up udpPurgeTimer
            this.udpPurgeTimer.Dispose();
            this.udpPurgeTimer = null;

            ///Cleaning up tcpPurgeTimer
            this.tcpPurgeTimer.Dispose();
            this.tcpPurgeTimer = null;

            KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Client killed!!!", this.id));
        }

        /// <summary>
        /// Closes the connections, cleans references used by the client itself, such as sockets, and buffers.
        /// </summary>
        /// <param name="caller"></param>
        /// <param name="arg">KSPMEventArgs filled with informationa about what happened</param>
        protected void BreakConnections(NetworkEntity caller, object arg)
        {
            Message disposingMessage;

            ///To avoid execute twice or more times this method.
            ///
            /*
            if (!this.holePunched)
                return;
            */

            if (this.currentStatus == ClientStatus.None || this.currentStatus == ClientStatus.Disconnecting)
                return;

            //this.currentStatus = ClientStatus.None;
            this.currentStatus = ClientStatus.Disconnecting;

            ///***********************Sockets code
            this.udpHolePunched = false;
            this.holePunched = false;
            this.usingUDP = false;

            ///Disabling the TCP keep alive timer.
            this.tcpKeepAliveTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);

            if (this.ownerNetworkCollection.socketReference != null )
            {
                ///This means that it was a nicely disconnection.
                if (this.ownerNetworkCollection.socketReference.Connected && arg == null)
                {
                    this.ownerNetworkCollection.socketReference.Shutdown(SocketShutdown.Both);
                    this.ownerNetworkCollection.socketReference.Disconnect(true);
                }
                ///Closing the socket either it is connected or not.
                this.ownerNetworkCollection.socketReference.Close();
                this.ownerNetworkCollection.socketReference = null;
            }

            if (this.udpNetworkCollection.socketReference != null)
            {
                this.udpNetworkCollection.socketReference.Close();
                this.udpNetworkCollection.socketReference = null;
            }

            this.clientOwner = null;

            //this.gameServerInformation = null;

            ///*********************Releasing server information objects.
            this.udpServerInformation.Dispose();

            ///**********************Cleaning up the messages's Queues.
            this.commandsQueue.Purge(true);
            this.outgoingTCPMessages.Purge(true);

            ///Cleaning the UDP queues.
            this.incomingUDPMessages.DequeueCommandMessage(out disposingMessage);
            while (disposingMessage != null)
            {
                this.udpIOMessagesPool.Recycle(disposingMessage);
                this.incomingUDPMessages.DequeueCommandMessage(out disposingMessage);
            }
            this.outgoingUDPMessages.DequeueCommandMessage(out disposingMessage);
            while (disposingMessage != null)
            {
                this.udpIOMessagesPool.Recycle(disposingMessage);
                this.outgoingUDPMessages.DequeueCommandMessage(out disposingMessage);
            }

            ///*****************Cleaning the chat system.
            if (this.chatSystem != null)
            {
                this.chatSystem.Release();
                this.chatSystem = null;
            }

            ///Disabling the timer.
            this.udpPurgeTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
            this.tcpPurgeTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);

            if (arg == null)
            {
                this.OnUserDisconnected(this, new KSPMEventArgs(KSPMEventArgs.EventType.Disconnect, KSPMEventArgs.EventCause.NiceDisconnect));
            }
            else
            {
                this.OnUserDisconnected(this, (KSPMEventArgs)arg);
            }

            KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Disconnected after {1} seconds alive.", this.id, this.AliveTime / 1000));

            ///Setting it as an invalid network entity.
            this.currentStatus = ClientStatus.None;
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

        #endregion

        #region ErrorHandling

        /// <summary>
        /// Trheaded method to handle those runtime errors.
        /// </summary>
        protected void HandleErrorsThreadMethod()
        {
            System.Exception runtimeError = null;
            System.Type exceptionKind = null;
            if (!this.ableToRun)
            {
                KSPMGlobals.Globals.Log.WriteTo(Error.ErrorType.ClientUnableToRun.ToString());
                return;
            }
            try
            {
                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Starting to handle errors.", this.id));
                while (this.aliveFlag)
                {
                    if (this.runtimeErrors.Count != 0)
                    {
                        runtimeError = this.runtimeErrors.Dequeue();
                        exceptionKind = runtimeError.GetType();
                        if (runtimeError != null)
                        {
                            if (exceptionKind.Equals(typeof(System.NullReferenceException)))
                            {
                            }
                            else if (exceptionKind.Equals(typeof(SocketException)))///Something happened to the connection.
                            {
                                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] ===Socket ERROR=== {1}.", this.id, runtimeError.Message));
                                this.BreakConnections(this, new KSPMEventArgs(KSPMEventArgs.EventType.RuntimeError, KSPMEventArgs.EventCause.ErrorByException));
                            }
                        }
                    }
                    Thread.Sleep(5);
                }
            }
            catch (ThreadAbortException)
            {
                this.aliveFlag = false;
            }
        }

        #endregion

        #region Setters/Getters

        /// <summary>
        /// Gets the pairing code used during the connection process.
        /// </summary>
        public int PairingCode
        {
            get
            {
                return this.pairingCode;
            }
        }

        /// <summary>
        /// Gets the GameUser who is using the GameClient.
        /// </summary>
        public GameUser ClientOwner
        {
            get
            {
                return this.clientOwner;
            }
        }

        /// <summary>
        /// Gets the chat system on this GameClient reference.
        /// </summary>
        public ChatManager ChatSystem
        {
            get
            {
                return this.chatSystem;
            }
        }

        /// <summary>
        /// Gets the TCP command queue of the outgoing messages.
        /// </summary>
        public CommandQueue OutgoingTCPQueue
        {
            get
            {
                return this.outgoingTCPMessages;
            }
        }

        /// <summary>
        /// Gets the UDP command queue of the outgoing messages.
        /// </summary>
        public CommandQueue OutgoingUDPQueue
        {
            get
            {
                return this.outgoingUDPMessages;
            }
        }

        /// <summary>
        /// Tells if the GameClient is still running.
        /// </summary>
        /// <returns></returns>
        public override bool IsAlive()
        {
            return this.aliveFlag;
        }

        /// <summary>
        /// Gets the messges pool used to send/receive messages through the UDP connection.
        /// </summary>
        public MessagesPool UDPIOMessagesPool
        {
            get
            {
                return this.udpIOMessagesPool;
            }
        }

        /// <summary>
        /// Gets the ServerInformation to which this client is connected.
        /// <returns>Null if there is not connected.</returns>
        /// </summary>
        public ServerInformation ConnectedTo
        {
            get
            {
                return this.gameServerInformation;
            }
        }

        #endregion

        #region UserManagement

        /// <summary>
        /// Event raised when an user has connected to the server.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void OnUserDisconnected(NetworkEntity sender, KSPMEventArgs e)
        {
            if (this.UserDisconnected != null)
            {
                this.UserDisconnected(sender, e);
            }
        }

        #endregion
    }
}