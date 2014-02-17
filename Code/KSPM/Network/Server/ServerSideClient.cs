using System.Net;
using System.Net.Sockets;
using System.Threading;

using KSPM.Network.Common;
using KSPM.Network.Common.Packet;
using KSPM.Globals;
using KSPM.Game;


namespace KSPM.Network.Server
{
    /// <summary>
    /// Represents a client handled by the server.
    /// </summary>
    public class ServerSideClient : NetworkEntity
    {
        /// <summary>
        /// ServerSide status.
        /// </summary>
        public enum ClientStatus : byte { Handshaking = 0, Handshaked, Authenticating, Authenticated, UDPConnecting, Connected, AwaitingACK, AwaitingReply, UDPSettingUp };
        protected enum MessagesThreadStatus : byte { None = 0, AwaitingReply, ListeningForCommands };

        /// <summary>
        /// Thread to run the main body of the thread.
        /// </summary>
        protected Thread mainThread;

        /// <summary>
        /// Thread to handle the incoming messages.
        /// </summary>
        protected Thread messageHandlerTread;

        /// <summary>
        /// Constrols the mainThread lifecycle.
        /// </summary>
        protected bool aliveFlag;

        /// <summary>
        /// Tells if the client is ready to run.
        /// </summary>
        protected bool ableToRun;

        /// <summary>
        /// Tells the current stage of the mainThread.
        /// </summary>
        protected ClientStatus currentStatus;

        /// <summary>
        /// Tells the current status if the messages thread, if it is awaiting a reply or awating a command.
        /// </summary>
        protected MessagesThreadStatus commandStatus;

        /// <summary>
        /// A reference to the game user, this is kind a second level of the KSPM model.
        /// I have made it public to perform fastest implementations.
        /// </summary>
        public User gameUser;

        #region UDP

        /// <summary>
        /// UDP socket to handle the non-oriented packages.
        /// </summary>
        public NetworkBaseCollection udpCollection;

        /// <summary>
        /// Holds the udp information about the remote client.
        /// </summary>
        protected IPEndPoint udpRemoteNetworkInformation;

        /// <summary>
        /// Thread to handle the incoming packages.
        /// </summary>
        protected Thread udpListeningThread;

        /// <summary>
        /// Thread to handle the outgoing packages.
        /// </summary>
        protected Thread udpOutgoingHandlerThread;

        /// <summary>
        /// Tells if the udp socket is properly set and fully operational.
        /// </summary>
        protected bool usingUdpConnection;

        /// <summary>
        /// Pairing code used to test the udp connection with the remote client.
        /// </summary>
        protected int pairingCode;

        #endregion

        #region InitializingCode

        /// <summary>
        /// Creates a ServerSideReference.
        /// </summary>
        protected ServerSideClient() : base()
        {
            this.currentStatus = ClientStatus.Handshaking;
            this.mainThread = new Thread(new ThreadStart(this.HandleMainBodyMethod));
            this.messageHandlerTread = new Thread(new ThreadStart(this.HandleIncomingMessagesMethod));

            this.udpListeningThread = new Thread(new ThreadStart(this.HandleIncomingUDPPacketsThreadMethod));
            this.udpOutgoingHandlerThread = new Thread(new ThreadStart(this.HandleOutgoingUDPPacketsThreadMethod));

            this.commandStatus = MessagesThreadStatus.None;
            this.ableToRun = true;
            this.usingUdpConnection = false;
        }

        /// <summary>
        /// Creates a ServerSideCliente object from a NetworkEntity reference and then disclose the network entity.
        /// </summary>
        /// <param name="baseNetworkEntity">Reference (ref) to the NetwrokEntity used as a base to create the new ServerSideClient object.</param>
        /// <param name="ssClient">New server side clint out reference.</param>
        /// <returns></returns>
        public static Error.ErrorType CreateFromNetworkEntity(ref NetworkEntity baseNetworkEntity, out ServerSideClient ssClient )
        {
            ssClient = null;
            if (baseNetworkEntity == null)
            {
                return Error.ErrorType.InvalidNetworkEntity;
            }
            ssClient = new ServerSideClient();
            ssClient.ownerSocket = baseNetworkEntity.ownerSocket;
            ssClient.rawBuffer = baseNetworkEntity.rawBuffer;
            ssClient.secondaryRawBuffer = baseNetworkEntity.secondaryRawBuffer;
            ssClient.id = baseNetworkEntity.Id;
            baseNetworkEntity.Dispose();
            return Error.ErrorType.Ok;
        }

        /// <summary>
        /// Creates a pairing code using a Random generator, so it is slow and take care about how much you use it.
        /// </summary>
        public int CreatePairingCode()
        {
            System.Random rand = new System.Random((int) System.DateTime.Now.Ticks & 0x0000FFFF);
            this.pairingCode = rand.Next();
            rand = null;
            return this.pairingCode;
        }

        #endregion

