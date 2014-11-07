using UnityEngine;
using System.Collections;

using KSPM.Globals;
using KSPM.IO.Logging;
using KSPM.Game;
using KSPM.Network.Client;
using KSPM.Network.Client.RemoteServer;
using KSPM.Network.Common.Messages;

//[ExecuteInEditMode]
[RequireComponent(typeof(KSPMManager))]
public class KSPMClient : MonoBehaviour
{
    public GameManager gameManager;
    public SceneManager sceneManager;
    protected KSPMManager kspmManager;

    protected GameClient kspmClient;
    protected ServerList hosts;
    protected string userName = "Chuchito";
    protected ServerInformation uiServerInformation;
    protected int serverInformationIndex;
    protected GameUser gameUserReference;
    protected int usersConnected;
    protected PlayerManager.GameRol connectedUserGamingRol = PlayerManager.GameRol.Spectator;
    protected int connectedUserGameId = -1;

    protected bool ableToPlay;

    void Awake()
    {
        DontDestroyOnLoad(this);
        this.kspmManager = this.GetComponent<KSPMManager>();
        this.ableToPlay = false;
    }

	// Use this for initialization
	void Start ()
    {
        KSPMGlobals.Globals.ChangeIOFilePath(UnityGlobals.WorkingDirectory);
        KSPMGlobals.Globals.InitiLogging(Log.LogginMode.Buffered, false);
        this.uiServerInformation = new ServerInformation();
        ServerList.ReadServerList(out hosts);
        hosts.Hosts[0].Clone(ref this.uiServerInformation);
        this.serverInformationIndex = 0;
        this.usersConnected = 0;
        this.sceneManager.LoadingComplete += new SceneManager.LoadingCompleteEventHandler(sceneManager_LoadingComplete);
        //this.StartClient();
        UnityGlobals.SingletonReference.KSPMClientReference = this;
	}

    void sceneManager_LoadingComplete(object sender, GameEvenArgs e)
    {
        string loadeLevelName = (string)e.EventParameter;
        if (loadeLevelName.Equals("Game", System.StringComparison.OrdinalIgnoreCase))
        {
            KSPMAction<object, object> action = this.kspmManager.ActionsPool.BorrowAction;
            action.Completed += new KSPMAction<object, object>.ActionCompleted(GameStartAction_Completed);
            action.ActionKind = KSPMAction<object, object>.ActionType.NormalMethod;
            action.ActionMethod.BasicAction = this.gameManager.StartGameAction;
            action.ParametersStack.Push(this.kspmClient);
            this.kspmManager.ActionsToDo.Enqueue(action);
        }
    }

    void GameStartAction_Completed(object caller, System.Collections.Generic.Stack<object> parameters)
    {
        Message readytoStartMessage = null;
        GameClient client = (GameClient)caller;
        this.gameManager.currentStatus = GameManager.GameStatus.ReadyToStart;
        GameMessage.GameStatusMessage(client, this.gameManager, out readytoStartMessage);
        this.kspmClient.OutgoingTCPQueue.EnqueueCommandMessage(ref readytoStartMessage);
        //this.gameManager.currentStatus = GameManager.GameStatus.Playing;
    }
	
	// Update is called once per frame
	void Update () {
	
	}

    void OnApplicationQuit()
    {
        this.StopClient();
        KSPMGlobals.Globals.Log.Dispose();
    }

    int sel = -1;

