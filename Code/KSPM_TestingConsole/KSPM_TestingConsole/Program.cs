using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KSPM.Network.Server;
using System.Xml.Serialization;
using System.Xml;
using System.IO;

namespace KSPM_TestingConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            XmlTextReader reader = new XmlTextReader(ServerSettings.SettingsFilename);
            XmlSerializer serializer = new XmlSerializer(typeof(ServerSettings));
            ServerSettings settings = new ServerSettings();
            ServerSettings loadedSettings = null;
            ServerSettings.WriteSettings(ref settings);
            
            try
            {
                loadedSettings = (ServerSettings)serializer.Deserialize(reader, );
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine(ex.Message);
            }
            
            /*
            if (ServerSettings.ReadSettings(ref loadedSettings))
            {
                Console.WriteLine("OK");
            }
            else
            {
                Console.WriteLine("Error");
            }
            */
            Console.ReadLine();
        }

    }
}
