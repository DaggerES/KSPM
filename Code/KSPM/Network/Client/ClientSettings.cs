using System.Xml.Serialization;
using System.Xml;
using System.IO;

namespace KSPM.Network.Client
{
    /// <summary>
    /// Class to hold the setting on the client side.
    /// </summary>
    public class ClientSettings : KSPM.Network.Common.AbstractSettings
    {
        /// <summary>
        /// Default name of the settings file.
        /// </summary>
        [XmlIgnore]
        public static string SettingsFilename = "clientSettings.xml";

        /// <summary>
        /// Size of the buffer to be used on each byte array on the client side.<b>DO NOT change it if you do not what you are doing.</b>
        /// It uses the multiplier 1.
        /// </summary>
        [XmlIgnore]
        public static readonly int ClientBufferSize = 1024 * 1;

        /// <summary>
        /// Sets in which port the client will be working with TCP packets.
        /// </summary>
        [XmlIgnore]
        protected static readonly int ClientTCPPort = 4800;

        /// <summary>
        /// Sets in which port the client will be working with UDP packets.
        /// </summary>
        [XmlIgnore]
        protected static readonly int ClientUDPPort = 4801;

        /// <summary>
        /// Sets the maximum amount of time to be awaited for when a connection process is performed.
        /// </summary>
        [XmlIgnore]
        protected static long ClientConnectionTimeOut = 5000;

        /// <summary>
        /// Sets the interval of time to send a KeepAlive command.
        /// </summary>
        [XmlIgnore]
        public static readonly long TCPKeepAliveInterval = 3600000;

        /// <summary>
        /// Sets the amount of time that the system will check if it is able to process commands.<b>DO NOT CHANGE IT IF YOU DON NOT KNOW WHAT YOU ARE DOING.</b>
        /// </summary>
        [XmlIgnore]
        public static readonly long PurgeTimeIterval = 1000;

        /// <summary>
        /// Tells how much available space has to have the queue to start accepting messages.<b>Is set in percent.</b>
        /// </summary>
        [XmlIgnore]
        public static readonly float AvailablePercentAfterPurge = 0.95f;

        /// <summary>
        /// Local port used by the TCP channel.
        /// </summary>
        [XmlElement("TCPPort")]
        public int tcpPort;

        /// <summary>
        /// Local port used by the UDP channel.
        /// </summary>
        [XmlElement("UDPPort")]
        public int udpPort;

        /// <summary>
        /// Amount of time that marks when the connection has taken so much time and it must be terminated or do something about it.
        /// </summary>
        [XmlElement("NetworkTimeout")]
        public long connectionTimeout;

        /// <summary>
        /// Read the settings file and inflate an object with the stored information.
        /// If an error happens a default settings are created.
        /// </summary>
        /// <param name="settings">Out Reference to the ClientSettings object which would be filled.</param>
        /// <returns>Ok or SettingsCanNotBeWritten.</returns>
        public static KSPM.Network.Common.Error.ErrorType ReadSettings(out ClientSettings settings)
        {
            KSPM.Network.Common.Error.ErrorType result = Common.Error.ErrorType.Ok;
            StreamReader settingsStreamReader;
            XmlSerializer settingsSerializer;
            XmlTextReader settingsReader;
            settings = null;
            try
            {
                settingsStreamReader = new StreamReader( KSPM.Globals.KSPMGlobals.Globals.IOFilePath + ClientSettings.SettingsFilename, System.Text.UTF8Encoding.UTF8);
                settingsReader = new XmlTextReader(settingsStreamReader);
                settingsSerializer = new XmlSerializer(typeof(ClientSettings));
                settings = (ClientSettings)settingsSerializer.Deserialize(settingsReader);
                settingsReader.Close();
                settingsStreamReader.Close();
            }
            catch (FileNotFoundException)///If the file can not be loaded a default one is created iand written.
            {
                ClientSettings.DefaultSettings(out settings);
                result = ClientSettings.WriteSettings(ref settings);
            }
            catch (DirectoryNotFoundException)
            { }
            catch (IOException)
            { }
            catch (System.InvalidOperationException)
            { }
            return result;
        }

        /// <summary>
        /// Write the settings object into a Xml file using the UTF8 encoding.
        /// If an error happens a default settings are created.
        /// </summary>
        /// <param name="settings">Reference to the ServerSettings object</param>
        /// <returns>Ok or SettingsCanNotBeWritten.</returns>
        public static KSPM.Network.Common.Error.ErrorType WriteSettings(ref ClientSettings settings)
        {
            XmlTextWriter settingsWriter;
            XmlSerializer settingsSerializer;
            KSPM.Network.Common.Error.ErrorType result = Common.Error.ErrorType.Ok;
            if (settings == null)
            {
                ClientSettings.DefaultSettings(out settings);
            }
            settingsWriter = new XmlTextWriter( KSPM.Globals.KSPMGlobals.Globals.IOFilePath + ClientSettings.SettingsFilename, System.Text.UTF8Encoding.UTF8);
            settingsWriter.Formatting = Formatting.Indented;
            settingsSerializer = new XmlSerializer(typeof(ClientSettings));
            try
            {
                settingsSerializer.Serialize(settingsWriter, settings);
                settingsWriter.Close();
            }
            catch (System.Exception ex)
            {
                result = Common.Error.ErrorType.IOFileCanNotBeWritten;
                KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo(ex.Message);
            }
            return result;
        }

        /// <summary>
        /// Crates a default settings object.
        /// </summary>
        /// <param name="settings"></param>
        public static void DefaultSettings(out ClientSettings settings)
        {
            settings = new ClientSettings();
            settings.connectionTimeout = ClientSettings.ClientConnectionTimeOut;
            settings.tcpPort = ClientSettings.ClientTCPPort;
            settings.udpPort = ClientSettings.ClientUDPPort;
        }

        /// <summary>
        /// At this moment does not do anything.
        /// </summary>
        public override void Release()
        {
        }
    }
}
