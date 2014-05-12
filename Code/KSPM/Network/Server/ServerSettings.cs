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

        [XmlIgnore]
        protected static readonly int UDPPortRangeStart = 50000;
        [XmlIgnore]
        protected static readonly int UDPPortRangeEnd = 50500;

        /// <summary>
        /// Tells the size of those buffers used internally such as the PacketHandler buffer.
        /// </summary>
        [XmlIgnore]
        public static readonly uint PoolingCacheSize = 16;

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

        [XmlElement("TCPPort")]
        public int tcpPort;

        [XmlElement("UDPPortRange")]
        public IOPortManager.AssignablePortRange udpPortRange;

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

        /// <summary>
        /// Load a default sattings values into the given paremeter.
        /// </summary>
        /// <param name="settings"></param>
        public static void DefaultSettings(out ServerSettings settings)
        {
            settings = new ServerSettings();
            settings.connectionsBackog = ServerSettings.ServerConnectionsBacklog;
            settings.maxAuthenticationAttempts = ServerSettings.ServerAuthenticationAllowingTries;
            settings.maxConnectedClients = ServerSettings.ServerMaxConnectedClients;
            settings.tcpPort = ServerSettings.DefaultTCPListeningPort;
            settings.udpPortRange.assignablePortStart = ServerSettings.UDPPortRangeStart;
            settings.udpPortRange.assignablePortEnd = ServerSettings.UDPPortRangeEnd;
        }

        /// <summary>
        /// <b>Does nothing.</b>
        /// </summary>
        public override void Release()
        {
        }
    }
}
