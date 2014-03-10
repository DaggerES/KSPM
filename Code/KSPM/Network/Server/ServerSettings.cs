using System;
using System.Xml.Serialization;
using System.Xml;
using System.IO;

namespace KSPM.Network.Server
{
    /// <summary>
    /// Class to handle the settings used by the server for its proper operation.
    /// </summary>
    public class ServerSettings : KSPM.Network.Common.AbstractSettings
    {
        [XmlIgnore]
        public static string SettingsFilename = "serverSettings.xml";
        [XmlIgnore]
        public static readonly int ServerBufferSize = 1024*1;
        [XmlIgnore]
        protected static int ServerConnectionsBacklog = 10;
        [XmlIgnore]
        protected static int ServerAuthenticationAllowingTries = 3;
		[XmlIgnore]
		public static readonly int DefaultTCPListeningPort = 4700;

        [XmlElement("TCPPort")]
        public int tcpPort;
        [XmlElement("MaxConnectedClients")]
        public uint maxConnectedClients;
        [XmlElement("AuthenticationAttempts")]
        public int maxAuthenticationAttempts;

        /// <summary>
        /// The maximun of enqueued connections that the TCP socket can handle.
        /// </summary>
        [XmlIgnore]
        public int connectionsBackog;

        /// <summary>
        /// Read the settings file and inflate an object with the stored information.
        /// </summary>
        /// <param name="settings">Reference to the ServerSettings object which would be filled.</param>
        /// <returns>False if there was an error during the write task.</returns>
        public static bool ReadSettings(ref ServerSettings settings)
        {
            bool success = false;
            StreamReader settingsStreamReader;
            XmlSerializer settingsSerializer;
            XmlTextReader settingsReader;
            settingsStreamReader = new StreamReader(ServerSettings.SettingsFilename, System.Text.UTF8Encoding.UTF8);
            settingsReader = new XmlTextReader(settingsStreamReader);
            settingsSerializer = new XmlSerializer(typeof(ServerSettings));
            try
            {
                settings = (ServerSettings)settingsSerializer.Deserialize(settingsReader);
                settings.connectionsBackog = ServerSettings.ServerConnectionsBacklog;
                success = true;
            }
            catch (InvalidOperationException)
            {
            }
            return success;
        }

        /// <summary>
        /// Write the settings object into a Xml file using the UTF8 encoding.
        /// </summary>
        /// <param name="settings">Reference to the ServerSettings object</param>
        /// <returns>False if there was an error such as if the reference is set to a null.</returns>
        public static bool WriteSettings(ref ServerSettings settings)
        {
            bool success = false;
            XmlTextWriter settingsWriter;
            XmlSerializer settingsSerializer;
            if (settings == null)
                return false;
            settingsWriter = new XmlTextWriter(ServerSettings.SettingsFilename, System.Text.UTF8Encoding.UTF8);
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

        public override void Release()
        {
        }
    }
}
