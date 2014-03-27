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
            Console.WriteLine(string.Format("{0} : {1}", "ASD", sizeof(int)));
            KSPMGlobals.Globals.InitiLogging(Log.LogginMode.File, false);

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

        }

    }
}
