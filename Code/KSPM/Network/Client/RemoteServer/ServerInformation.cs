using System.Xml.Serialization;

namespace KSPM.Network.Client.RemoteServer
{
    public class ServerInformation : System.IDisposable
    {
		public static readonly ServerInformation LoopbackServerInformation = new ServerInformation( "Loopback", "127.0.0.1", KSPM.Network.Server.ServerSettings.DefaultTCPListeningPort );

        /// <summary>
        /// Server name, it is used by the user to identify them best.
        /// </summary>
        [XmlElement("ServerName")]
        public string name;

        /// <summary>
        /// Ip address of the server.
        /// </summary>
        [XmlElement("IP_Address")]
        public string ip;

        /// <summary>
        /// Port where the server will be listening for incoming connections.
        /// </summary>
        [XmlElement("PortNumber")]
        public int port;

		/// <summary>
		/// Creates a ServerInformation with the given parameters.
		/// </summary>
		/// <param name="serverName">Server name.</param>
		/// <param name="ip">Ip.</param>
		/// <param name="port">Port.</param>
		public ServerInformation( string serverName, string ip, int port )
		{
			this.name = serverName;
			this.ip = ip;
			this.port = port;
		}

		public ServerInformation()
		{}

        /// <summary>
        /// Releases the properties and set them to null.
        /// </summary>
        public void Dispose()
        {
            this.ip = null;
            this.port = -1;
        }

        public override bool Equals(object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }
            else
            {
                ServerInformation reference = (ServerInformation)obj;
                return reference.ip.Equals(this.ip) && reference.port == this.port;
            }
        }

        public override int GetHashCode()
        {
            return this.ip.GetHashCode() + this.port.GetHashCode();
        }
    }
}
