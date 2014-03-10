using System.Net.Sockets;
using System.Net;

using KSPM.Network.Common;
using KSPM.Network.Common.Messages;
using KSPM.Network.Common.Packet;
using KSPM.Game;
using KSPM.Globals;
using KSPM.Network.NAT;
using KSPM.Network.Client.RemoteServer;

using System.Threading;

namespace KSPM.Network.Client
{
    /// <summary>
    /// Class to represent the remote client.
    /// </summary>
    public class GameClient : NetworkEntity, IAsyncTCPReceiver, IAsyncTCPSender, IAsyncReceiver, IAsyncSender
    {
        public enum ClientStatus : byte { None = 0, Rebind, Handshaking, Authenticating, UDPSettingUp, Awaiting, Connected};

        /// <summary>
        /// Client settings to be used to work.
        /// </summary>
        public ClientSettings workingSettings;

        /// <summary>
        /// ManualResetEvent reference to manage the signaling among the threads and the async methods.
        /// </summary>
        protected readonly ManualResetEvent TCPSignalHandler = new ManualResetEvent(false);

        /// <summary>
        /// ManualResetEvento reference to manage the signaling among the udp threads.
        /// </summary>
        protected readonly ManualResetEvent UDPSignalHandler = new ManualResetEvent(false);

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

        #endregion

        #region UDPPRoperties

        /// <summary>
        /// Thread to handle the outgoing UDP messages.
        /// </summary>
        protected Thread handleOutgoingUDPMessagesThread;

        /// <summary>
        /// Thread to handle the incoming UDP messages.
        /// </summary>
        protected Thread handleIncomingUDPMessagesThread;

        /// <summary>
        /// Thread to handle all the UDP commands.
        /// </summary>
        protected Thread handleUDPCommandsThread;

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

        #endregion

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

            this.currentStatus = ClientStatus.None;

            ///Threading code
            this.mainBodyThread = new Thread(new ThreadStart(this.HandleMainBodyThreadMethod));
            this.handleOutgoingTCPMessagesThread = new Thread(new ThreadStart(this.HandleOutgoingTCPMessagesThreadMethod));
            this.handleIncomingTCPMessagesThread = new Thread(new ThreadStart(this.HandleIncomingTCPMessagesThreadMethod));

            this.handleIncomingUDPMessagesThread = new Thread(new ThreadStart(this.HandleIncomingUDPMessagesThreadMethod));
            this.handleOutgoingUDPMessagesThread = new Thread(new ThreadStart(this.HandleOutgoingUDPMessagesThreadMethod));
            this.handleUDPCommandsThread = new Thread(new ThreadStart(this.HandleUDPCommandsThreadMethod));

            this.commandsQueue = new CommandQueue();
            this.outgoingTCPMessages = new CommandQueue();

            this.incomingUDPMessages = new CommandQueue();
            this.outgoingUDPMessages = new CommandQueue();

            ///Initializing the TCP Network
            this.ownerNetworkCollection = new NetworkBaseCollection(ClientSettings.ClientBufferSize);
            ///Initializing the UDP Network
            this.udpNetworkCollection = new NetworkBaseCollection(ClientSettings.ClientBufferSize);

            ///Network init
            this.udpServerInformation = new ServerInformation();
            this.usingUDP = false;
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

                this.mainBodyThread.Start();
                this.handleOutgoingTCPMessagesThread.Start();
                this.handleIncomingTCPMessagesThread.Start();

