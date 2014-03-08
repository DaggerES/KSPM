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
            ServerList hosts = null;
            ServerList.ReadServerList(out hosts);
            //server.ip = "189.210.119.226";
            /*server.ip = "192.168.15.16";
            server.port = 4700;
            server.name = "Testeando";
            hosts.Hosts.Add(server);*/
            ServerList.WriteServerList(ref hosts);
            GameClient client = new GameClient();
            ConsoleKeyInfo pressedKey;
            bool exit = false;
            //client.SetGameUser(myUser);
            //client.SetServerHostInformation(server);
            client.InitializeClient();
            while ( !exit )
            {
                /*
                Console.WriteLine("Press q to quit");
                Console.WriteLine("Press r to connect");
                Console.WriteLine("Press d to disconnect");
                pressedKey = Console.ReadKey();
                switch( pressedKey.Key )
                {
                    case ConsoleKey.Q:
                        exit = true;
                        break;
                    case ConsoleKey.R:
                        client.SetGameUser(myUser);
                        client.SetServerHostInformation(hosts.Hosts[ 0 ]);
                        client.Connect();
                        break;
                    case ConsoleKey.D:
                        client.Disconnect();
                        break;
                    default:
                        break;
                }
                */
                
                client.SetGameUser(myUser);
                client.SetServerHostInformation(hosts.Hosts[0]);
                client.Connect();
                System.Threading.Thread.Sleep(10000);
                client.Disconnect();
                
            }

            client.Disconnect();
            Console.ReadLine();
            client.Release();
        }
    }
}
