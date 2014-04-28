//#define PROFILING

using System.Net;
using System.Net.Sockets;
using System.Threading;

using KSPM.Network.Common;
using KSPM.Network.Common.Packet;
using KSPM.Network.Common.Messages;
using KSPM.Network.Common.Events;
using KSPM.Globals;
using KSPM.Game;

using KSPM.Diagnostics;


namespace KSPM.Network.Server
{
    /// <summary>
    /// Represents a client handled by the server.
    /// </summary>
    public class ServerSideClient : NetworkEntity, IPacketArrived, IUDPPacketArrived
    {

#if PROFILING

        Profiler profilerOutgoingMessages;
        Profiler profilerPacketizer;

#endif

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
        SocketAsyncEventArgsPool tcpInEventsPool;
        SocketAsyncEventArgsPool tcpOutEventsPool;

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

        /// <summary>
        /// Delegate to process the incoming UDP datagrams.
        /// </summary>
        protected delegate void ProcessUDPMessageAsync();

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
        /// Pool of Messages to be used to send/receive datagrams.
        /// </summary>
        protected MessagesPool udpIOMessagesPool;

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

            ///TCP Buffering
            this.tcpBuffer = new IO.Memory.CyclicalMemoryBuffer(ServerSettings.PoolingCacheSize, (uint)ServerSettings.ServerBufferSize);
            this.packetizer = new PacketHandler(this.tcpBuffer);
            this.tcpInEventsPool = new SocketAsyncEventArgsPool(ServerSettings.PoolingCacheSize / 2, this.OnTCPIncomingDataComplete);
            this.tcpOutEventsPool = new SocketAsyncEventArgsPool(ServerSettings.PoolingCacheSize / 2, KSPMGlobals.Globals.KSPMServer.OnSendingOutgoingDataComplete);

            ///Setting UDP queues.
            this.incomingPackets = new CommandQueue();
            this.outgoingPackets = new CommandQueue();

            ///UDP Buffering
            this.udpCollection = new ConnectionlessNetworkCollection(ServerSettings.ServerBufferSize);
            this.udpBuffer = new IO.Memory.CyclicalMemoryBuffer(ServerSettings.PoolingCacheSize, (uint)ServerSettings.ServerBufferSize);
            this.udpPacketizer = new PacketHandler(this.udpBuffer);
            this.udpInputSAEAPool = new SharedBufferSAEAPool(ServerSettings.PoolingCacheSize, this.udpCollection.secondaryRawBuffer, this.OnUDPIncomingDataComplete);
            this.udpOutSAEAPool = new SocketAsyncEventArgsPool(ServerSettings.PoolingCacheSize, this.OnUDPSendingDataComplete);
            this.udpIOMessagesPool = new MessagesPool(ServerSettings.PoolingCacheSize * 1000, new RawMessage(Message.CommandType.Null, null, 0));

            this.markedToDie = false;

            this.timer = new System.Diagnostics.Stopwatch();
            this.timer.Start();

#if PROFILING
            this.profilerOutgoingMessages = new Profiler("UDP_ReceivingMessages");
            this.profilerPacketizer = new Profiler("UDP_Packetizer");
#endif
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

        protected void ReceiveUDPDatagram()
        {
#if PROFILING

            this.profilerOutgoingMessages.Set();
#endif
            ///Checking if the reference is still running and the sockets are working.
            if (!this.aliveFlag)
                return;
            SocketAsyncEventArgs incomingData = this.udpInputSAEAPool.NextSlot;
            incomingData.AcceptSocket = this.udpCollection.socketReference;
            incomingData.RemoteEndPoint = this.udpCollection.remoteEndPoint;

            ///Setting the buffer offset and count, keep in mind that we are no assigning a new buffer, we are only setting working paremeters.
            incomingData.SetBuffer(0, (int)this.udpInputSAEAPool.BufferSize);
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
            catch (System.NullReferenceException)
            {
            }
        }

