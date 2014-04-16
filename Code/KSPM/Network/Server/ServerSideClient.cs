using System.Net;
using System.Net.Sockets;
using System.Threading;

using KSPM.Network.Common;
using KSPM.Network.Common.Packet;
using KSPM.Network.Common.Messages;
using KSPM.Network.Common.Events;
using KSPM.Globals;
using KSPM.Game;


namespace KSPM.Network.Server
{
    /// <summary>
    /// Represents a client handled by the server.
    /// </summary>
    public class ServerSideClient : NetworkEntity, IPacketArrived, IUDPPacketArrived
    {
        /// <summary>
        /// ServerSide status.
        /// </summary>
        public enum ClientStatus : byte { Handshaking = 0, Authenticated, Connected, Awaiting, UDPSettingUp };

        /// <summary>
        /// Delegate to runs the connection process.
        /// </summary>
        protected delegate void ConnectAsync();

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
        /// Pool of SocketAsyncEventArgs used to receive tcp streams.
        /// </summary>
        SocketAsyncEventArgsPool tcpIOEventsPool;

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
        /// Pool of SocketAsyncEventArgs used to receive tcp streams.
        /// </summary>
        SharedBufferSAEAPool udpIOEventsPool;

        MessagesPool udpIOMessagesPool;

        #endregion

        #region UserHandling

        /// <summary>
        /// 
        /// </summary>
        public event UserConnectedEventHandler UserConnected;

        /// <summary>
        /// A reference to the game user, this is kind a second level of the KSPM model.
        /// I have made it public to perform fastest implementations.
        /// </summary>
        public User gameUser;

        #endregion

        #region UDP

        /// <summary>
        /// UDP socket to handle the non-oriented packages.
        /// </summary>
        public ConnectionlessNetworkCollection udpCollection;

        /// <summary>
        /// Tells if the udp socket is properly set and fully operational.
        /// </summary>
        protected bool usingUdpConnection;

        /// <summary>
        /// Pairing code used to test the udp connection with the remote client.
        /// </summary>
        protected int pairingCode;

        /// <summary>
        /// Flag which tells if the ServerSideClient has finished the connection process with the remote client.
        /// </summary>
        protected bool connected;

        /// <summary>
        /// Tells if the references is marked to be killed. Avoids to send twice or more the disconnect message.
        /// </summary>
        protected bool markedToDie;

        /// <summary>
        /// UDPMessages queue to hold those incoming packets.
        /// </summary>
        protected CommandQueue incomingPackets;

        /// <summary>
        /// UDPMessages queue to be send to the remote client.
        /// </summary>
        public CommandQueue outgoingPackets;

        #endregion

        #region Profiling

        KSPM.IO.Logging.DiagnosticsLog reporter;

        #endregion

        #region InitializingCode

        /// <summary>
        /// Creates a ServerSideReference, only initializes those properties required to work with TCP connections.
        /// </summary>
        protected ServerSideClient() : base()
        {
            this.currentStatus = ClientStatus.Handshaking;

            this.ableToRun = true;
            this.usingUdpConnection = false;

            this.udpCollection = null;
            ///Setting an invalid pairing code.
            this.pairingCode = -1;

            ///Set to null, because inside GameServer this property is set to a proper reference.
            this.gameUser = null;

            this.connected = false;

            this.incomingPackets = new CommandQueue();
            this.outgoingPackets = new CommandQueue();

            ///TCP Buffering
            this.tcpBuffer = new IO.Memory.CyclicalMemoryBuffer(ServerSettings.PoolingCacheSize, (uint)ServerSettings.ServerBufferSize);
            this.packetizer = new PacketHandler(this.tcpBuffer);
            this.tcpIOEventsPool = new SocketAsyncEventArgsPool(ServerSettings.PoolingCacheSize);

            ///UDP Buffering
            this.udpBuffer = new IO.Memory.CyclicalMemoryBuffer(ServerSettings.PoolingCacheSize, (uint)ServerSettings.ServerBufferSize);
            this.udpPacketizer = new PacketHandler(this.udpBuffer);
            this.udpIOEventsPool = new SharedBufferSAEAPool(ServerSettings.PoolingCacheSize, 1024);
            this.udpIOMessagesPool = new MessagesPool(ServerSettings.PoolingCacheSize * 1000, new RawMessage(Message.CommandType.Null, null, 0));

            this.markedToDie = false;


            this.timer = new System.Diagnostics.Stopwatch();
            this.timer.Start();
        }

