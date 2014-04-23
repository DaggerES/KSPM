using UnityEngine;
using System.Collections;

using KSPM.Globals;
using KSPM.IO.Logging;
using KSPM.Network.Client;
using KSPM.Network.Client.RemoteServer;

//[ExecuteInEditMode]
public class KSPMClient : MonoBehaviour
{
    protected GameClient kspmClient;
    protected ServerList hosts;
    protected string userName = "Username...";
    protected ServerInformation currentServer;
	// Use this for initialization
	void Start ()
    {
        KSPMGlobals.Globals.ChangeIOFilePath(string.Format("{0}/{1}/", UnityGlobals.IOSwapPath, UnityGlobals.SwapFolder));
        KSPMGlobals.Globals.InitiLogging(Log.LogginMode.Buffered, false);
        KSPMGlobals.Globals.Log.WriteTo(KSPMGlobals.Globals.IOFilePath);
        //this.StartClient();
        this.currentServer = new ServerInformation();
        ServerList.ReadServerList(out hosts);
        this.currentServer = hosts.Hosts[0];
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    void OnApplicationQuit()
    {
        //this.StopClient();
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
        }
        if (GUI.Button(new Rect(128, 20, 96, 32), "Disconnect"))
        {
            this.kspmClient.Disconnect();
        }
        GUI.EndGroup();

        GUI.BeginGroup(new Rect(400, 480, 256, 256), "Servers");
        this.currentServer.name = GUI.TextField(new Rect(10, 20, 128, 20), this.currentServer.name);
        this.currentServer.ip = GUI.TextField(new Rect(10, 40, 128, 20), this.currentServer.ip);
        this.currentServer.port = int.Parse(GUI.TextField(new Rect(10, 60, 128, 20), this.currentServer.port.ToString()));
        if (GUI.Button(new Rect(148, 20, 32, 20), "+"))
        {
            this.hosts.AddHost(currentServer);
            ServerList.WriteServerList(ref this.hosts);
        }
        GUI.EndGroup();
    }

    protected void StartClient()
    {
        if (this.kspmClient == null)
        {
            this.kspmClient = new GameClient();
            this.kspmClient.InitializeClient();
        }
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