        protected void OnUDPIncomingDataComplete(object sender, SocketAsyncEventArgs e)
        {
#if PROFILING
            if (this.profilerOutgoingMessages != null)
            {
                this.profilerOutgoingMessages.Mark();
            }
#endif
            int readBytes = 0;
            if (!this.connected)
            {
                KSPMGlobals.Globals.Log.WriteTo("UDP Completed RECV-");
            }
            if (e.SocketError == SocketError.Success )
            {
                readBytes = e.BytesTransferred;
                if (readBytes > 0 && this.aliveFlag) 
                {
                    this.udpBuffer.Write(e.Buffer, (uint)readBytes);
                    ///Setting the sender of the datagram.
                    this.udpCollection.remoteEndPoint = e.RemoteEndPoint;
#if PROFILING
                    this.profilerPacketizer.Set();
#endif
                    if (!this.connected)
                    {
                        KSPMGlobals.Globals.Log.WriteTo("UDP RECV-" + readBytes.ToString());
                    }
                    //this.udpPacketizer.UDPPacketizeCRCMemoryAlloc(this);
                    this.udpPacketizer.UDPPacketizeCRCLoadIntoMessage(this, this.udpIOMessagesPool);
#if PROFILING
                    if (this.profilerPacketizer != null)
                    {
                        this.profilerPacketizer.Mark();
                    }
#endif
                    ///Either we have success reading the incoming data or not we need to recycle the SocketAsyncEventArgs used to perform this reading process.
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
                    KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}][\"{1}\"] Remote client disconnected 0 bytes received, performing a removing process on it.", this.id, "OnUDPIncomingDataComplete"));
                    ///Recycling the SAEA object before killing this reference.
                    ///Either we have success reading the incoming data or not we need to recycle the SocketAsyncEventArgs used to perform this reading process.
                    if (this.udpInputSAEAPool == null)///Means that the reference has been killed. So we have to release this SocketAsyncEventArgs by hand.
                    {
                        e.Dispose();
                        e = null;
                    }
                    else
                    {
                        this.udpInputSAEAPool.Recycle(e);
                    }
                    KSPMGlobals.Globals.KSPMServer.DisconnectClient(this);
                }
            }
            else
            {
                    KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}][\"{1}:{2}\"] Something went wrong with the remote client, performing a removing process on it.", this.id, "OnUDPIncomingDataComplete", e.SocketError));
                    ///Recycling the SAEA object before killing this reference.
                    ///Either we have success reading the incoming data or not we need to recycle the SocketAsyncEventArgs used to perform this reading process.
                    if (this.udpInputSAEAPool == null)///Means that the reference has been killed. So we have to release this SocketAsyncEventArgs by hand.
                    {
                        e.Dispose();
                        e = null;
                    }
                    else
                    {
                        this.udpInputSAEAPool.Recycle(e);
                    }
                    KSPMGlobals.Globals.KSPMServer.DisconnectClient(this);
            }
        }

        /// <summary>
        /// Not used at this moment.
        /// </summary>
        /// <param name="rawData"></param>
        /// <param name="fixedLegth"></param>
        public void ProcessUDPPacket(byte[] rawData, uint fixedLegth)
        {
            Message incomingMessage;
            KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo(fixedLegth.ToString());
            if (PacketHandler.InflateRawMessage(rawData, out incomingMessage) == Error.ErrorType.Ok)
            {
                ///Puting the incoming RawMessage into the queue to be processed.
                this.incomingPackets.EnqueueCommandMessage(ref incomingMessage);
                this.ProcessUDPCommand();
            }
        }

        /// <summary>
        /// Process the incoming Message. At this moment only adds it into the Queue.
        /// </summary>
        /// <param name="incomingMessage"></param>
        public void ProcessUDPMessage(Message incomingMessage)
        {
            this.incomingPackets.EnqueueCommandMessage(ref incomingMessage);
        }

        /// <summary>
        /// Asynchronous method to process each incoming UDP datagram.
        /// </summary>
        protected void ProcessUDPCommandAsyncMethod()
        {
            Message incomingMessage = null;
            Message responseMessage = null;
            RawMessage rawMessageReference = null;
            int intBuffer;

            KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}]-Starting to handle UDP commands [{1}].", this.id, this.aliveFlag));

            ///It will cycle until the Queue is not empty, in such case it will sleep 5 ms ans tries again.
            while (this.aliveFlag)
            {
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
                            ///Recycling the message used in the connection process.
                            this.udpIOMessagesPool.Recycle(incomingMessage);
                            /*
                            incomingMessage.Release();
                            incomingMessage = null;*/
                            break;
                        case Message.CommandType.UDPChat:
                            ///At this moment we only raises the event, but it can be raised with whichever incoming message.
                            KSPMGlobals.Globals.KSPMServer.OnUDPMessageArrived(this, rawMessageReference);
                            break;
                        default:
                            KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] {1} unknown command", this.id, incomingMessage.Command.ToString()));
                            break;
                    }
                    //this.SendUDPDatagram();
                }
                else
                {
                    Thread.Sleep(5);
                }
            }
        }

        /// <summary>
        /// Method fired when the asynchronous method ProcessUDP is completed. At this moment is stoped when this ServerSideClient reference is released.
        /// </summary>
        /// <param name="result"></param>
        protected void OnProcessUDPCommandComplete(System.IAsyncResult result)
        {
            ///Does nothing
        }

        #region DEPRECATED_CODE

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

        #endregion

        /// <summary>
        /// Sends a message as datagram, but the message is not queued at all.
        /// </summary>
        /// <param name="message"></param>
        public void SendAsDatagram(Message message)
        {
            Message outgoingMessage = null;
            SocketAsyncEventArgs outgoingData = null;
            /*
            if (!this.ableToRun)
            {
                KSPMGlobals.Globals.Log.WriteTo(Error.ErrorType.ServerClientUnableToRun.ToString());
                return;
            }
            */
            try
            {
                if (this.usingUdpConnection)
                {
                    ///Taking a message from the pool.
                    outgoingMessage = this.udpIOMessagesPool.BorrowMessage;
                    ///Loading the message with the proper content.
                    ((RawMessage)outgoingMessage).LoadWith(message.bodyMessage, 0, message.MessageBytesSize);

                    if (outgoingMessage != null && this.aliveFlag)
                    {
                        outgoingData = this.udpOutSAEAPool.NextSlot;
                        outgoingData.AcceptSocket = this.udpCollection.socketReference;
                        outgoingData.RemoteEndPoint = this.udpCollection.remoteEndPoint;
                        outgoingData.SetBuffer(outgoingMessage.bodyMessage, 0, (int)outgoingMessage.MessageBytesSize);
                        outgoingData.UserToken = outgoingMessage;
                        this.udpCollection.socketReference.SendToAsync(outgoingData);
                    }
                }
            }
            catch (SocketException ex)///Something happened to the remote client, so it is required to this ServerSideClient to kill itself.
            {
                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}][\"{1}-{2}:{3}\"] Something went wrong with the remote client, performing a removing process on it.", this.id, "SendAsDatagram", ex.SocketErrorCode, ex.Message));
                KSPMGlobals.Globals.KSPMServer.DisconnectClient(this);
            }
        }

        /// <summary>
        /// Sends a queued message as a datagram.
        /// </summary>
        public void SendUDPDatagram()
        {
            Message outgoingMessage = null;
            SocketAsyncEventArgs outgoingData = null;
            try
            {
                if (this.aliveFlag)///Is it still alive?.
                {
                    this.outgoingPackets.DequeueCommandMessage(out outgoingMessage);
                    if (outgoingMessage != null)
                    {
                        ///Already set up the UDP socket.
                        if (this.usingUdpConnection)
                        {
                            if (outgoingMessage.IsBroadcast)///Message sent through broadcasting methods.
                            {
                                if (this.connected)///Is already connected.
                                {
                                    outgoingData = this.udpOutSAEAPool.NextSlot;
                                    outgoingData.AcceptSocket = this.udpCollection.socketReference;
                                    outgoingData.RemoteEndPoint = this.udpCollection.remoteEndPoint;
                                    outgoingData.SetBuffer(outgoingMessage.bodyMessage, 0, (int)outgoingMessage.MessageBytesSize);
                                    outgoingData.UserToken = outgoingMessage;
                                    this.udpCollection.socketReference.SendToAsync(outgoingData);
                                }
                                else
                                {
                                    ///Recycling the message because it is not going to be sent.
                                    this.udpIOMessagesPool.Recycle(outgoingMessage);
                                }
                            }
                            else///So it is seting up connection message.
                            {
                                outgoingData = this.udpOutSAEAPool.NextSlot;
                                outgoingData.AcceptSocket = this.udpCollection.socketReference;
                                outgoingData.RemoteEndPoint = this.udpCollection.remoteEndPoint;
                                outgoingData.SetBuffer(outgoingMessage.bodyMessage, 0, (int)outgoingMessage.MessageBytesSize);
                                outgoingData.UserToken = outgoingMessage;
                                this.udpCollection.socketReference.SendToAsync(outgoingData);
                            }
                        }
                        else
                        {
                            ///Recycling the message because it is not going to be sent. NOT using UDP conn.
                            this.udpIOMessagesPool.Recycle(outgoingMessage);
                        }
                    }
                }
            }
            catch (SocketException ex)///Something happened to the remote client, so it is required to this ServerSideClient to kill itself.
            {
                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}][\"{1}-{2}:{3}\"] Something went wrong with the remote client, performing a removing process on it.", this.id, "SendUDPDatagram", ex.SocketErrorCode, ex.Message));
                KSPMGlobals.Globals.KSPMServer.DisconnectClient(this);
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
                KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}][\"{1}:{2}\"] Something went wrong with the remote client, performing a removing process on it.", this.id, "OnUDPSendingDataComplete", e.SocketError));
                ///Either we have have sucess sending the data, it's required to recycle the outgoing message.
                this.udpIOMessagesPool.Recycle((Message)e.UserToken);
                this.udpOutSAEAPool.Recycle(e);
                KSPMGlobals.Globals.KSPMServer.DisconnectClient(this);
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

                ///Creating the asynchronous call wich is going to handle the UDP command processing.
                ProcessUDPMessageAsync processUDPMessages = new ProcessUDPMessageAsync(this.ProcessUDPCommandAsyncMethod);
                processUDPMessages.BeginInvoke(this.OnProcessUDPCommandComplete, processUDPMessages);

                ///Creating the asynchronous call wich is going to handle the connection process.
                ConnectAsync connectionProcess = new ConnectAsync(this.HandleConnectionProcess);
                connectionProcess.BeginInvoke(this.AsyncConnectionProccesComplete, connectionProcess);

                ///Starting to receive TCP streams.
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
            Message disposingMessage = null;
            ///***********************Killing threads code
            this.aliveFlag = false;
            this.ableToRun = false;
            this.connected = false;
            this.markedToDie = true;///To avoid killing twice or more the same reference.

            ///***********************TCP Sockets code
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

            ///****************UDP sockets code.
            if (this.udpCollection.socketReference != null)
            {
                this.udpCollection.socketReference.Close();
            }
            this.udpCollection.Dispose();
            this.udpCollection = null;

            ///User release.
            if (this.gameUser != null)
            {
                this.gameUser.Release();
                this.gameUser = null;
            }

            ///Cleaning TCP buffers
            this.tcpBuffer.Release();
            this.tcpBuffer = null;
            this.packetizer.Release();
            this.packetizer = null;

            ///Cleaning TCP SAEAs
            this.tcpInEventsPool.Release(false);
            this.tcpInEventsPool = null;
            this.tcpOutEventsPool.Release(false);
            this.tcpOutEventsPool = null;

            ///Cleaning the UDP queues.
            this.incomingPackets.DequeueCommandMessage(out disposingMessage);
            while (disposingMessage != null)
            {
                this.udpIOMessagesPool.Recycle(disposingMessage);
                this.incomingPackets.DequeueCommandMessage(out disposingMessage);
            }
            this.outgoingPackets.DequeueCommandMessage(out disposingMessage);
            while (disposingMessage != null)
            {
                this.udpIOMessagesPool.Recycle(disposingMessage);
                this.outgoingPackets.DequeueCommandMessage(out disposingMessage);
            }
            this.incomingPackets.Purge(false);
            this.outgoingPackets.Purge(false);
            this.incomingPackets = null;
            this.outgoingPackets = null;

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

            KSPMGlobals.Globals.Log.WriteTo(string.Format("[{0}] ServerSide Client killed after {1} seconds alive.", this.id, this.AliveTime / 1000));
            
            this.timer.Reset();

#if PROFILING
            this.profilerPacketizer.Dispose();
            this.profilerPacketizer = null;
            this.profilerOutgoingMessages.Dispose();
            this.profilerOutgoingMessages = null;
#endif
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

        /// <summary>
        /// Gets the SocketAsyncEventArgsPool used to send TCP streams.
        /// </summary>
        public SocketAsyncEventArgsPool TCPOutSocketAsyncEventArgsPool
        {
            get
            {
                return this.tcpOutEventsPool;
            }
        }

        /// <summary>
        /// Gets the MessagesPool used to receive/send UDP datagrams.
        /// </summary>
        public MessagesPool IOUDPMessagesPool
        {
            get
            {
                return this.udpIOMessagesPool;
            }
        }

        #endregion

        #region UserManagement

        /// <summary>
        /// Registers the event which is going to be raised when a user is fully connected to the system.
        /// </summary>
        /// <param name="eventReference"></param>
        public void RegisterUserConnectedEvent(UserConnectedEventHandler eventReference)
        {
            if (eventReference == null)
            {
                return;
            }
            this.UserConnected = eventReference;
        }

        /// <summary>
        /// Raises the OnUserConnected event.
        /// </summary>
        /// <param name="e"></param>
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