        /// <summary>
        /// Handles the main behaviour of the server side client.
        /// </summary>
        protected void HandleMainBodyMethod()
        {
            Message tempMessage = null;
            NetworkEntity myNetworkEntityReference = this;

            if (!this.ableToRun)
            {
                KSPMGlobals.Globals.Log.WriteTo(Error.ErrorType.ServerClientUnableToRun.ToString());
                return;
            }
            KSPMGlobals.Globals.Log.WriteTo("Going alive " + this.ownerSocket.RemoteEndPoint.ToString());
            this.aliveFlag = true;
            while (this.aliveFlag)
            {
                switch (this.currentStatus)
                {
                        ///This is the starting status of each ServerSideClient.
                    case ClientStatus.Handshaking:
                        Message.HandshakeAccetpMessage(ref myNetworkEntityReference, out tempMessage);
                        PacketHandler.EncodeRawPacket(ref myNetworkEntityReference);
                        KSPMGlobals.Globals.KSPMServer.outgoingMessagesQueue.EnqueueCommandMessage(ref tempMessage);
                        this.currentStatus = ClientStatus.AwaitingReply;
                        this.commandStatus = MessagesThreadStatus.AwaitingReply;
                        //Awaiting for the Authentication message coming from the remote client.
                        break;
                    case ClientStatus.AwaitingReply:
                        break;
                    case ClientStatus.Authenticated:
                        this.currentStatus = ClientStatus.UDPSettingUp;
                        this.commandStatus = MessagesThreadStatus.ListeningForCommands;
                        break;
                    case ClientStatus.Connected:

                        break;
                    case ClientStatus.Handshaked:
                        this.commandStatus = MessagesThreadStatus.ListeningForCommands;
                        break;
                }
                Thread.Sleep(3);
            }
        }

        #region TCPCode

        /// <summary>
        /// Receives the incoming messages and pass them to the server to be processed.
        /// </summary>
        protected void HandleIncomingMessagesMethod()
        {
            int receivedBytes = 0;
            Message incomingMessage = null;
            NetworkEntity ownNetworkEntity = this;
            if (!this.ableToRun)
            {
                KSPMGlobals.Globals.Log.WriteTo(Error.ErrorType.ServerClientUnableToRun.ToString());
                return;
            }
            try
            {
                while (this.aliveFlag)
                {
                    switch (this.commandStatus)
                    {
                        case MessagesThreadStatus.AwaitingReply:
                            //KSPMGlobals.Globals.Log.WriteTo(this.ownerSocket.Poll(1000, SelectMode.SelectRead).ToString());
                            if (this.ownerSocket.Poll(500, SelectMode.SelectRead))
                            {
                                KSPMGlobals.Globals.Log.WriteTo("READING...");
                                receivedBytes = this.ownerSocket.Receive(this.secondaryRawBuffer, this.secondaryRawBuffer.Length, SocketFlags.None);
                                if (receivedBytes > 0)
                                {
                                    if (PacketHandler.DecodeRawPacket(ref this.secondaryRawBuffer) == Error.ErrorType.Ok)
                                    {
                                        if (PacketHandler.InflateMessage(ref ownNetworkEntity, out incomingMessage) == Error.ErrorType.Ok)
                                        {
                                            incomingMessage.SetOwnerMessageNetworkEntity(ref ownNetworkEntity);
                                            KSPMGlobals.Globals.KSPMServer.commandsQueue.EnqueueCommandMessage(ref incomingMessage);
                                            this.commandStatus = MessagesThreadStatus.None;
                                        }
                                    }
                                    receivedBytes = -1;
                                }
                            }
                            break;
                        case MessagesThreadStatus.ListeningForCommands:
                            if (this.ownerSocket.Poll(500, SelectMode.SelectRead))
                            {
                                KSPMGlobals.Globals.Log.WriteTo("READING Command...");
                                receivedBytes = this.ownerSocket.Receive(this.secondaryRawBuffer, this.secondaryRawBuffer.Length, SocketFlags.None);
                                if (receivedBytes > 0)
                                {
                                    if (PacketHandler.DecodeRawPacket(ref this.secondaryRawBuffer) == Error.ErrorType.Ok)
                                    {
                                        if (PacketHandler.InflateMessage(ref ownNetworkEntity, out incomingMessage) == Error.ErrorType.Ok)
                                        {
                                            incomingMessage.SetOwnerMessageNetworkEntity(ref ownNetworkEntity);
                                            KSPMGlobals.Globals.KSPMServer.commandsQueue.EnqueueCommandMessage(ref incomingMessage);
                                            this.commandStatus = MessagesThreadStatus.None;
                                        }
                                    }
                                    receivedBytes = -1;
                                }
                            }
                            break;
                        case MessagesThreadStatus.None:
                            break;
                    }
                    Thread.Sleep(3);
                }
            }
            catch (ThreadAbortException)
            {
                this.aliveFlag = false;
            }
        }

        #endregion

        #region UDPCode

