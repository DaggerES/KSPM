using UnityEngine;
using System.Collections;

using KSPM.Network.Server;
using KSPM.Globals;
using KSPM.IO.Logging;

public class KSPMServer : MonoBehaviour
{
    protected ServerSettings settings;
    protected GameServer kspmServer;
    protected string text;
	// Use this for initialization
	void Start ()
    {
        //Debug.Log(System.Environment.CurrentDirectory);
        KSPMGlobals.Globals.ChangeIOFilePath(string.Format("{0}/{1}/", UnityGlobals.IOSwapPath, UnityGlobals.SwapFolder));
        KSPMGlobals.Globals.InitiLogging(Log.LogginMode.Buffered, false);
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

    protected void StartServer()
    {
        if (this.kspmServer != null)
            return;
        ServerSettings.ReadSettings(out settings);
        this.kspmServer = new GameServer(ref settings);
        this.kspmServer.UserConnected += new KSPM.Network.Common.Events.UserConnectedEventHandler(kspmServer_UserConnected);
        this.kspmServer.UserDisconnected += new KSPM.Network.Common.Events.UserDisconnectedEventHandler(kspmServer_UserDisconnected);
        KSPMGlobals.Globals.SetServerReference(ref this.kspmServer);
        this.kspmServer.StartServer();
    }

    void kspmServer_UserDisconnected(object sender, KSPM.Network.Common.Events.KSPMEventArgs e)
    {
        ServerSideClient reference = (ServerSideClient)sender;
        Debug.Log(reference.gameUser.Username + " se fue." );
    }

    void kspmServer_UserConnected(object sender, KSPM.Network.Common.Events.KSPMEventArgs e)
    {
        ServerSideClient reference = (ServerSideClient)sender;
        Debug.Log(reference.gameUser.Username);
    }

    protected void StopServer()
    {
        if (this.kspmServer != null && this.kspmServer.IsAlive)
        {
            this.kspmServer.ShutdownServer();
        }
    }
}