                this.handleIncomingUDPMessagesThread.Start();
                this.handleOutgoingUDPMessagesThread.Start();
                this.handleUDPCommandsThread.Start();
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
                    KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Connection timeout.", this.id));
                    this.BreakConnections(this, null);
                }
                if (!this.holePunched)
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
            RawMessage rawMessageReference = null;
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
                            try
                            {
                                this.udpNetworkCollection.socketReference = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                                this.udpNetworkCollection.socketReference.Bind(new IPEndPoint(IPAddress.Any, this.workingSettings.udpPort));
                                KSPMGlobals.Globals.NAT.Punch(ref this.udpNetworkCollection.socketReference, this.udpServerInformation.ip, this.udpServerInformation.port);
                                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] UDP hole status: {1}.", this.id, KSPMGlobals.Globals.NAT.Status.ToString()));
                                this.udpHolePunched = KSPMGlobals.Globals.NAT.Status == NATTraversal.NATStatus.Connected;
                                if (this.udpHolePunched)
                                {
                                    Message.UDPPairingMessage(this, out outgoingMessage);
                                    rawMessageReference = (RawMessage)outgoingMessage;
                                    PacketHandler.EncodeRawPacket(ref rawMessageReference.bodyMessage);
                                    this.outgoingUDPMessages.EnqueueCommandMessage(ref outgoingMessage);
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
                            KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Connected to [{1}].", this.id, this.udpServerInformation.NetworkEndPoint.ToString()));
                            connected = true;
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
                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Disconnecting.", this.id));
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
            ServerInformation udpServerInformationFromNetwork = new ServerInformation();
            int receivedPairingCode = -1;
            if (!this.ableToRun)
            {
                KSPMGlobals.Globals.Log.WriteTo(Error.ErrorType.ClientUnableToRun.ToString());
                return;
            }
            try
            {
                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Starting to handle the main body.", this.id));
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
                                    udpServerInformationFromNetwork.port = System.BitConverter.ToInt32(managedMessageReference.OwnerNetworkEntity.ownerNetworkCollection.secondaryRawBuffer, 5);
                                    udpServerInformationFromNetwork.ip = ((IPEndPoint)managedMessageReference.OwnerNetworkEntity.ownerNetworkCollection.socketReference.RemoteEndPoint).Address.ToString();
                                    receivedPairingCode = System.BitConverter.ToInt32(managedMessageReference.OwnerNetworkEntity.ownerNetworkCollection.secondaryRawBuffer, 9);

                                    //this.pairingCode = System.BitConverter.ToInt32(managedMessageReference.OwnerNetworkEntity.ownerNetworkCollection.secondaryRawBuffer, 9);
                                    //KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] UDP pairing code. {1}", this.id, this.pairingCode));
                                    //this.pairingCode = ~this.pairingCode;
                                    if (!this.udpServerInformation.Equals(udpServerInformationFromNetwork) && this.pairingCode != receivedPairingCode)
                                    {
                                        udpServerInformationFromNetwork.Clone(ref this.udpServerInformation);
                                        this.pairingCode = ~receivedPairingCode;
                                        this.currentStatus = ClientStatus.UDPSettingUp;
                                        KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] UDP pairing code. {1}", this.id, this.pairingCode));
                                    }
                                    //this.udpServerInformation.port = System.BitConverter.ToInt32(managedMessageReference.OwnerNetworkEntity.ownerNetworkCollection.secondaryRawBuffer, 5);
                                    //this.udpServerInformation.ip = ((IPEndPoint)managedMessageReference.OwnerNetworkEntity.ownerNetworkCollection.socketReference.RemoteEndPoint).Address.ToString();
                                    break;
                            }
							///Cleaning up.
							command.Release();
							command = null;
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

