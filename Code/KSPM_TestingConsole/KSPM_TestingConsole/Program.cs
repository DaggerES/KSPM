using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KSPM.Network.Server;
using KSPM.IO.Logging;
using KSPM.Diagnostics;
using KSPM.Globals;
using System.Xml.Serialization;
using System.Xml;
using System.IO;

namespace KSPM_TestingConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            KSPMGlobals.Globals.InitiLogging(Log.LogginMode.Console, false);
            ServerSettings gameSettings = null;
            ServerSettings.ReadSettings(ref gameSettings);
            GameServer server = new GameServer(ref gameSettings);
            KSPMGlobals.Globals.SetServerReference(ref server);
            server.StartServer();
            Console.ReadLine();
            server.ShutdownServer();
        }

    }
}
