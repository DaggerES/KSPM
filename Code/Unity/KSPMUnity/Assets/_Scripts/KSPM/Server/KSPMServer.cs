using UnityEngine;
using System.Collections;

using KSPM.Network.Common;
using KSPM.Network.Server;
using KSPM.Network.Common.Messages;
using KSPM.Globals;
using KSPM.IO.Logging;

[RequireComponent(typeof(KSPMManager))]
public class KSPMServer : MonoBehaviour
{
    protected ServerSettings settings;
    protected string text;
    protected KSPMManager kspmManager;

    public GameServer KSPMServerReference;
    public GameManager gameManager;
    public SceneManager sceneManager;


    void Awake()
    {
        DontDestroyOnLoad(this);
        this.kspmManager = this.GetComponent<KSPMManager>();
    }

	// Use this for initialization
	void Start ()
    {
        KSPMGlobals.Globals.ChangeIOFilePath(UnityGlobals.WorkingDirectory);
        KSPMGlobals.Globals.InitiLogging(Log.LogginMode.Buffered, false);
        this.KSPMServerReference = null;
	}

    void OnGUI()
    {
        GUI.TextArea(new Rect(400, 0, 640, 400), ((BufferedLog)KSPMGlobals.Globals.Log).Buffer);
        
        if (GUI.Button(new Rect(0, 0, 128, 32), "Start server"))
        {
            this.StartServer();
        }
        if (GUI.Button(new Rect(160, 0, 128, 32), "Stop server"))
        {
            this.StopServer();
        }
        
    }

    void OnApplicationQuit()
    {
        this.StopServer();
        KSPMGlobals.Globals.Log.Dispose();
    }

    public bool StartServer()
    {
        if (this.KSPMServerReference != null)
            return false;
        ServerSettings.ReadSettings(out settings);
        this.KSPMServerReference = new GameServer(ref settings);
        this.KSPMServerReference.UserConnected += new KSPM.Network.Common.Events.UserConnectedEventHandler(kspmServer_UserConnected);
        this.KSPMServerReference.UserDisconnected += new KSPM.Network.Common.Events.UserDisconnectedEventHandler(kspmServer_UserDisconnected);
        this.KSPMServerReference.UDPMessageArrived += new KSPM.Network.Common.Events.UDPMessageArrived(KSPMServerReference_UDPMessageArrived);
        KSPMGlobals.Globals.SetServerReference(ref KSPMServerReference);
        return this.KSPMServerReference.StartServer();
    }

    public void StopServer()
    {
        if (this.KSPMServerReference != null && this.KSPMServerReference.IsAlive)
        {
            this.KSPMServerReference.ShutdownServer();
            this.KSPMServerReference = null;
            Debug.Log("Server killed");
        }
    }

    void KSPMServerReference_UDPMessageArrived(object sender, Message message)
    {
        
    }

    void kspmServer_UserDisconnected(object sender, KSPM.Network.Common.Events.KSPMEventArgs e)
    {
        ServerSideClient ssClientConnected = (ServerSideClient)sender;
        Message userConnectedMessage = null;
        GameMessage.UserDisconnectedMessage(ssClientConnected, out userConnectedMessage);
        this.KSPMServerReference.ClientsManager.TCPBroadcastTo(this.KSPMServerReference.ClientsManager.RemoteClients, userConnectedMessage);
        //((GameObject)(ssClientConnected.gameUser.UserDefinedHolder) ).GetComponent<MPGamePlayer>().r
        ((ServerSideClientController)ssClientConnected.gameUser.UserDefinedHolder).Release();
        Debug.Log(ssClientConnected.gameUser.Username + " se fue." );
    }

    void kspmServer_UserConnected(object sender, KSPM.Network.Common.Events.KSPMEventArgs e)
    {
        KSPMAction<object, object> action = this.kspmManager.ActionsPool.BorrowAction;
        action.ActionMethod.BasicAction = this.gameManager.PlayerManagerReference.CreateEmptyPlayerAction;
        action.ActionKind = KSPMAction<object, object>.ActionType.NormalMethod;
        action.ParametersStack.Push(sender);
        action.Completed += new KSPMAction<object, object>.ActionCompleted(GamePlayerCreated);
        this.kspmManager.ActionsToDo.Enqueue(action);
    }

    void GamePlayerCreated(object caller, System.Collections.Generic.Stack<object> parameters)
    {
        ServerSideClient ssClientConnected = (ServerSideClient)caller;
        Debug.Log(ssClientConnected.gameUser.Username);

        ///Creating the message of the connected user and broadcasting to everyone in the network.
        Message userConnectedMessage = null;
        GameMessage.UserConnectedMessage(ssClientConnected, out userConnectedMessage);
        this.KSPMServerReference.ClientsManager.TCPBroadcastTo(this.KSPMServerReference.ClientsManager.RemoteClients, userConnectedMessage);

        if (this.KSPMServerReference.ClientsManager.ConnectedClients == this.gameManager.RequiredUsers)
        {
            this.sceneManager.LoadingComplete += new SceneManager.LoadingCompleteEventHandler(sceneManager_LoadingComplete);
            KSPMAction<object, object> action = this.kspmManager.ActionsPool.BorrowAction;
            action.ActionMethod.EnumeratedAction = this.sceneManager.LoadLevelAction;
            action.ActionKind = KSPMAction<object, object>.ActionType.EnumeratedMethod;
            action.ParametersStack.Push("Game");
            action.ParametersStack.Push(ssClientConnected);
            this.kspmManager.ActionsToDo.Enqueue(action);
        }
    }

    void sceneManager_LoadingComplete(object sender, GameEvenArgs e)
    {
        string loadedLevel = (string)e.EventParameter;
        if (loadedLevel.Equals("Game"))
        {
            this.gameManager.StartGame(UnityGlobals.WorkingMode.Server);
            Message userConnectedMessage = null;
            GameMessage.GameStatusMessage((NetworkEntity)sender, this.gameManager,out userConnectedMessage);
            this.KSPMServerReference.ClientsManager.TCPBroadcastTo(this.KSPMServerReference.ClientsManager.RemoteClients, userConnectedMessage);
        }
    }

    protected void TickTimerElapsed(object state)
    {
        /*
        Message updateMessage;
        ServerSideClient ssClientReference = (ServerSideClient)state;
        updateMessage = ssClientReference.IOUDPMessagesPool.BorrowMessage;
        UDPGameMessage.LoadUDPUpdateBallMessage(ssClientReference, this.gameManager.movementManager, this.gameManager.movementManager.targetPosition, ref updateMessage);
        ssClientReference.outgoingPackets.EnqueueCommandMessage(ref updateMessage);
        ssClientReference.SendUDPDatagram();
        */
    }

    public void SendBallForce()
    {
        Message updateMessage;
        ///Taking the  first RemoteClient reference to create the message.
        ServerSideClient ssClientReference = (ServerSideClient)this.KSPMServerReference.ClientsManager.RemoteClients[ 0 ];
        updateMessage = ssClientReference.IOUDPMessagesPool.BorrowMessage;
        UDPGameMessage.LoadUDPBallForceMessage( ssClientReference, this.gameManager.movementManager, ref updateMessage );
        this.KSPMServerReference.ClientsManager.UDPBroadcastClients(updateMessage);
        ssClientReference.IOUDPMessagesPool.Recycle(updateMessage);
    }
}
