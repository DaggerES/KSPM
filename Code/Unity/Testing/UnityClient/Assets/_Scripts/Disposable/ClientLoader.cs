using UnityEngine;
using System.Collections;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using KSPM.Network.Client;
using KSPM.Network.Client.RemoteServer;
using KSPM.IO.Logging;
using KSPM.Diagnostics;
using KSPM.Globals;
using KSPM.Game;

public class ClientLoader : MonoBehaviour
{
    public string userName;

    byte[] utf8Bytes;
    GameClient client;
    GameUser myUser;
    ServerList hosts;
	// Use this for initialization
	void Start ()
    {
        Debug.Log("ASD");
        KSPMGlobals.Globals.InitiLogging(Log.LogginMode.File, false);
        UTF8Encoding utf8Encoder = new UTF8Encoding();
        utf8Bytes = utf8Encoder.GetBytes(userName);
        myUser = new GameUser(ref userName, ref utf8Bytes);
        ServerInformation server = new ServerInformation();
        hosts = null;
        ServerList.ReadServerList(out hosts);
        Debug.Log("ASD1");
        this.client = new GameClient();
        client.SetGameUser(myUser);
        client.SetServerHostInformation(hosts.Hosts[0]);
        Debug.Log("ASD");
        Debug.Log(client.InitializeClient().ToString());
        //client.Connect();
	}
	
	// Update is called once per frame
	void Update ()
    {
	}

    void OnGUI()
    {
        if (GUI.Button(new Rect(0, 0, 256, 128), "Connect"))
        {
            client.SetGameUser(myUser);
            client.SetServerHostInformation(hosts.Hosts[0]);
            Debug.Log(this.client.Connect().ToString());
        }

        if (GUI.Button(new Rect(512, 0, 256, 128), "Disconnect"))
        {
            this.client.Disconnect();
        }
    }
}
