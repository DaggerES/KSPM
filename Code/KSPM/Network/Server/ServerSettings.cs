using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Xml;

namespace KSPM.Network.Server
{
    public class ServerSettings : AbstractSettings
    {
        [XmlIgnore]
        public static string SettingsFilename = "serverSettings.xml";
        [XmlElement("TCPPort")]
        public int tcpPort;
        [XmlElement("MaxConnectedClients")]
        public uint maxConnectedClients;
        [XmlIgnore]
        public int connectionsBackog;

        public static bool ReadSettings(ref ServerSettings settings)
        {
            bool success = false;
            XmlSerializer settingsSerializer;
            XmlTextReader settingsReader;
            settingsReader = new XmlTextReader(ServerSettings.SettingsFilename);
            settingsSerializer = new XmlSerializer(typeof(ServerSettings));
            try
            {
                settings = (ServerSettings)settingsSerializer.Deserialize(settingsReader, "http://www.w3.org/2001/XMLSchema-instance");
                success = true;
            }
            catch (InvalidOperationException)
            {
            }
            return success;
        }

        public static bool WriteSettings(ref ServerSettings settings)
        {
            bool success = false;
            XmlTextWriter settingsWriter;
            XmlSerializer settingsSerializer;
            settingsWriter = new XmlTextWriter(ServerSettings.SettingsFilename, UTF8Encoding.UTF8);
            settingsWriter.Formatting = Formatting.Indented;
            settingsSerializer = new XmlSerializer(typeof(ServerSettings));
            try
            {
                settingsSerializer.Serialize(settingsWriter, settings);
                settingsWriter.Close();
                success = true;
            }
            catch (InvalidOperationException)
            {
            }
            return success;
        }
    }
}
