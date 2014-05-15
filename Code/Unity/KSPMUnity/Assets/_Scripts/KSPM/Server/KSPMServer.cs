using UnityEngine;
using System.Collections;

using KSPM.Network.Server;
using KSPM.Network.Common.Messages;
using KSPM.Globals;
using KSPM.IO.Logging;

public class KSPMServer : MonoBehaviour
{
    protected ServerSettings settings;
    protected string text;

    public GameServer KSPMServerReference;

	// Use this for initialization
	void Start ()
    {
        //Debug.Log(System.Environment.CurrentDirectory);
        KSPMGlobals.Globals.ChangeIOFilePath(UnityGlobals.WorkingDirectory);
        KSPMGlobals.Globals.InitiLogging(Log.LogginMode.Buffered, false);
        this.KSPMServerReference = null;
	}
	
	// Update is called once per frame
	void Update ()
    {
	}

    void OnGUI()
    {
        GUI.TextArea(new Rect(400, 0, 640, 400), ((BufferedLog)KSPMGlobals.Globals.Log).Buffer);
        
        if (GUI.Button(new Rect(0, 0, 128, 32), "Start server"))
        {
            //text += " HOLA";
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
        KSPMGlobals.Globals.SetServerReference(ref KSPMServerReference);
        return this.KSPMServerReference.StartServer();
    }

    void kspmServer_UserDisconnected(object sender, KSPM.Network.Common.Events.KSPMEventArgs e)
    {
        ServerSideClient ssClientConnected = (ServerSideClient)sender;
        Message userConnectedMessage = null;
        GameMessage.UserDisconnectedMessage(ssClientConnected, out userConnectedMessage);
        this.KSPMServerReference.ClientsManager.TCPBroadcastTo(this.KSPMServerReference.ClientsManager.RemoteClients, userConnectedMessage);
        Debug.Log(ssClientConnected.gameUser.Username + " se fue." );
    }

    void kspmServer_UserConnected(object sender, KSPM.Network.Common.Events.KSPMEventArgs e)
    {
        ServerSideClient ssClientConnected = (ServerSideClient)sender;
        Message userConnectedMessage = null;
        GameMessage.UserConnectedMessage(ssClientConnected, out userConnectedMessage);
        this.KSPMServerReference.ClientsManager.TCPBroadcastTo(this.KSPMServerReference.ClientsManager.RemoteClients, userConnectedMessage);
        Debug.Log(ssClientConnected.gameUser.Username);
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
}