    void OnGUI()
    {
        if (this.hosts != null)
        {
            GUILayout.BeginVertical();

            GUILayout.BeginScrollView(new Vector2(500,100), GUILayout.Width(250f), GUILayout.Height(300));

            for (int i = 0; i < this.hosts.Hosts.Count; i++)
            {
                //GUILayout.Button(this.hosts.Hosts[i].name);
                if (GUILayout.Toggle(sel == i, this.hosts.Hosts[i].name, GUILayout.Height(64f)))
                {
                    if (sel != i)
                    {
                        sel = i;
                        Debug.Log(sel);
                    }
                }
                Rect lastRec = GUILayoutUtility.GetLastRect();
                GUI.Label(lastRec, this.hosts.Hosts[i].ip);
            }
            GUILayout.EndScrollView();

            GUILayout.EndVertical();
        }
        /*
        GUI.TextArea(new Rect(400, 0, 768, 400), ((BufferedLog)KSPMGlobals.Globals.Log).Buffer);

        GUI.BeginGroup(new Rect(50, 0, 256, 480), "User account");
        userName = GUI.TextField(new Rect(10, 20, 128, 32), userName, 16);
        GUI.EndGroup();

        GUI.BeginGroup(new Rect(50, 480, 256, 64), "Connection");
        if (GUI.Button(new Rect(10, 20, 96, 32), "Connect"))
        {
            byte[] utf8Bytes;
            System.Text.UTF8Encoding utf8Encoder = new System.Text.UTF8Encoding();
            utf8Bytes = utf8Encoder.GetBytes(userName);
            this.gameUserReference = new GameUser(ref this.userName, ref utf8Bytes);
            this.kspmClient.SetGameUser(this.gameUserReference);
            this.kspmClient.SetServerHostInformation(this.uiServerInformation);
            this.gameManager.currentStatus = GameManager.GameStatus.NetworkSettingUp;
            if (this.kspmClient.Connect() == KSPM.Network.Common.Error.ErrorType.Ok)
            {
                Debug.Log("Connected");
            }
        }
        if (GUI.Button(new Rect(128, 20, 96, 32), "Disconnect"))
        {
            this.kspmClient.Disconnect();
        }
        GUI.EndGroup();

        GUI.BeginGroup(new Rect(400, 480, 256, 256), "Servers");
        this.uiServerInformation.name = GUI.TextField(new Rect(10, 20, 128, 20), this.uiServerInformation.name);
        this.uiServerInformation.ip = GUI.TextField(new Rect(10, 40, 128, 20), this.uiServerInformation.ip);
        this.uiServerInformation.port = int.Parse(GUI.TextField(new Rect(10, 60, 128, 20), this.uiServerInformation.port.ToString()));
        if (GUI.Button(new Rect(148, 20, 32, 20), "+"))
        {
            this.hosts.AddHost(uiServerInformation);
            ServerList.WriteServerList(ref this.hosts);
        }
        if (GUI.Button(new Rect(148, 80, 32, 20), "->>"))
        {
            this.serverInformationIndex = (this.serverInformationIndex + 1) % this.hosts.Hosts.Count;
            this.hosts.Hosts[serverInformationIndex].Clone(ref this.uiServerInformation);
        }
        GUI.EndGroup();

        GUI.BeginGroup(new Rect(700, 480, 256, 256), "Game");
        if (this.ableToPlay)
        {
            if (GUI.Button(new Rect(10, 20, 96, 30), "Start"))
            {
            }
        }
        GUI.EndGroup();
        */
    }

    protected void StartClient()
    {
        if (this.kspmClient == null)
        {
            this.kspmClient = new GameClient();
            this.kspmClient.UserDisconnected += new KSPM.Network.Common.Events.UserDisconnectedEventHandler(kspmClient_UserDisconnected);
            this.kspmClient.TCPMessageArrived += new KSPM.Network.Common.Events.TCPMessageArrived(kspmClient_TCPMessageArrived);
            this.kspmClient.UDPMessageArrived += new KSPM.Network.Common.Events.UDPMessageArrived(kspmClient_UDPMessageArrived);
            this.kspmClient.InitializeClient();
            this.gameManager.currentStatus = GameManager.GameStatus.NetworkSettingUp;
        }
    }

