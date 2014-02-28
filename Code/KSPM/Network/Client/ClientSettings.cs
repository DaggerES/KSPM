using System.Xml.Serialization;
using System.Xml;
using System.IO;

namespace KSPM.Network.Client
{
    public class ClientSettings : KSPM.Network.Common.AbstractSettings
    {
        [XmlIgnore]
        public static string SettingsFilename = "clientSettings.xml";

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

        [XmlElement("TCPPort")]
        public int tcpPort;

        [XmlElement("UDPPort")]
        public int udpPort;

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
                settingsStreamReader = new StreamReader(ClientSettings.SettingsFilename, System.Text.UTF8Encoding.UTF8);
                settingsReader = new XmlTextReader(settingsStreamReader);
                settingsSerializer = new XmlSerializer(typeof(ClientSettings));
                settings = (ClientSettings)settingsSerializer.Deserialize(settingsReader);
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
            settingsWriter = new XmlTextWriter(ClientSettings.SettingsFilename, System.Text.UTF8Encoding.UTF8);
            settingsWriter.Formatting = Formatting.Indented;
            settingsSerializer = new XmlSerializer(typeof(ClientSettings));
            try
            {
                settingsSerializer.Serialize(settingsWriter, settings);
                settingsWriter.Close();
            }
            catch (System.Exception ex)
            {
                result = Common.Error.ErrorType.SettingsCanNotBeWritten;
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
