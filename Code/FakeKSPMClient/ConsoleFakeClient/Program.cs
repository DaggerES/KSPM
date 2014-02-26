using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using KSPM.Network.Client;
using KSPM.IO.Logging;
using KSPM.Diagnostics;
using KSPM.Globals;
using KSPM.Game;

namespace ConsoleFakeClient
{
    class Program
    {
        static void Main(string[] args)
        {
            KSPMGlobals.Globals.InitiLogging(Log.LogginMode.Console, false);
            string userName = "Scr_Ra(0_o)";
            byte[] utf8Bytes;
            UTF8Encoding utf8Encoder = new UTF8Encoding();
            utf8Bytes = utf8Encoder.GetBytes(userName);
            GameUser myUser = new GameUser(ref userName, ref utf8Bytes);
            ServerInformation server = new ServerInformation();
            server.ip = "192.168.15.11";
            server.port = 4700;
            GameClient client = new GameClient();
            client.SetGameUser(myUser);
            client.SetServerHostInformation(server);
            client.InitializeClient();
            client.Connect();
            Console.ReadLine();
            client.Disconnect();
            Console.ReadLine();
            client.Release();
        }
    }
}
