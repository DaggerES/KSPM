using System;
using System.Xml.Serialization;
using System.Xml;
using System.IO;

using KSPM.Network.Common;

namespace KSPM.Network.Server
{
    /// <summary>
    /// Class to handle the settings used by the server for its proper operation.
    /// </summary>
    public class ServerSettings : KSPM.Network.Common.AbstractSettings
    {
        [XmlIgnore]
        public static string SettingsFilename = "serversettings.xml";
        [XmlIgnore]
        public static readonly int ServerBufferSize = 1024*1;
        [XmlIgnore]
        protected static int ServerConnectionsBacklog = 10;
        [XmlIgnore]
        protected static int ServerAuthenticationAllowingTries = 3;
		[XmlIgnore]
		public static readonly int DefaultTCPListeningPort = 4700;
        [XmlIgnore]
        public static readonly long ConnectionProcessTimeOut = 5000;
        [XmlIgnore]
        protected static readonly uint ServerMaxConnectedClients = 8;

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
        public static Error.ErrorType ReadSettings(out ServerSettings settings)
        {
            Error.ErrorType success = Error.ErrorType.Ok;
            StreamReader settingsStreamReader;
            XmlSerializer settingsSerializer;
            XmlTextReader settingsReader;
            settings = null;
            try
            {
                settingsStreamReader = new StreamReader(KSPM.Globals.KSPMGlobals.Globals.IOFilePath + ServerSettings.SettingsFilename, System.Text.UTF8Encoding.UTF8);
                settingsReader = new XmlTextReader(settingsStreamReader);
                settingsSerializer = new XmlSerializer(typeof(ServerSettings));
                settings = (ServerSettings)settingsSerializer.Deserialize(settingsReader);
                settings.connectionsBackog = ServerSettings.ServerConnectionsBacklog;
            }
            catch (InvalidOperationException)
            {
                ServerSettings.DefaultSettings(out settings);
                success = ServerSettings.WriteSettings(ref settings);
            }
            catch (DirectoryNotFoundException ex)
            {
                success = Error.ErrorType.IODirectoryNotFound;
                KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo(ex.Message);
            }
            catch (FileNotFoundException)
            {
                ServerSettings.DefaultSettings(out settings);
                success = ServerSettings.WriteSettings(ref settings);
            }
            return success;
        }

        /// <summary>
        /// Write the settings object into a Xml file using the UTF8 encoding.
        /// </summary>
        /// <param name="settings">Reference to the ServerSettings object</param>
        /// <returns>False if there was an error such as if the reference is set to a null.</returns>
        public static Error.ErrorType WriteSettings(ref ServerSettings settings)
        {
            Error.ErrorType success = Error.ErrorType.Ok;
            XmlTextWriter settingsWriter;
            XmlSerializer settingsSerializer;
            if (settings == null)
            {
                ServerSettings.DefaultSettings(out settings);
            }
            try
            {
                settingsWriter = new XmlTextWriter(KSPM.Globals.KSPMGlobals.Globals.IOFilePath + ServerSettings.SettingsFilename, System.Text.UTF8Encoding.UTF8);
                settingsWriter.Formatting = Formatting.Indented;
                settingsSerializer = new XmlSerializer(typeof(ServerSettings));
                settingsSerializer.Serialize(settingsWriter, settings);
                settingsWriter.Close();
            }
            catch (InvalidOperationException ex)
            {
                success = Error.ErrorType.IOFileCanNotBeWritten;
                KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo(ex.Message);
            }
            catch (DirectoryNotFoundException)
            {
                success = Error.ErrorType.IODirectoryNotFound;
            }
            return success;
        }

        public static void DefaultSettings(out ServerSettings settings)
        {
            settings = new ServerSettings();
            settings.connectionsBackog = ServerSettings.ServerConnectionsBacklog;
            settings.maxAuthenticationAttempts = ServerSettings.ServerAuthenticationAllowingTries;
            settings.maxConnectedClients = ServerSettings.ServerMaxConnectedClients;
            settings.tcpPort = ServerSettings.DefaultTCPListeningPort;
        }

        public override void Release()
        {
        }
    }
}