    void kspmClient_UDPMessageArrived(object sender, Message message)
    {
        float x,y,z;
        int itemsCount;
        KSPMAction<object, object> action;
        switch ((UDPGameMessage.UDPGameCommand)message.bodyMessage[13])
        {
            case UDPGameMessage.UDPGameCommand.BallUpdate:
                if (this.gameManager.currentStatus == GameManager.GameStatus.Playing)
                {
                    x = System.BitConverter.ToSingle(message.bodyMessage, 14);
                    y = System.BitConverter.ToSingle(message.bodyMessage, 18);
                    z = System.BitConverter.ToSingle(message.bodyMessage, 22);
                    action = this.kspmManager.ActionsPool.BorrowAction;
                    action.ActionKind = KSPMAction<object, object>.ActionType.NormalMethod;
                    action.ActionMethod.BasicAction = this.gameManager.movementManager.UpdateTargetPositionAction;
                    action.ParametersStack.Push(x);
                    action.ParametersStack.Push(y);
                    action.ParametersStack.Push(z);
                    action.ParametersStack.Push(this);
                    this.kspmManager.ActionsToDo.Enqueue(action);
                    Debug.Log(x);
                }
                break;
            case UDPGameMessage.UDPGameCommand.WorldPositionsUpdate:
                action = this.kspmManager.ActionsPool.BorrowAction;
                action.ActionKind = KSPMAction<object, object>.ActionType.NormalMethod;
                action.ActionMethod.BasicAction = this.gameManager.WorldUpdateAction;
                itemsCount = System.BitConverter.ToInt32(message.bodyMessage, 14);
                for (int i = 0; i < itemsCount; i++)
                {
                    x = System.BitConverter.ToSingle(message.bodyMessage, 18 + i * 12);
                    y = System.BitConverter.ToSingle(message.bodyMessage, 22 + i * 12);
                    z = System.BitConverter.ToSingle(message.bodyMessage, 26 + i * 12);
                    action.ParametersStack.Push(z);
                    action.ParametersStack.Push(y);
                    action.ParametersStack.Push(x);
                }
                action.ParametersStack.Push(itemsCount);
                action.ParametersStack.Push(sender);
                this.kspmManager.ActionsToDo.Enqueue(action);
                break;
            case UDPGameMessage.UDPGameCommand.BallForce:
                x = System.BitConverter.ToSingle(message.bodyMessage, 14);
                y = System.BitConverter.ToSingle(message.bodyMessage, 18);
                z = System.BitConverter.ToSingle(message.bodyMessage, 22);
                Debug.Log(x);
                this.gameManager.movementManager.ApplyForce( x, y, z);
                break;
        }
        ((GameClient)sender).UDPIOMessagesPool.Recycle(message);
    }