        /// <summary>
        /// Initializes the udp socket to listening mode.
        /// </summary>
        /// <returns>If there is some exception caught the return is ServerClientUnableToRun</returns>
        protected Error.ErrorType InitializeUDPConnection()
        {
            IPEndPoint remoteNetInformation;
            this.udpCollection = new NetworkBaseCollection(ServerSettings.ServerBufferSize);
            this.udpCollection.socketReference = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            remoteNetInformation = (IPEndPoint)this.ownerSocket.RemoteEndPoint;
            this.udpRemoteNetworkInformation = new IPEndPoint(remoteNetInformation.Address, 0);//0 because It should be any available port.
            try
            {
                this.udpCollection.socketReference.Bind(this.udpRemoteNetworkInformation);
                this.usingUdpConnection = false;
            }
            catch (System.Exception ex)
            {
                KSPMGlobals.Globals.Log.WriteTo(ex.Message);
                return Error.ErrorType.ServerClientUnableToRun;
            }
            return Error.ErrorType.Ok;
        }

        /// <summary>
        /// Handles the incoming udp packets.
        /// </summary>
        protected void HandleIncomingUDPPacketsThreadMethod()
        {
            if (!this.ableToRun)
            {
                KSPMGlobals.Globals.Log.WriteTo(Error.ErrorType.ServerClientUnableToRun.ToString());
                return;
            }
            try
            {
                while (this.aliveFlag)
                {
                    if (this.usingUdpConnection)
                    {

                    }
                    Thread.Sleep(3);
                }
            }
            catch (ThreadAbortException)
            {
                this.usingUdpConnection = false;
                this.aliveFlag = false;
            }
        }

        /// <summary>
        /// Handle the outgoing packets, such as broadcast the packet to the other clients.
        /// </summary>
        protected void HandleOutgoingUDPPacketsThreadMethod()
        {
            if (!this.ableToRun)
            {
                KSPMGlobals.Globals.Log.WriteTo(Error.ErrorType.ServerClientUnableToRun.ToString());
                return;
            }
            try
            {
                while (this.aliveFlag)
                {
                    if (this.usingUdpConnection)
                    {

                    }
                    Thread.Sleep(3);
                }
            }
            catch (ThreadAbortException)
            {
                this.usingUdpConnection = false;
                this.aliveFlag = false;
            }
        }
        #endregion

        #region Management

        /// <summary>
        /// Starts the client so it is going to be able to live in another thread.
        /// </summary>
        /// <returns></returns>
        public bool StartClient()
        {
            bool result = false;
            if (!this.ableToRun)
            {
                KSPMGlobals.Globals.Log.WriteTo(Error.ErrorType.ServerClientUnableToRun.ToString());
                return false;
            }
            try
            {
                this.mainThread.Start();
                this.messageHandlerTread.Start();

                this.udpListeningThread.Start();
                this.udpOutgoingHandlerThread.Start();

                result = true;
            }
            catch( System.Exception ex )
            {
                KSPMGlobals.Globals.Log.WriteTo(ex.Message);
                this.ableToRun = false;
            }
            return result;
        }

        /// <summary>
        /// Stops the current client and make it unable to run again.
        /// </summary>
        public void ShutdownClient()
        {
            ///***********************Killing threads code
            this.aliveFlag = false;
            this.mainThread.Abort();
            this.mainThread.Join();
            KSPMGlobals.Globals.Log.WriteTo(string.Format( "[{0}] Killed mainthread...", this.mainThread.Name));
            this.messageHandlerTread.Abort();
            this.messageHandlerTread.Join(1000);
            KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Killed messagesThread...", this.messageHandlerTread.Name));
            this.udpListeningThread.Abort();
            this.udpListeningThread.Join();
            KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Killed udpListeningThread...", this.udpListeningThread.Name));
            this.udpOutgoingHandlerThread.Abort();
            this.udpOutgoingHandlerThread.Join();
            KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Killed udpOutgoingHandlerThread...", this.udpOutgoingHandlerThread.Name));

            ///***********************Sockets code
            if (this.ownerSocket != null && this.ownerSocket.Connected)
            {
                this.ownerSocket.Disconnect(false);
                this.ownerSocket.Shutdown(SocketShutdown.Both);
                this.ownerSocket.Close();
            }
            this.ownerSocket = null;
            this.rawBuffer = null;

            this.ableToRun = false;

            KSPMGlobals.Globals.Log.WriteTo("ServerSide Client killed!!!");
        }

        /// <summary>
        /// Changes the Awating status from the client, this does not require be thread safe.
        /// </summary>
        /// <param name="newStatus"></param>
        public void RemoveAwaitingState(ServerSideClient.ClientStatus newStatus)
        {
            this.currentStatus = newStatus;
        }

        /// <summary>
        /// Overrides the Release method and stop threads and other stuff inside the object.
        /// </summary>
        public override void Release()
        {
            this.ShutdownClient();
        }

        #endregion

        #region Setters/Getters

        public int PairingCode
        {
            get
            {
                return this.pairingCode;
            }
        }

        #endregion
    }
}