        #region TCPCode

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
                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Starting to handle outgoing messages.", this.id));
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
                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Starting to handle incoming TCP messages.", this.id));
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
                            KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}]===Error==={1}.", callingEntity.ownerNetworkCollection.secondaryRawBuffer[ 4 ], incomingMessage.Command));
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
				if( sentBytes > 0 )
				{
                	networkReference.MessageSent(networkReference, null);
				}
            }
            catch (System.Exception)///Catch any exception thrown by the Socket.EndReceive method, mostly the ObjectDisposedException which is thrown when the thread is aborted and the socket is closed.
            {
            }
        }

        #endregion

        #region UDPCode

        protected void HandleIncomingUDPMessagesThreadMethod()
        {
            ///Holds the information about who is sending the packet.
            EndPoint remoteEndPoint = null;
            if (!this.ableToRun)
            {
                KSPMGlobals.Globals.Log.WriteTo(Error.ErrorType.ClientUnableToRun.ToString());
                return;
            }
            try
            {
                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Starting to handle incoming UDP messages.", this.id));
                while (this.aliveFlag)
                {
                    if (this.udpHolePunched)///Checking if the UDP hole is already punched.
                    {
                        this.UDPSignalHandler.Reset();
                        remoteEndPoint = this.udpNetworkCollection.socketReference.LocalEndPoint;
                        this.udpNetworkCollection.socketReference.BeginReceiveFrom(this.udpNetworkCollection.secondaryRawBuffer, 0, this.udpNetworkCollection.secondaryRawBuffer.Length, SocketFlags.None, ref remoteEndPoint, this.AsyncReceiverCallback, this);
                        this.UDPSignalHandler.WaitOne();
                    }
                    Thread.Sleep(11);
                }
            }
            catch (ThreadAbortException)
            {
                this.udpHolePunched = false;
                this.aliveFlag = false;
            }
        }

        public void AsyncReceiverCallback(System.IAsyncResult result)
        {
            int readBytes;
            Message incomingMessage = null;
            EndPoint receivedReference;
            this.UDPSignalHandler.Set();
            GameClient myClientRerence = (GameClient) result.AsyncState;
            try
            {
                receivedReference = new IPEndPoint(IPAddress.Any, 0);
                if (this.udpNetworkCollection.socketReference != null)
                {
                    readBytes = myClientRerence.udpNetworkCollection.socketReference.EndReceiveFrom(result, ref receivedReference);
                    if (readBytes > 0)
                    {
                        if (PacketHandler.DecodeRawPacket(ref myClientRerence.udpNetworkCollection.secondaryRawBuffer) == Error.ErrorType.Ok)
                        {
                            if (PacketHandler.InflateRawMessage(myClientRerence.udpNetworkCollection.secondaryRawBuffer, out incomingMessage) == Error.ErrorType.Ok)
                            {
                                this.incomingUDPMessages.EnqueueCommandMessage(ref incomingMessage);
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)///Catch any exception thrown by the Socket.EndReceive method, mostly the ObjectDisposedException which is thrown when the thread is aborted and the socket is closed.
            {
                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}]===Error==={1}.", this.id, ex.Message));
            }
        }

        protected void HandleOutgoingUDPMessagesThreadMethod()
        {
            Message outgoingMessage = null;
            RawMessage rawMessageReference = null;
            
            if (!this.ableToRun)
            {
                KSPMGlobals.Globals.Log.WriteTo(Error.ErrorType.ClientUnableToRun.ToString());
                return;
            }
            try
            {
                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Starting to handle outgoing UDP messages.", this.id));
                while (this.aliveFlag)
                {
                    if (this.udpHolePunched)
                    {
                        if (!this.outgoingUDPMessages.IsEmpty())
                        {
                            this.outgoingUDPMessages.DequeueCommandMessage(out outgoingMessage);
                            rawMessageReference = (RawMessage)outgoingMessage;
                            if (outgoingMessage != null)
                            {
								this.udpNetworkCollection.socketReference.BeginSendTo(rawMessageReference.bodyMessage, 0, (int)rawMessageReference.MessageBytesSize, SocketFlags.None, this.udpServerInformation.NetworkEndPoint, this.AsyncSenderCallback, this);
                                ///Cleaning up
                                outgoingMessage.Release();
                                outgoingMessage = null;
                            }
                        }
                    }
                    Thread.Sleep(7);
                }
            }
            catch (ThreadAbortException)
            {
                this.udpHolePunched = false;
                this.aliveFlag = false;
            }
        }

		/// <summary>
		/// Sends data in an async way. <b> Required to implement some way to inform to the upside levels.</b>
		/// </summary>
		/// <param name="result"></param>
        public void AsyncSenderCallback(System.IAsyncResult result)
        {
            int sentBytes;
            GameClient owner = null;
            try
            {
                owner = (GameClient)result.AsyncState;
                sentBytes = owner.udpNetworkCollection.socketReference.EndSendTo(result);
            }
			catch (System.Exception ex)
            {
				KSPMGlobals.Globals.Log.WriteTo(ex.Message);
            }
        }

        protected void HandleUDPCommandsThreadMethod()
        {
            Message command = null;
            if (!this.ableToRun)
            {
                KSPMGlobals.Globals.Log.WriteTo(Error.ErrorType.ClientUnableToRun.ToString());
                return;
            }
            try
            {
                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Starting to handle UDP commands.", this.id));
                while (this.aliveFlag)
                {
                    if (!this.incomingUDPMessages.IsEmpty())
                    {
                        this.incomingUDPMessages.DequeueCommandMessage(out command);
                        if (command != null)
                        {
                            switch (command.Command)
                            {
                                ///Means that everything works fine, so you are able to send/receive data through the UDP connection.
                                case Message.CommandType.UDPPairingOk:
                                    this.currentStatus = ClientStatus.Connected;
                                    break;
                                ///Means that the message was received by the remote server, but something were wrong, anyway the communication is stablished.
                                ///At this the Connected status is going to be set, but a Warning must be raised.
                                case  Message.CommandType.UDPPairingFail:
                                    this.currentStatus = ClientStatus.Connected;
                                    KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] '{1}' Connection stablished, but something were wrong.", this.id, command.Command.ToString()));
                                    break;
                            }
                            ///Cleaning up.
                            command.Release();
                            command = null;
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
            KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Killed mainthread.", this.id));

            this.handleOutgoingTCPMessagesThread.Abort();
            this.handleOutgoingTCPMessagesThread.Join();
            KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Killed handledOutgoingTCPThread.", this.id));

            this.handleIncomingTCPMessagesThread.Abort();
            this.handleIncomingTCPMessagesThread.Join();
            KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Killed handledIncomingTCPThread.", this.id));

            this.mainBodyThread = null;
            this.handleOutgoingTCPMessagesThread = null;
            this.handleIncomingTCPMessagesThread = null;

            this.handleUDPCommandsThread.Abort();
            this.handleUDPCommandsThread.Join();
            KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Killed handleUDPCommandsThread.", this.id));

            this.handleOutgoingUDPMessagesThread.Abort();
            this.handleOutgoingUDPMessagesThread.Join();
            KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Killed handledOutgoingUDPThread.", this.id));

            this.handleIncomingUDPMessagesThread.Abort();
            this.handleIncomingUDPMessagesThread.Join();
            KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Killed handledIncomingUDPThread.", this.id));

            this.handleUDPCommandsThread = null;
            this.handleOutgoingUDPMessagesThread = null;
            this.handleIncomingUDPMessagesThread = null;


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

        /// <summary>
        /// Closes the connections, cleans references used by the client itself, such as sockets, and buffers.
        /// </summary>
        /// <param name="caller"></param>
        /// <param name="arg"></param>
        protected void BreakConnections(NetworkEntity caller, object arg)
        {
            ///***********************Sockets code
            this.udpHolePunched = false;
            this.holePunched = false;
            this.usingUDP = false;
            if (this.ownerNetworkCollection.socketReference != null && this.ownerNetworkCollection.socketReference.Connected)
            {
                this.ownerNetworkCollection.socketReference.Shutdown(SocketShutdown.Both);
                this.ownerNetworkCollection.socketReference.Disconnect(true);
            }

            ///Closing the socket either it is connected or not.
            this.ownerNetworkCollection.socketReference.Close();
            this.ownerNetworkCollection.socketReference = null;

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
            this.incomingUDPMessages.Purge(true);
            this.outgoingUDPMessages.Purge(true);

            this.currentStatus = ClientStatus.None;

            KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Disconnected.", this.id));
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

        public int PairingCode
        {
            get
            {
                return this.pairingCode;
            }
        }
    }
}
