using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KSPM.Network.Server;
using KSPM.IO.Logging;
using KSPM.Diagnostics;
using KSPM.Globals;
using KSPM.Network.Client.RemoteServer;
using KSPM.Network.Client;
using KSPM.Game;
using System.Xml.Serialization;
using System.Xml;
using System.IO;

using System.Threading;

namespace KSPM_TestingConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            KSPMGlobals.Globals.InitiLogging(Log.LogginMode.Console, false);

			////Server test
			
            ServerSettings gameSettings = null;
            if (ServerSettings.ReadSettings(out gameSettings) == KSPM.Network.Common.Error.ErrorType.Ok)
            {
                GameServer server = new GameServer(ref gameSettings);
                KSPMGlobals.Globals.SetServerReference(ref server);
                server.StartServer();
                Console.ReadLine();
                server.ShutdownServer();
            }
            Console.ReadLine();
			
            /*
			string userName = "Scr_Ra(s0_o)";
			byte[] utf8Bytes;
			UTF8Encoding utf8Encoder = new UTF8Encoding();
			utf8Bytes = utf8Encoder.GetBytes(userName);
			GameUser myUser = new GameUser(ref userName, ref utf8Bytes);
			ServerInformation server = new ServerInformation();
			ServerList hosts = null;
			ServerList.ReadServerList(out hosts);

			OperatingSystem os = Environment.OSVersion;
			//server.ip = "189.210.119.226";
			/*
			server.ip = "192.168.15.114";
            server.port = 4700;
			server.name = "Testeando";
			hosts.Hosts.Add(server);
			*/
            /*
			KSPM.Network.Common.Error.ErrorType a =  ServerList.WriteServerList(ref hosts);
			GameClient client = new GameClient();
			ConsoleKeyInfo pressedKey;
			bool exit = false;
			//client.SetGameUser(myUser);
			//client.SetServerHostInformation(server);
			client.InitializeClient();

			client.SetGameUser (myUser);
			client.SetServerHostInformation (hosts.Hosts [2]);
			client.Connect ();
			Thread.Sleep (10000);
			client.Disconnect();
			Console.ReadLine();
			client.Release();
            */
        }

    }
}
