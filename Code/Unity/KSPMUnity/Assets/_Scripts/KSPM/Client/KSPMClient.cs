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
        this.uiServerInformation = new ServerInformation();
        ServerList.ReadServerList(out hosts);
        hosts.Hosts[0].Clone(ref this.uiServerInformation);
        this.serverInformationIndex = 0;
        this.usersConnected = 0;
        this.sceneManager.LoadingComplete += new SceneManager.LoadingCompleteEventHandler(sceneManager_LoadingComplete);
        this.StartClient();
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
            action.ParametersStack.Push(this);
            this.kspmManager.ActionsToDo.Enqueue(action);
        }
    }

    void GameStartAction_Completed(object caller, System.Collections.Generic.Stack<object> parameters)
    {
    }
	
	// Update is called once per frame
	void Update () {
	
	}

    void OnApplicationQuit()
    {
        this.StopClient();
        KSPMGlobals.Globals.Log.Dispose();
    }

    void OnGUI()
    {
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
        }
    }

    void kspmClient_UDPMessageArrived(object sender, Message message)
    {
        float x,y,z;
        switch ((UDPGameMessage.UDPGameCommand)message.bodyMessage[13])
        {
            case UDPGameMessage.UDPGameCommand.BallUpdate:
                KSPMAction<object, object> action = this.kspmManager.ActionsPool.BorrowAction;
                action.ActionKind = KSPMAction<object, object>.ActionType.NormalMethod;
                action.ActionMethod.BasicAction = this.gameManager.movementManager.UpdateTargerPositionAction;
                action.ParametersStack.Push(System.BitConverter.ToSingle( message.bodyMessage, 10 ));
                action.ParametersStack.Push(System.BitConverter.ToSingle(message.bodyMessage, 14));
                action.ParametersStack.Push(System.BitConverter.ToSingle(message.bodyMessage, 18));
                action.ParametersStack.Push(this);
                this.kspmManager.ActionsToDo.Enqueue(action);
                break;
            case UDPGameMessage.UDPGameCommand.BallForce:
                x = System.BitConverter.ToSingle(message.bodyMessage, 10);
                y = System.BitConverter.ToSingle(message.bodyMessage, 14);
                z = System.BitConverter.ToSingle(message.bodyMessage, 18);
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
        message.Dispose();
        gameMessage = (GameMessage)incomingMessage;
        Debug.Log(gameMessage.UserCommand);
        switch (gameMessage.UserCommand)
        {
            case GameMessage.GameCommand.UserConnected:
                this.usersConnected++;
                KSPMAction<object, object> action = this.kspmManager.ActionsPool.BorrowAction;
                action.ActionKind = KSPMAction<object, object>.ActionType.NormalMethod;
                action.ActionMethod.BasicAction = this.gameManager.PlayerManagerReference.CreateEmptyLocalPlayerAction;
                action.Completed += new KSPMAction<object, object>.ActionCompleted(LocalGamePlayerCreated_Completed);
                action.ParametersStack.Push(this.kspmClient);
                this.kspmManager.ActionsToDo.Enqueue(action);
                break;
            case GameMessage.GameCommand.UserDisconnected:
                this.usersConnected--;
                break;
            case GameMessage.GameCommand.GameStatus:
                /*
                switch ((GameManager.GameStatus)gameMessage.bodyMessage[10])
                {
                    case GameManager.GameStatus.Waiting:
                        break;
                }
                */
                break;
            default:
                break;
        }
        gameMessage.Release();
    }

    void LocalGamePlayerCreated_Completed(object caller, System.Collections.Generic.Stack<object> parameters)
    {
        ///Checking if there are enough players to start the game.
        if (this.usersConnected == this.gameManager.RequiredUsers)
        {
            KSPMAction<object, object> action = this.kspmManager.ActionsPool.BorrowAction;
            action.ActionKind = KSPMAction<object, object>.ActionType.EnumeratedMethod;
            action.ActionMethod.EnumeratedAction = this.sceneManager.LoadLevelAction;
            action.ParametersStack.Push("Game");
            action.ParametersStack.Push(this);
            this.kspmManager.ActionsToDo.Enqueue(action);
        }
    }

    void kspmClient_UserDisconnected(object sender, KSPM.Network.Common.Events.KSPMEventArgs e)
    {
        Debug.Log(e.ToString());
    }

    /// <summary>
    /// Deprecated.
    /// </summary>
    /// <param name="displacement"></param>
    public void SendControlsUpdate(UnityEngine.Vector3 displacement)
    {
        Message updateMessage = this.kspmClient.UDPIOMessagesPool.BorrowMessage;
        UDPGameMessage.LoadUDPControlUpdateMessage(this.kspmClient, displacement, ref updateMessage);
        this.kspmClient.OutgoingUDPQueue.EnqueueCommandMessage(ref updateMessage);
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