        /// <summary>
        /// Creates a ServerSideCliente object from a NetworkEntity reference and then disclose the network entity.
        /// </summary>
        /// <param name="baseNetworkEntity">Reference (ref) to the NetwrokEntity used as a base to create the new ServerSideClient object.</param>
        /// <param name="ssClient">New server side clint out reference.</param>
        /// <returns></returns>
        public static Error.ErrorType CreateFromNetworkEntity(NetworkEntity baseNetworkEntity, out ServerSideClient ssClient )
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

        #region ConnectionCode

        /// <summary>
        /// Handles the main behaviour of the server side client.
        /// </summary>
        protected void HandleConnectionProcess()
        {
            Message tempMessage = null;
            NetworkEntity myNetworkEntityReference = this;
            bool thisThreadAlive = true;

            if (!this.ableToRun)
            {
                KSPMGlobals.Globals.Log.WriteTo(Error.ErrorType.ServerClientUnableToRun.ToString());
                return;
            }
			KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}]Going alive {1}", this.id, this.ownerNetworkCollection.socketReference.RemoteEndPoint.ToString()));
            while (thisThreadAlive)
            {
                switch (this.currentStatus)
                {
                        ///This is the starting status of each ServerSideClient.
                    case ClientStatus.Handshaking:
                        Message.HandshakeAccetpMessage(myNetworkEntityReference, out tempMessage);
                        PacketHandler.EncodeRawPacket(ref tempMessage.bodyMessage);
                        KSPMGlobals.Globals.KSPMServer.priorityOutgoingMessagesQueue.EnqueueCommandMessage(ref tempMessage);
                        this.currentStatus = ClientStatus.Awaiting;
                        //Awaiting for the Authentication message coming from the remote client.
                        break;
                    case ClientStatus.Awaiting:
                        break;
                    case ClientStatus.Authenticated:
                        this.currentStatus = ClientStatus.UDPSettingUp;
                        Message.UDPSettingUpMessage(myNetworkEntityReference, out tempMessage);
                        PacketHandler.EncodeRawPacket(ref tempMessage.bodyMessage);
                        KSPMGlobals.Globals.KSPMServer.priorityOutgoingMessagesQueue.EnqueueCommandMessage(ref tempMessage);
                        KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}]{1} Pairing code", this.Id, System.Convert.ToString(this.pairingCode, 2)));
                        this.usingUdpConnection = true;
                        //this.ReceiveUDPDatagramNoSAEA();
                        this.ReceiveUDPDatagram();

