using System.Net;
using System.Net.Sockets;
using System.Threading;

using KSPM.Network.Common;
using KSPM.Network.Common.Packet;
using KSPM.Network.Common.Messages;
using KSPM.Globals;
using KSPM.Game;


namespace KSPM.Network.Server
{
    /// <summary>
    /// Represents a client handled by the server.
    /// </summary>
    public class ServerSideClient : NetworkEntity, IAsyncReceiver, IAsyncSender, IAsyncTCPReceiver
    {
        /// <summary>
        /// ServerSide status.
        /// </summary>
        public enum ClientStatus : byte { Handshaking = 0, Authenticated, Connected, Awaiting, UDPSettingUp };

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
        /// A reference to the game user, this is kind a second level of the KSPM model.
        /// I have made it public to perform fastest implementations.
        /// </summary>
        public User gameUser;

        #region UDP

        /// <summary>
        /// UDP socket to handle the non-oriented packages.
        /// </summary>
        public ConnectionlessNetworkCollection udpCollection;

        /// <summary>
        /// Thread to handle the incoming packages.
        /// </summary>
        protected Thread udpListeningThread;

        /// <summary>
        /// Thread to handle the outgoing packages.
        /// </summary>
        protected Thread udpOutgoingHandlerThread;

        /// <summary>
        /// Thread to handle UDP commands.
        /// </summary>
        protected Thread udpHandlingCommandsThread;

        /// <summary>
        /// Tells if the udp socket is properly set and fully operational.
        /// </summary>
        protected bool usingUdpConnection;

        /// <summary>
        /// Pairing code used to test the udp connection with the remote client.
        /// </summary>
        protected int pairingCode;

        /// <summary>
        /// UDPMessages queue to hold those incoming packets.
        /// </summary>
        protected CommandQueue incomingPackets;

        /// <summary>
        /// UDPMessages queue to be send to the remote client.
        /// </summary>
        public CommandQueue outgoingPackets;

        #endregion

        #region ThreadingProperties
        /// <summary>
        /// ManualResetEvent reference to manage the signaling among the threads and the async methods.
        /// </summary>
        protected readonly ManualResetEvent UDPSignalHandler = new ManualResetEvent(false);

        /// <summary>
        /// ManualResetEvent reference to manage the signaling among the threads which handle the TCP connections.
        /// </summary>
        protected readonly ManualResetEvent TCPSignalHandler = new ManualResetEvent(false);

        #endregion

        #region InitializingCode

        /// <summary>
        /// Creates a ServerSideReference, only initializes those properties required to work with TCP connections.
        /// </summary>
        protected ServerSideClient() : base()
        {
            this.currentStatus = ClientStatus.Handshaking;
            this.mainThread = new Thread(new ThreadStart(this.HandleMainBodyMethod));
            this.messageHandlerTread = new Thread(new ThreadStart(this.HandleIncomingMessagesMethod));

            this.udpListeningThread = new Thread(new ThreadStart(this.HandleIncomingUDPPacketsThreadMethod));
            this.udpOutgoingHandlerThread = new Thread(new ThreadStart(this.HandleOutgoingUDPPacketsThreadMethod));
            this.udpHandlingCommandsThread = new Thread(new ThreadStart(this.HandleUDPCommandsThreadMethod));

            this.ableToRun = true;
            this.usingUdpConnection = false;

            this.udpCollection = null;
            ///Setting an invalid pairing code.
            this.pairingCode = -1;

            ///Set to null, because inside GameServer this property is set to a proper reference.
            this.gameUser = null;

            this.incomingPackets = new CommandQueue();
            this.outgoingPackets = new CommandQueue();
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
            baseNetworkEntity.ownerNetworkCollection.Clone(out ssClient.ownerNetworkCollection);
            ssClient.id = baseNetworkEntity.Id;
            baseNetworkEntity.Dispose();
            ssClient.InitializeUDPConnection();
            return Error.ErrorType.Ok;
        }

        #endregion


