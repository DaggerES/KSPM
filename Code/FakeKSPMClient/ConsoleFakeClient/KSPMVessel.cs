
using System.Collections.Generic;
using System.IO;
using KSPM.Network.Common.Messages;

public class KSPMVessel
{
    /// <summary>
    /// Enumeration to defines those available states that a vessel can have.
    /// </summary>
    public enum Status : byte
    {
        /// <summary>
        /// Invalid vessel.
        /// </summary>
        Invalid = 0,

        /// <summary>
        /// Sending or receiving bytes.
        /// </summary>
        Sync_ing,

        /// <summary>
        /// The ship has been received and decompressed succesfully.
        /// </summary>
        ReadyToFligh,
    }

    #region Synchronization class

    public class SyncInformation
    {
        public short PacketCounter;
        public FileStream swapStream;
        protected string swapPath;

        public SyncInformation()
        {/*
            this.swapPath = string.Format("{0}.Ship-{1}.ns", KSPMUnityGlobals.WorkingDirectory, System.DateTime.Now.ToFileTimeUtc());
            this.swapStream = new FileStream(this.swapPath, FileMode.Create);
            this.PacketCounter = -1;*/
        }

        public SyncInformation(byte[] buffer, int offset, int size)
        {/*
            this.swapPath = string.Format("{0}.Ship-{1}.ns", KSPMUnityGlobals.WorkingDirectory, System.DateTime.Now.ToFileTimeUtc());
            this.swapStream = new FileStream(this.swapPath, FileMode.Create);
            this.PacketCounter = 0;
            this.AppendIncomingData(buffer, offset, size);*/
        }

        public void AppendIncomingData(byte[] buffer, int offset, int size)
        {
            this.swapStream.Write(buffer, offset, size);
            this.PacketCounter++;
        }

        public void Flush()
        {
            this.swapStream.Flush();
            this.swapStream.Close();
        }

        /*
        public FileStream SwapStream
        {
            get
            {
                return this.swapStream;
            }
        }
        */
    }

    #endregion

    /// <summary>
    /// Id generator to each vessel on the system that requires be synchronized among the players.
    /// </summary>
    protected static uint KSPMVesselIdGenerator;

    //public Vessel SyncVessel;

    //public ShipConstruct Ship;

    /// <summary>
    /// Status of the current reference.
    /// </summary>
    public Status CurrentStatus;

    /// <summary>
    /// Class reference to manage everyting regarding to send/receive a vessel.
    /// </summary>
    public SyncInformation Synchronizator;

    /// <summary>
    /// Unique network id.
    /// </summary>
    protected uint networkVesselId;

    // Use this for initialization
    void Start()
    {
        this.CurrentStatus = Status.Invalid;
        //this.SyncVessel = null;
    }

    public uint NetworkId
    {
        get
        {
            return this.networkVesselId;
        }
        set
        {
            this.networkVesselId = value;
        }
    }

