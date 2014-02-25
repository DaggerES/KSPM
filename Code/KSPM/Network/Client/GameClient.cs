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
        public enum ClientStatus : byte { None = 0, Handshaking, Authenticating, UDPSettingUp, Awaiting};

        /// <summary>
        /// ManualResetEvent reference to manage the signaling among the threads and the async methods.
        /// </summary>
        protected readonly ManualResetEvent TCPSignalHandler = new ManualResetEvent(false);

        /// <summary>
        /// Tells the current status of the client.
        /// </summary>
        protected ClientStatus currentStatus;

        /// <summary>
        /// Reference to the game user which is using the multiplayer, and from whom its information would be get.
        /// </summary>
        protected GameUser clientOwner;

        /// <summary>
        /// Holds information requiered to stablish a connection to the PC who is hosting the game server.
        /// </summary>
        protected ServerInformation gameServer;

        /// <summary>
        /// Flag to tells if everything has been set up and the client is ready to run.
        /// </summary>
        protected bool ableToRun;

        /// <summary>
        /// Flag to control the client's life cycle.
        /// </summary>
        protected bool aliveFlag;

        /// <summary>
        /// Tells which method will be used to stablish the connection.
        /// </summary>
        NATTraversal natMethod;

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
        /// Thread to handle the outgoing messages.
        /// </summary>
        protected Thread handleOutgoingTCPMessagesThread;

        /// <summary>
        /// Server information to connect through UDP
        /// </summary>
        protected ServerInformation udpServerInformation;

        /// <summary>
        /// Pairing code.
        /// </summary>
        protected int pairingCode;

        /// <summary>
        /// Protected constructor I still don't know how it should work.
        /// </summary>
        public GameClient() : base()
        {
            this.clientOwner = null;
            this.gameServer = null;
            this.ableToRun = false;
            this.aliveFlag = false;

            this.currentStatus = ClientStatus.None;

            ///Threading code
            this.mainBodyThread = new Thread(new ThreadStart(this.HandleMainBodyThreadMethod));
            this.handleOutgoingTCPMessagesThread = new Thread(new ThreadStart(this.HandleOutgoingTCPMessagesThreadMethod));

            this.commandsQueue = new CommandQueue();
            this.outgoingTCPMessages = new CommandQueue();

            ///Initializing the TCP Network
            this.ownerNetworkCollection = new NetworkBaseCollection(ClientSettings.ClientBufferSize);
        }

        public void SetGameUser(GameUser gameUserReference)
        {
            this.clientOwner = gameUserReference;
        }

        public void SetServerHostInformation(ServerInformation hostInformation)
        {
            this.gameServer = hostInformation;
        }

        /// <summary>
        /// Initializes everything needed to work, as threads.
        /// </summary>
        /// <returns></returns>
        public Error.ErrorType InitializeClient()
        {
            try
            {
                this.aliveFlag = true;
                this.mainBodyThread.Start();
                this.handleOutgoingTCPMessagesThread.Start();
            }
            catch (System.Exception) { }
            return Error.ErrorType.Ok;
        }

        public Error.ErrorType Connect()
        {
            Thread connectThread;
            ///Checking if the required information is already provided.
            if (this.clientOwner == null)
            {
                KSPMGlobals.Globals.Log.WriteTo(Error.ErrorType.ClientInvalidGameUser.ToString());
                return Error.ErrorType.ClientInvalidGameUser;
            }
            if (this.gameServer == null)
            {
                KSPMGlobals.Globals.Log.WriteTo(Error.ErrorType.ClientInvalidServerInformation.ToString());
                return Error.ErrorType.ClientInvalidServerInformation;
            }
            this.ownerNetworkCollection.socketReference = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.ownerNetworkCollection.socketReference.Bind(new IPEndPoint(IPAddress.Any, ClientSettings.ClientTCPPort));
            connectThread = new Thread(new ThreadStart(this.HandleConnectThreadMethod));
            connectThread.Start();
            connectThread.Join();
            if (this.natMethod.Status == NATTraversal.NATStatus.Connected)
            {

            }
            return Error.ErrorType.Ok;
        }

        protected void HandleConnectThreadMethod()
        {
            bool connected = false;
            Message outgoingMessage = null;
            ManagedMessage managedMessageReference = null;
            KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Starting to connect...", this.id));
            this.natMethod.Punch(ref this.ownerNetworkCollection.socketReference, this.gameServer.ip, this.gameServer.port);
            KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] {1}...", this.id, this.natMethod.Status.ToString()));

            while (!connected)
            {
                switch (this.currentStatus)
                {
                    case ClientStatus.Handshaking:
                        Message.NewUserMessage(this, out outgoingMessage);
                        managedMessageReference = (ManagedMessage)outgoingMessage;
                        PacketHandler.EncodeRawPacket(ref managedMessageReference.OwnerNetworkEntity.ownerNetworkCollection.rawBuffer);
                        managedMessageReference.OwnerNetworkEntity.SetMessageSentCallback(this.AuthenticateClient);
                        this.outgoingTCPMessages.EnqueueCommandMessage(ref outgoingMessage);
                        this.currentStatus = ClientStatus.Awaiting;
                        break;
                    case ClientStatus.Authenticating:
                        this.AuthenticateClient(this, null);
                        this.currentStatus = ClientStatus.Awaiting;
                        break;
                    case ClientStatus.UDPSettingUp:

                        break;
                    case ClientStatus.Awaiting:
                        break;
                }
                Thread.Sleep(11);
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
                while (this.aliveFlag)
                {
                    this.TCPSignalHandler.Reset();
                    this.ownerNetworkCollection.socketReference.BeginReceive( this.ownerNetworkCollection.secondaryRawBuffer, 0, this.ownerNetworkCollection.secondaryRawBuffer.Length, SocketFlags.None, this.AsyncTCPReceiver, this );
                    this.TCPSignalHandler.WaitOne();

                    if (!this.commandsQueue.IsEmpty())
                    {
                        this.commandsQueue.DequeueCommandMessage(out command);
                        switch (command.Command)
                        {
                            case Message.CommandType.Handshake:///NewClient command accepted, proceed to authenticate.
                                this.currentStatus = ClientStatus.Authenticating;
                                break;
                            case Message.CommandType.UDPSettingUp:///Create the UDP conn.
                                managedMessageReference = (ManagedMessage)command;
                                this.udpServerInformation.port = System.BitConverter.ToInt32(managedMessageReference.OwnerNetworkEntity.ownerNetworkCollection.secondaryRawBuffer, 5);
                                this.udpServerInformation.ip = ((IPEndPoint)managedMessageReference.OwnerNetworkEntity.ownerNetworkCollection.socketReference.RemoteEndPoint).Address.ToString();
                                this.pairingCode = System.BitConverter.ToInt32(managedMessageReference.OwnerNetworkEntity.ownerNetworkCollection.secondaryRawBuffer, 9);
                                this.currentStatus = ClientStatus.UDPSettingUp;
                                break;
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

        /// <summary>
        /// Handles the outgoing messages through the TCP connection.
        /// </summary>
        protected void HandleOutgoingTCPMessagesThreadMethod()
        {
            if (!this.ableToRun)
            {
                KSPMGlobals.Globals.Log.WriteTo(Error.ErrorType.ClientUnableToRun.ToString());
                return;
            }
            try
            {
                while (this.aliveFlag)
                {
                    Thread.Sleep(7);
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
                            KSPMGlobals.Globals.KSPMServer.commandsQueue.EnqueueCommandMessage(ref incomingMessage);
                        }
                    }
                }
            }
            catch (System.Exception)///Catch any exception thrown by the Socket.EndReceive method, mostly the ObjectDisposedException which is thrown when the thread is aborted and the socket is closed.
            {
            }
        }

        public void AsyncTCPSender(System.IAsyncResult resul)
        {
        }

        protected void ShutdownClient()
        {
            this.mainBodyThread.Abort();
            this.mainBodyThread.Join();
            KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Killed mainthread...", this.id));

            this.handleOutgoingTCPMessagesThread.Abort();
            this.handleOutgoingTCPMessagesThread.Join();
            KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Killed handledOutgoingTCPThread...", this.id));

            this.mainBodyThread = null;
            this.handleOutgoingTCPMessagesThread = null;

            ///***********************Sockets code
            if (this.ownerNetworkCollection.socketReference != null && this.ownerNetworkCollection.socketReference.Connected)
            {
                this.ownerNetworkCollection.socketReference.Disconnect(false);
                this.ownerNetworkCollection.socketReference.Close();
            }
            this.ownerNetworkCollection.Dispose();
            this.ownerNetworkCollection = null;

            if (this.clientOwner != null)
            {
                this.clientOwner.Release();
                this.clientOwner = null;
            }

            this.commandsQueue.Purge(false);
            this.outgoingTCPMessages.Purge(false);

            this.ableToRun = false;
            this.aliveFlag = false;

            KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Client killed!!!", this.id));
        }

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