        /// <summary>
        /// Handles the main behaviour of the server side client.
        /// </summary>
        protected void HandleMainBodyMethod()
        {
            Message tempMessage = null;
            ManagedMessage managedMessageReference = null;
            NetworkEntity myNetworkEntityReference = this;

            if (!this.ableToRun)
            {
                KSPMGlobals.Globals.Log.WriteTo(Error.ErrorType.ServerClientUnableToRun.ToString());
                return;
            }
			KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}]Going alive {1}", this.id, this.ownerNetworkCollection.socketReference.RemoteEndPoint.ToString()));
            while (this.aliveFlag)
            {
                switch (this.currentStatus)
                {
                        ///This is the starting status of each ServerSideClient.
                    case ClientStatus.Handshaking:
                        Message.HandshakeAccetpMessage(myNetworkEntityReference, out tempMessage);
                        managedMessageReference = (ManagedMessage)tempMessage;
                        PacketHandler.EncodeRawPacket(ref managedMessageReference.OwnerNetworkEntity.ownerNetworkCollection.rawBuffer);
                        KSPMGlobals.Globals.KSPMServer.outgoingMessagesQueue.EnqueueCommandMessage(ref tempMessage);
                        this.currentStatus = ClientStatus.Awaiting;
                        //Awaiting for the Authentication message coming from the remote client.
                        break;
                    case ClientStatus.Awaiting:
                        break;
                    case ClientStatus.Authenticated:
                        //Thread.Sleep(1000);
                        this.currentStatus = ClientStatus.UDPSettingUp;
                        Message.UDPSettingUpMessage(myNetworkEntityReference, out tempMessage);
                        managedMessageReference = (ManagedMessage)tempMessage;
                        PacketHandler.EncodeRawPacket(ref managedMessageReference.OwnerNetworkEntity.ownerNetworkCollection.rawBuffer);
                        KSPMGlobals.Globals.KSPMServer.outgoingMessagesQueue.EnqueueCommandMessage(ref tempMessage);
                        KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}]{1} Pairing code", this.Id, System.Convert.ToString(this.pairingCode, 2)));
                        this.usingUdpConnection = true;
                        break;
                    case ClientStatus.Connected:
                        KSPMGlobals.Globals.KSPMServer.chatManager.RegisterUser(this, Chat.Managers.ChatManager.UserRegisteringMode.Public);
                        KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}]{1} has connected", this.Id, this.gameUser.Username));
                        Message.SettingUpChatSystem(this, KSPMGlobals.Globals.KSPMServer.chatManager.AvailableGroupList, out tempMessage);
                        managedMessageReference = (ManagedMessage)tempMessage;
                        PacketHandler.EncodeRawPacket(ref managedMessageReference.OwnerNetworkEntity.ownerNetworkCollection.rawBuffer);
                        KSPMGlobals.Globals.KSPMServer.outgoingMessagesQueue.EnqueueCommandMessage(ref tempMessage);
                        KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Setting up KSPM Chat system.", this.Id));
                        this.currentStatus = ClientStatus.Awaiting;
                        break;
                }
                Thread.Sleep(3);
            }
        }

        #region TCPCode

        /// <summary>
        /// Receives the incoming messages on the TCP protocol and passes them to the server to be processed.
        /// </summary>
        protected void HandleIncomingMessagesMethod()
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
                    this.TCPSignalHandler.Reset();
                    this.ownerNetworkCollection.socketReference.BeginReceive(this.ownerNetworkCollection.secondaryRawBuffer, 0, this.ownerNetworkCollection.secondaryRawBuffer.Length, SocketFlags.None, this.AsyncTCPReceiver, this);
                    this.TCPSignalHandler.WaitOne();
                    Thread.Sleep(3);
                }
            }
            catch (ThreadAbortException)
            {
                this.aliveFlag = false;
            }
            catch (SocketException)///Something happened to the remote client, so it is required to this ServerSideClient to kill itself.
            {
                Message killMessage = null;
                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Something went wrong with the remote client, performing a removing process on it.", this.id));
                Message.DisconnectMessage(this, out killMessage);
                KSPMGlobals.Globals.KSPMServer.commandsQueue.EnqueueCommandMessage(ref killMessage);
            }
        }

        /// <summary>
        /// Method used to receive Messages through the TCP socket.
        /// </summary>
        /// <param name="result">Holds a reference to this object.</param>
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

        #endregion

        #region UDPCode

        /// <summary>
        /// Initializes the udp socket to listening mode.
        /// </summary>
        /// <returns>If there is some exception caught the return is ServerClientUnableToRun</returns>
        protected Error.ErrorType InitializeUDPConnection()
        {
            IPEndPoint udpLocalEndPoint;
            this.udpCollection = new ConnectionlessNetworkCollection(ServerSettings.ServerBufferSize);
            this.udpCollection.socketReference = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            udpLocalEndPoint = (IPEndPoint)this.ownerNetworkCollection.socketReference.LocalEndPoint;
            try
            {
                this.udpCollection.socketReference.Bind(new IPEndPoint(udpLocalEndPoint.Address, 0));//0 because It should be any available port.
                this.usingUdpConnection = false;
            }
            catch (System.Exception ex)
            {
                KSPMGlobals.Globals.Log.WriteTo(ex.Message);
                return Error.ErrorType.ServerClientUnableToRun;
            }
            return Error.ErrorType.Ok;
        }

        protected void HandleUDPCommandsThreadMethod()
        {
            Message incomingMessage = null;
            Message responseMessage = null;
            RawMessage rawMessageReference = null;
            int intBuffer;
            if (!this.ableToRun)
            {
                KSPMGlobals.Globals.Log.WriteTo(Error.ErrorType.ServerClientUnableToRun.ToString());
                return;
            }
            try
            {
                while (this.aliveFlag)
                {
                    if (!this.incomingPackets.IsEmpty())
                    {
                        this.incomingPackets.DequeueCommandMessage(out incomingMessage);
                        rawMessageReference = (RawMessage)incomingMessage;
                        switch (incomingMessage.Command)
                        {
                            case Message.CommandType.UDPPairing:
                                intBuffer = System.BitConverter.ToInt32(rawMessageReference.bodyMessage, (int)PacketHandler.RawMessageHeaderSize + 1);
                                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}]{1} Received Pairing code", this.Id, System.Convert.ToString(intBuffer, 2)));
                                if ((this.pairingCode & intBuffer) == 0)//UDP tested.
                                {
                                    Message.UDPPairingOkMessage(this, out responseMessage);
                                    if (responseMessage != null)
                                    {
                                        rawMessageReference = (RawMessage)responseMessage;
                                        if (PacketHandler.EncodeRawPacket(ref rawMessageReference.bodyMessage) == Error.ErrorType.Ok)
                                        {
                                            this.outgoingPackets.EnqueueCommandMessage(ref responseMessage);
                                            this.currentStatus = ClientStatus.Connected;
                                        }
                                    }
                                }
                                else
                                {
                                    Message.UDPPairingFailMessage(this, out responseMessage);
                                    if (responseMessage != null)
                                    {
                                        rawMessageReference = (RawMessage)responseMessage;
                                        if (PacketHandler.EncodeRawPacket(ref rawMessageReference.bodyMessage) == Error.ErrorType.Ok)
                                        {
                                            this.outgoingPackets.EnqueueCommandMessage(ref responseMessage);
                                            this.currentStatus = ClientStatus.Connected;
                                        }
                                    }
                                }
                                break;
                            default:
                                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] {1} unknown command", this.id, incomingMessage.Command.ToString()));
                                break;
                        }
                    }
                }
                Thread.Sleep(3);
            }
            catch (ThreadAbortException)
            {
                this.usingUdpConnection = false;
                this.aliveFlag = false;
            }
        }

        /// <summary>
		/// Handles the incoming udp packets.<b>Not using Socket.BeginReceiveMessageFrom method, because it is not implemented yet inside Mono, instead is used Socket.BeginReceiveFrom method.</b>
        /// </summary>
        protected void HandleIncomingUDPPacketsThreadMethod()
        {
            EndPoint remoteEndPoint = null;
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
                        this.UDPSignalHandler.Reset();
                        remoteEndPoint = this.udpCollection.socketReference.LocalEndPoint;
						this.udpCollection.socketReference.BeginReceiveFrom( this.udpCollection.secondaryRawBuffer, 0, this.udpCollection.secondaryRawBuffer.Length, SocketFlags.None, ref remoteEndPoint, this.AsyncReceiverCallback, this );
                        this.UDPSignalHandler.WaitOne();
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

        public void AsyncReceiverCallback(System.IAsyncResult result)
        {
            int readBytes;
            Message incomingMessage = null;
            this.UDPSignalHandler.Set();
            ServerSideClient ssClientReference = (ServerSideClient)result.AsyncState;
            try
            {
                if (ssClientReference.udpCollection.socketReference != null)
                {
                    readBytes = ssClientReference.udpCollection.socketReference.EndReceiveFrom(result, ref this.udpCollection.remoteEndPoint);
                    if (readBytes > 0)
                    {
                        if (this.currentStatus == ClientStatus.UDPSettingUp)
                        {
                            if (PacketHandler.DecodeRawPacket(ref ssClientReference.udpCollection.secondaryRawBuffer) == Error.ErrorType.Ok)
                            {
                                if (PacketHandler.InflateRawMessage(ssClientReference.udpCollection.secondaryRawBuffer, out incomingMessage) == Error.ErrorType.Ok)
                                {
                                    this.incomingPackets.EnqueueCommandMessage(ref incomingMessage);
                                }
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)///Catch any exception thrown by the Socket.EndReceive method, mostly the ObjectDisposedException which is thrown when the thread is aborted and the socket is closed.
            {
                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}]===AsyncReceiverCallback_Error==={1}.", this.id, ex.Message));
            }
        }

        /// <summary>
        /// Handle the outgoing packets, such as broadcast the packet to the other clients.
        /// </summary>
        protected void HandleOutgoingUDPPacketsThreadMethod()
        {
            Message outgoingMessage = null;
            RawMessage rawMessage = null;
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
                        if (!this.outgoingPackets.IsEmpty())
                        {
                            this.outgoingPackets.DequeueCommandMessage(out outgoingMessage);
                            rawMessage = (RawMessage)outgoingMessage;
                            if( outgoingMessage != null )
                            {
                                this.udpCollection.socketReference.BeginSendTo(rawMessage.bodyMessage, 0, (int)rawMessage.MessageBytesSize, SocketFlags.None, this.udpCollection.remoteEndPoint, this.AsyncSenderCallback, this);
                            }
                            outgoingMessage.Release();
                        }
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

        public void AsyncSenderCallback(System.IAsyncResult result)
        {
            int sentBytes;
            ServerSideClient owner = null;
            try
            {
                owner = (ServerSideClient)result.AsyncState;
                sentBytes = owner.udpCollection.socketReference.EndSendTo(result);
            }
            catch (System.Exception)
            {
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
				this.aliveFlag = true;

                this.mainThread.Start();
                this.messageHandlerTread.Start();

                this.udpListeningThread.Start();
                this.udpOutgoingHandlerThread.Start();
                this.udpHandlingCommandsThread.Start();

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
            KSPMGlobals.Globals.Log.WriteTo(string.Format( "[{0}] Killed mainthread.", this.id));
            this.messageHandlerTread.Abort();
            this.messageHandlerTread.Join(1000);
            KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Killed messagesThread.", this.id));
            this.udpListeningThread.Abort(1000);
            this.udpListeningThread.Join();
            KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Killed udpListeningThread.", this.id));
            this.udpOutgoingHandlerThread.Abort();
            this.udpOutgoingHandlerThread.Join();
            KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Killed udpOutgoingHandlerThread.", this.id));
            this.udpHandlingCommandsThread.Abort();
            this.udpHandlingCommandsThread.Join();
            KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Killed udpCommandsHandlerThread.", this.id));
            this.mainThread = null;
            this.messageHandlerTread = null;
            this.udpListeningThread = null;
            this.udpOutgoingHandlerThread = null;
            this.udpHandlingCommandsThread = null;

            ///***********************Sockets code
            if (this.ownerNetworkCollection.socketReference != null)
            {
                if (this.ownerNetworkCollection.socketReference.Connected)
                {
                    this.ownerNetworkCollection.socketReference.Disconnect(false);
                }
                this.ownerNetworkCollection.socketReference.Close();
            }
            this.ownerNetworkCollection.Dispose();
            this.ownerNetworkCollection = null;

            if (this.udpCollection.socketReference != null)
            {
                this.udpCollection.socketReference.Close();
            }
            this.udpCollection.Dispose();
            this.udpCollection = null;

            if (this.gameUser != null)
            {
                this.gameUser.Release();
                this.gameUser = null;
            }

            this.ableToRun = false;

            ///Cleaning up the UDP queues;
            this.outgoingPackets.Purge(false);
            this.incomingPackets.Purge(false);

            KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] ServerSide Client killed!!!", this.id));
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

        /// <summary>
        /// Gets the paring code. <b>If it is the first time you call it, a valid paring code will be created by the CreatePairingCode method.</b>
        /// </summary>
        public int PairingCode
        {
            get
            {
                if (this.pairingCode < 0)
                    this.pairingCode = this.CreatePairingCode();
                return this.pairingCode;
            }
        }

        /// <summary>
        /// Creates a pairing code using a Random generator, so it is slow and take care about how much you use it.
        /// </summary>
        public int CreatePairingCode()
        {
            System.Random rand = new System.Random((int)System.DateTime.Now.Ticks & 0x0000FFFF);
            this.pairingCode = rand.Next();
            rand = null;
            return this.pairingCode;
        }

        #endregion
    }
}