    /*
    public static byte RequestNewNetworkVesselIdAction(object caller, Stack<object> parameters)
    {
        KSPMUnityGamePlayer player = (KSPMUnityGamePlayer)caller;
        bool aClientReferenceAsked = (bool)parameters.Pop();
        Message requestIdMessage = null;
        uint newId;
        int idWrapper;
        if (aClientReferenceAsked)
        {
            GameMessage.RequestVesselId(player.Parent, out requestIdMessage);
            KSPMUnityGlobals.SingletonReference.KSPMClientReference.kspmClientReference.OutgoingTCPQueue.EnqueueCommandMessage(ref requestIdMessage);
        }
        else
        {
            idWrapper = (int)KSPMVessel.KSPMVesselIdGenerator;
            newId = (uint)System.Threading.Interlocked.Increment(ref idWrapper);
            GameMessage.SendVesselId(player.Parent, newId, out requestIdMessage);
            //Debug.Log(requestIdMessage.ToString());
            ///Sending the requested id.
            KSPMUnityGlobals.SingletonReference.KSPMServerReference.KSPMGameServer.outgoingMessagesQueue.EnqueueCommandMessage(ref requestIdMessage);
            //KSPMUnityGlobals.SingletonReference.KSPMClientReference.kspmClientReference.OutgoingTCPQueue.EnqueueCommandMessage(ref requestIdMessage);
        }
        return 0;
    }

    /// <summary>
    /// Registers the Id sent by the server.
    /// Caller: KSPMUnityGamePlayer who has requested the id.
    /// The first parameter is the id itself.
    /// </summary>
    /// <param name="caller"></param>
    /// <param name="parameters"></param>
    /// <returns></returns>
    public static byte RegisterVesselIdAction(object caller, Stack<object> parameters)
    {
        KSPMUnityGamePlayer player = (KSPMUnityGamePlayer)caller;
        uint sentId = (uint)parameters.Pop();
        player.PlayingGame.RequestedVesselIds.Push(sentId);
        KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo(string.Format("Received new vessel ID from the server: {0}", sentId));
        return 0;
    }

    #region Sending Vessels

    /// <summary>
    /// Creates a new KSPMVessel instance and prepares everything to send it through the net.
    /// </summary>
    /// <param name="sourceVessel"></param>
    /// <returns></returns>
    public static KSPMVessel PrepareToSendVessel(ShipConstruct ship)
    {
        KSPMVessel outgoingVessel = new KSPMVessel();
        outgoingVessel.SyncVessel = null;
        outgoingVessel.Ship = ship;
        outgoingVessel.Synchronizator = new SyncInformation();
        outgoingVessel.networkVesselId = 0;
        outgoingVessel.CurrentStatus = Status.Invalid;
        return outgoingVessel;
    }

    /// <summary>
    /// Sends a ship through the system to anyone that requires it.
    /// </summary>
    /// <param name="caller"></param>
    /// <param name="parameters"></param>
    /// <returns></returns>
    public static byte SendShipAction(object caller, Stack<object> parameters)
    {
        EditorLogic logic = (EditorLogic)caller;
        string dirPath = (string)parameters.Pop();
        KSPMVessel playingVessel = (KSPMVessel)parameters.Pop();
        int bytesToSend;
        ///Setting the reference  of the just created vessel.
        KSPMUnityGlobals.SingletonReference.KSPMClientReference.UnityPlayer.PlayingVessel = playingVessel;
        if (KSPMUnityGlobals.SingletonReference.KSPMClientReference.UnityPlayer.PlayingGame.RequestedVesselIds.Count > 0)
        {
            KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo("Taking a network vessel id from the already existents.");
            playingVessel.networkVesselId = KSPMUnityGlobals.SingletonReference.KSPMClientReference.UnityPlayer.PlayingGame.RequestedVesselIds.Pop();
        }
        else
        {
            KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo("No ids in the networkIds stack");
            playingVessel.networkVesselId = 1;
        }
        //dirPath = ShipConstruction.SaveShip(logic.ship, "SyncShip");
        ///Opening the Auto-Saved ship craft file.
        FileStream fileToSend = new FileStream(dirPath, FileMode.Open);
        //FileStream compressedStream = new FileStream(dirPath + ".7z", FileMode.Create);

        ///Compressing the ship
        KSPMUnityGlobals.SingletonReference.SevenZipReference.Compress(fileToSend, playingVessel.Synchronizator.swapStream);
        bytesToSend = 1;
        playingVessel.Synchronizator.swapStream.Seek(0, SeekOrigin.Begin);
        while (bytesToSend > 0)
        {
            Message shipMessage = null;
            GameMessage.PackShip(KSPMUnityGlobals.SingletonReference.KSPMClientReference.kspmClientReference, -1, playingVessel, playingVessel.Synchronizator.swapStream, ref bytesToSend, out shipMessage);
            KSPMUnityGlobals.SingletonReference.KSPMClientReference.kspmClientReference.OutgoingTCPQueue.EnqueueCommandMessage(ref shipMessage);
        }
        fileToSend.Close();

        playingVessel.Synchronizator.Flush();

        KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo(string.Format("Ship sent from: {0}", dirPath));
        return 0;
    }

    #endregion
    #region Receiving Vessels

    public static KSPMVessel PrepareForIncomingVessel( uint id, byte[] buffer, int offset, int size)
    {
        KSPMVessel newVessel = new KSPMVessel();
        newVessel.Synchronizator = new SyncInformation(buffer, offset, size);
        newVessel.networkVesselId = id;
        newVessel.SyncVessel = null;
        newVessel.CurrentStatus = Status.Sync_ing;
        return newVessel;
    }

    /// <summary>
    /// Handles an incoming vessel wich was sent by a player, so it is compressed.
    /// Caller: KSPMUnityGamePlayer.
    /// 1st parameter: Incoming bytes.
    /// </summary>
    /// <param name="caller"></param>
    /// <param name="parameters"></param>
    /// <returns></returns>
    public static byte InflateVesselAction(object caller, Stack<object> parameters)
    {
        int vesselPart = -1;
        uint vesselId;
        int usableBytes;
        KSPMVessel incomingVessel = null;
        KSPMUnityGamePlayer player = (KSPMUnityGamePlayer)caller;
        byte[] buffer = (byte[])parameters.Pop();
        usableBytes = (int)parameters.Pop();
        vesselId = System.BitConverter.ToUInt32(buffer, GameMessage.UserDefinedMessageDataStartIndex);
        vesselPart = System.BitConverter.ToInt32(buffer, GameMessage.UserDefinedMessageDataStartIndex + 4);
        ///Checking if the current vessel is already registered on the system.
        if (player.PlayingGame.Vessels.ContainsKey(vesselId))
        {
            player.PlayingGame.Vessels.TryGetValue(vesselId, out incomingVessel);
            if (incomingVessel.CurrentStatus == Status.Sync_ing)
            {
                ///Means that the vessels is being received.
                incomingVessel.Synchronizator.AppendIncomingData(buffer, GameMessage.UserDefinedMessageDataStartIndex + 8, usableBytes - (GameMessage.UserDefinedMessageDataStartIndex + 8 + GameMessage.EndOfMessageCommand.Length));
                if (vesselPart < 0)
                {
                    incomingVessel.Synchronizator.Flush();
                }
            }
        }
        if (vesselPart == 1)
        {
            incomingVessel = KSPMVessel.PrepareForIncomingVessel( vesselId, buffer, GameMessage.UserDefinedMessageDataStartIndex + 8, usableBytes - (GameMessage.UserDefinedMessageDataStartIndex + 8 + GameMessage.EndOfMessageCommand.Length));
        }
        return 0;
    }

    #endregion
     * */
}