                        break;
                    case ClientStatus.Connected:
                        KSPMGlobals.Globals.KSPMServer.chatManager.RegisterUser(this, Chat.Managers.ChatManager.UserRegisteringMode.Public);
                        KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}]{1} has connected", this.Id, this.gameUser.Username));
                        Message.SettingUpChatSystem(this, KSPMGlobals.Globals.KSPMServer.chatManager.AvailableGroupList, out tempMessage);
                        PacketHandler.EncodeRawPacket(ref tempMessage.bodyMessage);
                        KSPMGlobals.Globals.KSPMServer.priorityOutgoingMessagesQueue.EnqueueCommandMessage(ref tempMessage);
                        KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Setting up KSPM Chat system.", this.Id));
                        this.connected = true;
                        this.currentStatus = ClientStatus.Awaiting;
                        thisThreadAlive = false;
                        break;
                }
                if ( !this.connected && this.timer.ElapsedMilliseconds > ServerSettings.ConnectionProcessTimeOut && !this.markedToDie)
                {
                    this.markedToDie = true;
                    KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] Connection process has taken too long: {1}.", this.id, this.timer.ElapsedMilliseconds));
                    KSPMGlobals.Globals.KSPMServer.DisconnectClient(this);
                    thisThreadAlive = false;
                }
                Thread.Sleep(3);
            }
        }

        protected void AsyncConnectionProccesComplete(System.IAsyncResult result)
        {
            ConnectAsync caller = (ConnectAsync)result.AsyncState;
            caller.EndInvoke(result);
            this.OnUserConnected(null);
            KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}]Connection complete", this.id));
        }

        #endregion

        #region TCPCode

        protected void ReceiveTCPStream()
        {
            SocketAsyncEventArgs incomingData = this.tcpIOEventsPool.NextSlot;
            incomingData.AcceptSocket = this.ownerNetworkCollection.socketReference;
            incomingData.SetBuffer(this.ownerNetworkCollection.secondaryRawBuffer, 0, this.ownerNetworkCollection.secondaryRawBuffer.Length);
            incomingData.Completed += new System.EventHandler<SocketAsyncEventArgs>(this.OnTCPIncomingDataComplete);
            try
            {
                if (!this.ownerNetworkCollection.socketReference.ReceiveAsync(incomingData))
                {
                    this.OnTCPIncomingDataComplete(this, incomingData);
                }
            }
            catch (System.ObjectDisposedException ex)
            {
                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}][\"{1}:{2}\"] Something went wrong with the remote client, performing a removing process on it.", this.id, "ReceiveTCPStream", ex.Message));
                KSPMGlobals.Globals.KSPMServer.DisconnectClient(this);
            }
            catch (SocketException ex)
            {
                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}][\"{1}-{2}:{3}\"] Something went wrong with the remote client, performing a removing process on it.", this.id, "ReceiveTCPStream", ex.SocketErrorCode, ex.Message));
                KSPMGlobals.Globals.KSPMServer.DisconnectClient(this);
            }
        }

        protected void OnTCPIncomingDataComplete(object sender, SocketAsyncEventArgs e)
        {
            int readBytes = 0;
            if (e.SocketError == SocketError.Success)
            {
                readBytes = e.BytesTransferred;
                if (readBytes > 0)
                {
                    this.tcpBuffer.Write(e.Buffer, (uint)readBytes);
                    this.packetizer.PacketizeCRC(this);
                    this.ReceiveTCPStream();
                }
                else
                {
                    ///If BytesTransferred is 0, it means that there is no more bytes to be read, so the remote socket was
                    ///disconnected.
                    KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}][\"{1}\"] Remote client disconnected, performing a removing process on it.", this.id, "OnTCPIncomingDataComplete"));
                    KSPMGlobals.Globals.KSPMServer.DisconnectClient(this);
                }
            }
            else
            {
                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}][\"{1}:{2}\"] Something went wrong with the remote client, performing a removing process on it.", this.id, "OnTCPIncomingDataComplete", e.SocketError));
                KSPMGlobals.Globals.KSPMServer.DisconnectClient(this);
            }
            ///Either we have success reading the incoming data or not we need to recycle the SocketAsyncEventArgs used to perform this reading process.
            e.Completed -= this.OnTCPIncomingDataComplete;
            if (this.tcpIOEventsPool == null)///Means that the reference has been killed. So we have to release this SocketAsyncEventArgs by hand.
            {
                e.Dispose();
                e = null;
            }
            else
            {
                this.tcpIOEventsPool.Recycle(e);
            }
        }

        public void ProcessPacket(byte[] rawData, uint fixedLegth)
        {
            Message incomingMessage = null;
            //KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo(fixedLegth.ToString());
            if (PacketHandler.InflateManagedMessageAlt(rawData, this, out incomingMessage) == Error.ErrorType.Ok)
            {
                if (this.connected)///If everything is already set up, commands go to the common queue.
                {
                    KSPMGlobals.Globals.KSPMServer.commandsQueue.EnqueueCommandMessage(ref incomingMessage);
                }
                else
                {
                    KSPMGlobals.Globals.KSPMServer.localCommandsQueue.EnqueueCommandMessage(ref incomingMessage);
                }
            }
        }

        public void ProcessPacket(byte[] rawData, uint rawDataOffset, uint fixedLength)
        {
            Message incomingMessage = null;
            //KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo(fixedLength.ToString());
            if (this.connected)
            {
                incomingMessage = KSPMGlobals.Globals.KSPMServer.incomingMessagesPool.BorrowMessage;
                ((BufferedMessage)incomingMessage).Load(rawData, rawDataOffset, fixedLength);
                ((BufferedMessage)incomingMessage).SetOwnerMessageNetworkEntity(this);
                KSPMGlobals.Globals.KSPMServer.commandsQueue.EnqueueCommandMessage(ref incomingMessage);
            }
            else
            {
                incomingMessage = KSPMGlobals.Globals.KSPMServer.priorityMessagesPool.BorrowMessage;
                ((BufferedMessage)incomingMessage).Load(rawData, rawDataOffset, fixedLength);
                ((BufferedMessage)incomingMessage).SetOwnerMessageNetworkEntity(this);
                KSPMGlobals.Globals.KSPMServer.localCommandsQueue.EnqueueCommandMessage(ref incomingMessage);
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
                this.udpCollection.socketReference.IOControl((IOControlCode)(-1744830452), new byte[] { 0, 0, 0, 0 }, null);
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


        protected void ReceiveUDPDatagramNoSAEA()
        {
            EndPoint src = this.udpCollection.remoteEndPoint;
            try
            {
                this.udpCollection.socketReference.BeginReceiveFrom(this.udpCollection.secondaryRawBuffer, 0, this.udpCollection.secondaryRawBuffer.Length, SocketFlags.None, ref src, this.AsyncUDPReceiveCallback, this);
            }
            catch (System.ObjectDisposedException ex)
            {
                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}][\"{1}:{2}\"] Something went wrong with the remote client, performing a removing process on it.", this.id, "ReceiveUDPDatagram", ex.Message));
                KSPMGlobals.Globals.KSPMServer.DisconnectClient(this);
            }
            catch (SocketException ex)
            {
                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}][\"{1}-{2}:{3}\"] Something went wrong with the remote client, performing a removing process on it.", this.id, "ReceiveUDPDatagram", ex.SocketErrorCode, ex.Message));
                KSPMGlobals.Globals.KSPMServer.DisconnectClient(this);
            }
        }

        protected void AsyncUDPReceiveCallback(System.IAsyncResult result)
        {
            int readBytes;
            ServerSideClient ssReference = null;
            try
            {
                ssReference = (ServerSideClient)result.AsyncState;
                readBytes = ssReference.udpCollection.socketReference.EndReceiveFrom(result, ref this.udpCollection.remoteEndPoint);
                if (readBytes > 0)
                {
                    this.udpBuffer.Write(this.udpCollection.secondaryRawBuffer, (uint)readBytes);
                    this.udpPacketizer.UDPPacketizeCRCMemoryAlloc(this);
                    this.ReceiveUDPDatagramNoSAEA();
                }
            }
            catch (System.Exception ex)
            {
                KSPMGlobals.Globals.Log.WriteTo(ex.Message);
            }
        }

        protected void ReceiveUDPDatagram()
        {
            SocketAsyncEventArgs incomingData = this.udpIOEventsPool.NextSlot;
            byte[] buffersito = new byte[1024];
            if (this.udpCollection.socketReference == null)
            {
                int a;
            }
            incomingData.AcceptSocket = this.udpCollection.socketReference;
            incomingData.RemoteEndPoint = this.udpCollection.remoteEndPoint;
            incomingData.SetBuffer(0, 1024);
            //incomingData.SetBuffer(this.udpCollection.secondaryRawBuffer, 0, this.udpCollection.secondaryRawBuffer.Length);
            if (incomingData.Buffer == null)
            {
                int b = 0;
            }
            incomingData.Completed += new System.EventHandler<SocketAsyncEventArgs>(this.OnUDPIncomingDataComplete);
            try
            {
                if (!this.udpCollection.socketReference.ReceiveFromAsync(incomingData))
                {
                    this.OnUDPIncomingDataComplete(this, incomingData);
                }
            }
            catch (System.ObjectDisposedException ex)
            {
                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}][\"{1}:{2}\"] Something went wrong with the remote client, performing a removing process on it.", this.id, "ReceiveUDPDatagram", ex.Message));
                KSPMGlobals.Globals.KSPMServer.DisconnectClient(this);
            }
            catch (SocketException ex)
            {
                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}][\"{1}-{2}:{3}\"] Something went wrong with the remote client, performing a removing process on it.", this.id, "ReceiveUDPDatagram", ex.SocketErrorCode, ex.Message));
                KSPMGlobals.Globals.KSPMServer.DisconnectClient(this);
            }
        }

        protected void OnUDPIncomingDataComplete(object sender, SocketAsyncEventArgs e)
        {
            int readBytes = 0;
            if (e.SocketError == SocketError.Success )//  || e.SocketError == SocketError.Fault || e.SocketError == SocketError.InvalidArgument)
            {
                readBytes = e.BytesTransferred;
                if (readBytes > 0)
                {
                    if (e.Buffer == null)
                    {
                        int a = 0;
                    }
                    this.udpBuffer.Write(e.Buffer, (uint)readBytes);
                    ///Setting the sender of the datagram.
                    this.udpCollection.remoteEndPoint = e.RemoteEndPoint;
                    this.udpPacketizer.UDPPacketizeCRCMemoryAlloc(this);
                    this.ReceiveUDPDatagram();
                }/*
                else
                {
                    ///If BytesTransferred is 0, it means that there is no more bytes to be read, so the remote socket was
                    ///disconnected.
                    KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}][\"{1}\"] Remote client disconnected, performing a removing process on it.", this.id, "OnUDPIncomingDataComplete"));
                    KSPMGlobals.Globals.KSPMServer.DisconnectClient(this);
                }
                */
            }
            else
            {
                //if (e.SocketError != SocketError.Fault)
                //{
                    KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}][\"{1}:{2}\"] Something went wrong with the remote client, performing a removing process on it.", this.id, "OnUDPIncomingDataComplete", e.SocketError));
                    KSPMGlobals.Globals.KSPMServer.DisconnectClient(this);
                //}
            }
            ///Either we have success reading the incoming data or not we need to recycle the SocketAsyncEventArgs used to perform this reading process.
            e.Completed -= this.OnUDPIncomingDataComplete;
            if (this.udpIOEventsPool == null)///Means that the reference has been killed. So we have to release this SocketAsyncEventArgs by hand.
            {
                e.Dispose();
                e = null;
            }
            else
            {
                this.udpIOEventsPool.Recycle(e);
            }
        }

        public void ProcessUDPPacket(byte[] rawData, uint fixedLegth)
        {
            Message incomingMessage;
            KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo(fixedLegth.ToString());
            /*
            if (this.currentStatus == ClientStatus.UDPSettingUp)
            {
                if (PacketHandler.InflateRawMessage(rawData, out incomingMessage) == Error.ErrorType.Ok)
                {
                    ///Puting the incoming RawMessage into the queue to be processed.
                    this.incomingPackets.EnqueueCommandMessage(ref incomingMessage);
                    this.ProcessUDPCommand();
                }
            }*/
            if (PacketHandler.InflateRawMessage(rawData, out incomingMessage) == Error.ErrorType.Ok)
            {
                ///Puting the incoming RawMessage into the queue to be processed.
                this.incomingPackets.EnqueueCommandMessage(ref incomingMessage);
                this.ProcessUDPCommand();
            }
        }

        protected void ProcessUDPCommand()
        {
            Message incomingMessage = null;
            Message responseMessage = null;
            RawMessage rawMessageReference = null;
            int intBuffer;
            this.incomingPackets.DequeueCommandMessage(out incomingMessage);
            if (incomingMessage != null)
            {
                rawMessageReference = (RawMessage)incomingMessage;
                switch (incomingMessage.Command)
                {
                    case Message.CommandType.UDPPairing:
                        intBuffer = System.BitConverter.ToInt32(rawMessageReference.bodyMessage, (int)PacketHandler.PrefixSize + 1);
                        responseMessage = this.udpIOMessagesPool.BorrowMessage;
                        KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}]{1} Received Pairing code", this.Id, System.Convert.ToString(intBuffer, 2)));
                        if ((this.pairingCode & intBuffer) == 0)//UDP tested.
                        {
                            Message.LoadUDPPairingOkMessage(this, ref responseMessage);
                            if (responseMessage != null)
                            {
                                rawMessageReference = (RawMessage)responseMessage;
                                this.outgoingPackets.EnqueueCommandMessage(ref responseMessage);
                                this.currentStatus = ClientStatus.Connected;
                            }
                        }
                        else
                        {
                            Message.LoadUDPPairingFailMessage(this, ref responseMessage);
                            if (responseMessage != null)
                            {
                                rawMessageReference = (RawMessage)responseMessage;
                                this.outgoingPackets.EnqueueCommandMessage(ref responseMessage);
                                this.currentStatus = ClientStatus.Connected;
                            }
                        }
                        this.SendUDPDatagram();
                        incomingMessage.Release();
                        incomingMessage = null;
                        break;
                    case Message.CommandType.UDPChat:
                        KSPMGlobals.Globals.KSPMServer.OnUDPMessageArrived(this, rawMessageReference);
                        break;
                    default:
                        KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] {1} unknown command", this.id, incomingMessage.Command.ToString()));
                        break;
                }
                //this.SendUDPDatagram();
            }
        }

        public void SendAsDatagram(Message message)
        {
            Message outgoingMessage = null;
            SocketAsyncEventArgs outgoingData = null;
            if (!this.ableToRun)
            {
                KSPMGlobals.Globals.Log.WriteTo(Error.ErrorType.ServerClientUnableToRun.ToString());
                return;
            }
            try
            {
                if (this.usingUdpConnection)
                {
                    //this.outgoingPackets.DequeueCommandMessage(out outgoingMessage);
                    ///Taking a message from the pool.
                    outgoingMessage = this.udpIOMessagesPool.BorrowMessage;
                    ///Loading the message with the proper content.
                    ((RawMessage)outgoingMessage).LoadWith(message.bodyMessage, 0, message.MessageBytesSize);

                    if (outgoingMessage != null)
                    {
                        outgoingData = this.udpIOEventsPool.NextSlot;
                        outgoingData.AcceptSocket = this.udpCollection.socketReference;
                        outgoingData.RemoteEndPoint = this.udpCollection.remoteEndPoint;
                        outgoingData.SetBuffer(outgoingMessage.bodyMessage, 0, (int)outgoingMessage.MessageBytesSize);
                        outgoingData.Completed += new System.EventHandler<SocketAsyncEventArgs>(this.OnUDPSendingDataComplete);
                        outgoingData.UserToken = outgoingMessage;
                        this.udpCollection.socketReference.SendToAsync(outgoingData);
                    }
                }
                Thread.Sleep(3);
            }
            catch (SocketException ex)///Something happened to the remote client, so it is required to this ServerSideClient to kill itself.
            {
                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}][\"{1}-{2}:{3}\"] Something went wrong with the remote client, performing a removing process on it.", this.id, "SendAsDatagram", ex.SocketErrorCode, ex.Message));
                KSPMGlobals.Globals.KSPMServer.DisconnectClient(this);
            }
        }

        protected void SendUDPDatagram()
        {
            Message outgoingMessage = null;
            SocketAsyncEventArgs outgoingData = null;
            if (!this.ableToRun)
            {
                KSPMGlobals.Globals.Log.WriteTo(Error.ErrorType.ServerClientUnableToRun.ToString());
                return;
            }
            try
            {
                if (this.usingUdpConnection)
                {
                    this.outgoingPackets.DequeueCommandMessage(out outgoingMessage);
                    if (outgoingMessage != null)
                    {
                        outgoingData = this.udpIOEventsPool.NextSlot;
                        outgoingData.AcceptSocket = this.udpCollection.socketReference;
                        outgoingData.RemoteEndPoint = this.udpCollection.remoteEndPoint;
                        outgoingData.SetBuffer(outgoingMessage.bodyMessage, 0, (int)outgoingMessage.MessageBytesSize);
                        outgoingData.Completed += new System.EventHandler<SocketAsyncEventArgs>(this.OnUDPSendingDataComplete);
                        outgoingData.UserToken = outgoingMessage;
                        this.udpCollection.socketReference.SendToAsync(outgoingData);
                    }
                }
                Thread.Sleep(3);
            }
            catch (SocketException ex)///Something happened to the remote client, so it is required to this ServerSideClient to kill itself.
            {
                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}][\"{1}-{2}:{3}\"] Something went wrong with the remote client, performing a removing process on it.", this.id, "SendUDPDatagram", ex.SocketErrorCode, ex.Message));
                KSPMGlobals.Globals.KSPMServer.DisconnectClient(this);
            }
        }

        protected void OnUDPSendingDataComplete(object sender, SocketAsyncEventArgs e)
        {
            int sentBytes = 0;
            if (e.SocketError == SocketError.Success)
            {
                sentBytes = e.BytesTransferred;
                if (sentBytes > 0)
                {
                    KSPMGlobals.Globals.Log.WriteTo(sentBytes.ToString());
                }
            }
            else
            {
                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}][\"{1}:{2}\"] Something went wrong with the remote client, performing a removing process on it.", this.id, "OnUDPSendingDataComplete", e.SocketError));
                KSPMGlobals.Globals.KSPMServer.DisconnectClient(this);
            }
            ///Either we have have sucess sending the data, it's required to recycle the outgoing message.
            this.udpIOMessagesPool.Recycle((Message)e.UserToken);
            ///Either we have success sending the incoming data or not we need to recycle the SocketAsyncEventArgs used to perform this reading process.
            e.Completed -= this.OnUDPSendingDataComplete;
            if (this.udpIOEventsPool == null)///Means that the reference has been killed. So we have to release this SocketAsyncEventArgs by hand.
            {
                e.Dispose();
                e = null;
            }
            else
            {
                this.udpIOEventsPool.Recycle(e);
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

                ConnectAsync connectionProcess = new ConnectAsync(this.HandleConnectionProcess);
                connectionProcess.BeginInvoke(this.AsyncConnectionProccesComplete, connectionProcess);

                this.ReceiveTCPStream();

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
            this.ableToRun = false;
            this.connected = false;
            this.markedToDie = true;

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

            ///Cleaning up the UDP queues;
            this.outgoingPackets.Purge(false);
            this.incomingPackets.Purge(false);

            this.tcpBuffer.Release();
            this.tcpBuffer = null;

            this.tcpIOEventsPool.Release(false);
            this.tcpIOEventsPool = null;

            this.udpIOMessagesPool.Release();

            KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] ServerSide Client killed after {1} seconds alive.", this.id, this.AliveTime / 1000));
            
            this.timer.Reset();
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

        /// <summary>
        /// Returns the alive flag value.
        /// </summary>
        /// <returns></returns>
        public override bool IsAlive()
        {
            return this.aliveFlag;
        }

        public SocketAsyncEventArgsPool IOSocketAsyncEventArgsPool
        {
            get
            {
                return this.tcpIOEventsPool;
            }
        }

        #endregion

        #region UserManagement

        public void RegisterUserConnectedEvent(UserConnectedEventHandler eventReference)
        {
            if (eventReference == null)
            {
                return;
            }
            this.UserConnected = eventReference;
        }

        protected void OnUserConnected(KSPMEventArgs e)
        {
            if (this.UserConnected != null)
            {
                this.UserConnected(this, e);
            }
        }

        #endregion
    }
}
