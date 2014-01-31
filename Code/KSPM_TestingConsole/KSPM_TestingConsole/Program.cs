using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KSPM.Network.Server;
using KSPM.IO.Logging;
using KSPM.Diagnostics;
using System.Xml.Serialization;
using System.Xml;
using System.IO;

namespace KSPM_TestingConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            
            FileLog log = new FileLog(FileLog.GetAUniqueFilename("KSPMLog"), true);
            log.WriteTo( "Hola");
            
            //Console.WriteLine(RealTimer.GetCurrentDateTime());
            Console.ReadLine();
        }

    }
}