    void kspmClient_TCPMessageArrived(object sender, KSPM.Network.Common.Messages.Message message)
    {
        GameMessage gameMessage = null;
        Message incomingMessage = null;
        GameMessage.LoadFromMessage(out incomingMessage, message);
        int intBuffer = -1;
        int userId;
        byte gamingRol;
        KSPMAction<object, object> action;
        message.Dispose();
        gameMessage = (GameMessage)incomingMessage;
        Debug.Log(gameMessage.UserCommand);
        switch (gameMessage.UserCommand)
        {
            case GameMessage.GameCommand.UserGameParameters:
                this.connectedUserGameId = System.BitConverter.ToInt32(gameMessage.bodyMessage, 14);
                this.connectedUserGamingRol = (PlayerManager.GameRol)gameMessage.bodyMessage[18];
                break;
            case GameMessage.GameCommand.GameParameters:
                action = this.kspmManager.ActionsPool.BorrowAction;
                action.ActionKind = KSPMAction<object, object>.ActionType.NormalMethod;
                action.ActionMethod.BasicAction = this.gameManager.PlayerManagerReference.CheckGamePlayersSummaryAction;
                action.Completed += new KSPMAction<object, object>.ActionCompleted(PlayersSummaryCheck_Completed);
                
                intBuffer = System.BitConverter.ToInt32(gameMessage.bodyMessage, 14);
                for (int i = 0; i < intBuffer; i++)
                {
                    userId = System.BitConverter.ToInt32(gameMessage.bodyMessage, 18 + i * 5);
                    gamingRol = gameMessage.bodyMessage[22 + i * 5];
                    action.ParametersStack.Push(gamingRol);
                    action.ParametersStack.Push(userId);
                    Debug.Log(userId + ":" + gamingRol);
                }
                action.ParametersStack.Push(intBuffer);
                action.ParametersStack.Push(this.kspmClient);
                this.kspmManager.ActionsToDo.Enqueue(action);

                break;
            case GameMessage.GameCommand.UserConnected:
                this.usersConnected++;
                intBuffer = System.BitConverter.ToInt32(gameMessage.bodyMessage, 14);
                gamingRol = gameMessage.bodyMessage[18];
                action = this.kspmManager.ActionsPool.BorrowAction;
                action.ActionKind = KSPMAction<object, object>.ActionType.NormalMethod;
                action.ActionMethod.BasicAction = this.gameManager.PlayerManagerReference.CreateEmptyLocalPlayerAction;
                action.Completed += new KSPMAction<object, object>.ActionCompleted(LocalGamePlayerCreated_Completed);
                action.ParametersStack.Push(gamingRol);
                action.ParametersStack.Push(intBuffer);
                action.ParametersStack.Push(this.connectedUserGameId);
                action.ParametersStack.Push(this.connectedUserGamingRol);
                action.ParametersStack.Push(this.kspmClient);
                this.kspmManager.ActionsToDo.Enqueue(action);
                break;
            case GameMessage.GameCommand.UserDisconnected:
                intBuffer = System.BitConverter.ToInt32(gameMessage.bodyMessage, 14);
                action = this.kspmManager.ActionsPool.BorrowAction;
                action.ActionMethod.BasicAction = this.gameManager.StopPlayer;
                action.ActionKind = KSPMAction<object, object>.ActionType.NormalMethod;
                action.ParametersStack.Push( ((GameClient)sender).ClientOwner.UserDefinedHolder);
                action.Completed += new KSPMAction<object, object>.ActionCompleted(GamePlayerStoped);
                this.kspmManager.ActionsToDo.Enqueue(action);
                Debug.Log("Disconnected");
                this.usersConnected--;
                break;
            case GameMessage.GameCommand.GameTerminate:
                Debug.Log("ADIOS");
                break;
            case GameMessage.GameCommand.GameStatus:
                switch ((GameManager.GameStatus)gameMessage.bodyMessage[14])
                {
                    case GameManager.GameStatus.Starting:
                        action = this.kspmManager.ActionsPool.BorrowAction;
                        action.ActionKind = KSPMAction<object, object>.ActionType.NormalMethod;
                        action.ActionMethod.BasicAction = this.gameManager.SetPlayersEnableValueAction;
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

    void PlayersSummaryCheck_Completed(object caller, System.Collections.Generic.Stack<object> parameters)
    {
        ///Ready to load the game scene.
        this.gameManager.currentStatus = GameManager.GameStatus.Starting;
        KSPMAction<object, object> action = this.kspmManager.ActionsPool.BorrowAction;
        action.ActionKind = KSPMAction<object, object>.ActionType.EnumeratedMethod;
        action.ActionMethod.EnumeratedAction = this.sceneManager.LoadLevelAction;
        action.ParametersStack.Push("Game");
        action.ParametersStack.Push(this);
        this.kspmManager.ActionsToDo.Enqueue(action);
    }

    void LocalGamePlayerCreated_Completed(object caller, System.Collections.Generic.Stack<object> parameters)
    {
        /*
        ///Checking if there are enough players to start the game.
        if (this.usersConnected == this.gameManager.RequiredUsers)
        {
            this.ableToPlay = true;
            this.gameManager.currentStatus = GameManager.GameStatus.Starting;
            KSPMAction<object, object> action = this.kspmManager.ActionsPool.BorrowAction;
            action.ActionKind = KSPMAction<object, object>.ActionType.EnumeratedMethod;
            action.ActionMethod.EnumeratedAction = this.sceneManager.LoadLevelAction;
            action.ParametersStack.Push("Game");
            action.ParametersStack.Push(this);
            this.kspmManager.ActionsToDo.Enqueue(action);
        }
        */
    }

    /***/
    void GamePlayerStoped(object caller, System.Collections.Generic.Stack<object> parameters)
    {
        KSPM.Network.Common.Error.ErrorType returnedError = (KSPM.Network.Common.Error.ErrorType)parameters.Pop();///Taking out the result of the previous calling.
        this.gameUserReference.Release();
        this.gameUserReference = null;
        if (returnedError == KSPM.Network.Common.Error.ErrorType.Ok)
        {
            int disconnectedPlayerId = (int)parameters.Pop();
            Debug.Log("Player with id[" + disconnectedPlayerId + "] se fue.");
        }
    }

    void kspmClient_UserDisconnected(object sender, KSPM.Network.Common.Events.KSPMEventArgs e)
    {   
        KSPMAction<object, object> action;
        action = this.kspmManager.ActionsPool.BorrowAction;
        action.ActionMethod.BasicAction = this.gameManager.StopPlayer;
        action.ActionKind = KSPMAction<object, object>.ActionType.NormalMethod;
        action.ParametersStack.Push(this.gameUserReference.UserDefinedHolder);
        action.Completed += new KSPMAction<object, object>.ActionCompleted(GamePlayerStoped);
        this.kspmManager.ActionsToDo.Enqueue(action);
        Debug.Log(e.ToString());
    }

    public void SendControlsUpdate(HostControl.MovementAction update)
    {
        Message updateMessage = this.kspmClient.UDPIOMessagesPool.BorrowMessage;
        UDPGameMessage.LoadUDPControlUpdateMessage(this.kspmClient, update, ref updateMessage);
        this.kspmClient.OutgoingUDPQueue.EnqueueCommandMessage(ref updateMessage);
    }

    protected void StopClient()
    {
        if ( this.kspmClient != null && this.kspmClient.AliveTime > 0)
        {
            this.kspmClient.Disconnect();
            this.kspmClient.Release();
        }
    }
}
