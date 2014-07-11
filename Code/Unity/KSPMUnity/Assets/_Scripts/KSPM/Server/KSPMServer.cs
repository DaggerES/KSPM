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
            if (this.StartServer())
            {
                this.gameManager.currentStatus = GameManager.GameStatus.NetworkSettingUp;
            }
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
        this.KSPMServerReference.TCPMessageArrived += new KSPM.Network.Common.Events.TCPMessageArrived(KSPMServerReference_TCPMessageArrived);
        
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

    void KSPMServerReference_TCPMessageArrived(object sender, Message message)
    {
        GameMessage gameMessage = null;
        Message incomingMessage = null;
        KSPMAction<object, object> action = null;
        GameMessage.LoadFromMessage(out incomingMessage, message);


        message.Dispose();
        gameMessage = (GameMessage)incomingMessage;
        Debug.Log(gameMessage.UserCommand);
        switch (gameMessage.UserCommand)
        {
            case GameMessage.GameCommand.GameStatus:
                switch ((GameManager.GameStatus)gameMessage.bodyMessage[14])
                {
                    case GameManager.GameStatus.ReadyToStart:
                        action = this.kspmManager.ActionsPool.BorrowAction;
                        action.ActionMethod.BasicAction = this.gameManager.PlayerManagerReference.SetPlayerReady;
                        action.ActionKind = KSPMAction<object, object>.ActionType.NormalMethod;
                        action.Completed += new KSPMAction<object, object>.ActionCompleted(ReadyFlagSet_Completed);
                        action.ParametersStack.Push(true);
                        action.ParametersStack.Push(sender);
                        this.kspmManager.ActionsToDo.Enqueue(action);
                        break;
                }
                break;
            default:
                break;
        }
        gameMessage.Release();
    }

    void ReadyFlagSet_Completed(object caller, System.Collections.Generic.Stack<object> parameters)
    {
        bool allPlayersReady;
        parameters.Pop();///Taking out the result of the previous method.
        allPlayersReady = (bool)parameters.Pop();
        if (allPlayersReady)
        {
            ///Setting the status to starting, so this is the signal to each client to start.
            this.gameManager.currentStatus = GameManager.GameStatus.Starting;
            Message userConnectedMessage = null;
            GameMessage.GameStatusMessage((NetworkEntity)caller, this.gameManager, out userConnectedMessage);
            this.KSPMServerReference.ClientsManager.TCPBroadcastTo(this.KSPMServerReference.ClientsManager.RemoteClients, userConnectedMessage);
        }
    }

    void KSPMServerReference_UDPMessageArrived(object sender, Message message)
    {
        ServerSideClient ssClientReference = (ServerSideClient) sender;
        ///Checking what kind of user defined command this message is.
        switch ((UDPGameMessage.UDPGameCommand)message.bodyMessage[13])
        {
            case UDPGameMessage.UDPGameCommand.ControlUpdate:
                //Debug.Log(message.bodyMessage[10]);
                KSPMAction<object, object> action = this.kspmManager.ActionsPool.BorrowAction;
                action.ActionKind = KSPMAction<object, object>.ActionType.NormalMethod;
                action.ActionMethod.BasicAction = HostControl.StaticMoveTargetAction;
                //action.ParametersStack.Push(z);
                //action.ParametersStack.Push(y);
                //action.ParametersStack.Push(x);
                action.ParametersStack.Push(message.bodyMessage[14]);
                action.ParametersStack.Push(sender);
                this.kspmManager.ActionsToDo.Enqueue(action);
                break;
        }
        ((ServerSideClient)sender).IOUDPMessagesPool.Recycle(message);
    }

    void kspmServer_UserDisconnected(object sender, KSPM.Network.Common.Events.KSPMEventArgs e)
    {
        ServerSideClient ssClientConnected = (ServerSideClient)sender;
        KSPMAction<object, object> action = this.kspmManager.ActionsPool.BorrowAction;
        action.ActionMethod.BasicAction = this.gameManager.PlayerManagerReference.StopPlayer;
        action.ActionKind = KSPMAction<object, object>.ActionType.NormalMethod;
        action.ParametersStack.Push(ssClientConnected.gameUser.UserDefinedHolder);
        action.Completed += new KSPMAction<object, object>.ActionCompleted(GamePlayerStoped);
        this.kspmManager.ActionsToDo.Enqueue(action);
    }

    /*********************************CHECK**************************/
    void GamePlayerStoped(object caller, System.Collections.Generic.Stack<object> parameters)
    {
        ServerSideClient ssClientConnected = (ServerSideClient)((GameObject)caller).GetComponent<GamePlayer>().Parent;
        Message userConnectedMessage = null;
        GameMessage.UserDisconnectedMessage(ssClientConnected, out userConnectedMessage);
        this.KSPMServerReference.ClientsManager.TCPBroadcastTo(this.KSPMServerReference.ClientsManager.RemoteClients, userConnectedMessage);
        Debug.Log(ssClientConnected.gameUser.Username + " se fue.");
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
        ///Taking out the result of the operation.
        parameters.Pop();
        MPGamePlayer createdPlayer = ((GameObject)parameters.Pop()).GetComponent<MPGamePlayer>();

        ///Send a game paramaters message to the connected user.
        Message gameParametersMessage = null;
        GameMessage.UserGameParametersMessage(ssClientConnected, createdPlayer, out gameParametersMessage);
        this.KSPMServerReference.priorityOutgoingMessagesQueue.EnqueueCommandMessage(ref gameParametersMessage);

        createdPlayer.tickController = new TickController(ssClientConnected.ClientLatency, ssClientConnected, this.TickTimerElapsed);
        Debug.Log(ssClientConnected.gameUser.Username);

        ///Creating the message of the connected user and broadcasting to everyone in the network.
        Message userConnectedMessage = null;
        GameMessage.UserConnectedMessage(ssClientConnected, createdPlayer, out userConnectedMessage);
        this.KSPMServerReference.ClientsManager.TCPBroadcastTo(this.KSPMServerReference.ClientsManager.RemoteClients, userConnectedMessage);

        if (this.KSPMServerReference.ClientsManager.ConnectedClients == this.gameManager.RequiredUsers)
        {
            
            Message startParametersMessage = null;
            GameMessage.GameStartParametersMessage(ssClientConnected, this.gameManager.PlayerManagerReference.Players, out startParametersMessage);
            this.KSPMServerReference.ClientsManager.TCPBroadcastTo(this.KSPMServerReference.ClientsManager.RemoteClients, startParametersMessage);

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
        }
    }

    protected void TickTimerElapsed(object state)
    {
        if (this.gameManager.currentStatus == GameManager.GameStatus.Playing)
        {
            Message updateMessage;
            ServerSideClient ssClientReference = (ServerSideClient)state;
            updateMessage = ssClientReference.IOUDPMessagesPool.BorrowMessage;
            UDPGameMessage.LoadUDPWorldUpdateMessage(ssClientReference, this.gameManager.WorldPositions, ref updateMessage);
            //UDPGameMessage.LoadUDPUpdateBallMessage(ssClientReference, this.gameManager.movementManager, this.gameManager.movementManager.targetPosition, ref updateMessage);
            ssClientReference.outgoingPackets.EnqueueCommandMessage(ref updateMessage);
            ssClientReference.SendUDPDatagram();
            //Debug.Log(this.gameManager.movementManager.targetPosition.x);
        }
    }

    public void SendBallForce()
    {
        Message updateMessage;
        ///Taking the  first RemoteClient reference to create the message.
        ServerSideClient ssClientReference = (ServerSideClient)this.KSPMServerReference.ClientsManager.RemoteClients[ 0 ];
        updateMessage = ssClientReference.IOUDPMessagesPool.BorrowMessage;
        UDPGameMessage.LoadUDPBallForceMessage( ssClientReference, this.gameManager.movementManager, ref updateMessage );
        this.KSPMServerReference.ClientsManager.UDPBroadcastClients(updateMessage);
        Debug.Log(updateMessage.ToString());
        ssClientReference.IOUDPMessagesPool.Recycle(updateMessage);
    }
}
