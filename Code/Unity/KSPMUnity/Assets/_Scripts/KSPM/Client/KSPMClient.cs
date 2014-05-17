using UnityEngine;
using System.Collections;

using KSPM.Globals;
using KSPM.IO.Logging;
using KSPM.Game;
using KSPM.Network.Client;
using KSPM.Network.Client.RemoteServer;
using KSPM.Network.Common.Messages;

//[ExecuteInEditMode]
public class KSPMClient : MonoBehaviour
{
    public GameManager gameManager;
    public SceneManager sceneManager;

    protected GameClient kspmClient;
    protected ServerList hosts;
    protected string userName = "Username...";
    protected ServerInformation uiServerInformation;
    protected int serverInformationIndex;
    protected GameUser gameUserReference;
    protected int usersConnected;

    void Awake()
    {
        DontDestroyOnLoad(this);
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
	}

    void sceneManager_LoadingComplete(object sender, System.EventArgs e)
    {
        string loadeLevelName = (string)sender;
        if (loadeLevelName.Equals("Game", System.StringComparison.OrdinalIgnoreCase))
        {
            this.gameManager.StartGame();
        }
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
            this.kspmClient.InitializeClient();
        }
    }

    void kspmClient_TCPMessageArrived(object sender, KSPM.Network.Common.Messages.Message message)
    {
        GameMessage gameMessage = null;
        Message incomingMessage = null;
        GameMessage.LoadFromMessage(out incomingMessage, message);
        message.Dispose();
        gameMessage = (GameMessage)incomingMessage;
        switch (gameMessage.UserCommand)
        {
            case GameMessage.GameCommand.UserConnected:
                Debug.Log("User connected");
                this.usersConnected++;
                if (this.usersConnected == this.gameManager.RequiredUsers)
                {
                    Debug.Log("Ready to start");
                    Application.LoadLevel("Game");
                    //this.sceneManager.LoadLevel(SceneManager.Scenes.Game);
                }
                break;
            case GameMessage.GameCommand.UserDisconnected:
                break;
            default:
                break;
        }
        gameMessage.Release();
    }

    void kspmClient_UserDisconnected(object sender, KSPM.Network.Common.Events.KSPMEventArgs e)
    {
        Debug.Log(e.ToString());
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
