using System.Xml.Serialization;

namespace KSPM.Network.Client.RemoteServer
{
    public class ServerInformation : System.IDisposable
    {
        [XmlIgnore]
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

        [XmlIgnore]
        protected System.Net.IPEndPoint networkEndPoint;

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
            try
            {
                this.networkEndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse(this.ip), this.port);
            }
            catch (System.Exception)
            {
                ///If anything happens a generic IpEndPoint is created.
                this.networkEndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Any, 0);
            }
		}

		public ServerInformation()
		{
            this.networkEndPoint = null;
        }

        /// <summary>
        /// Releases the properties and set them to null.
        /// </summary>
        public void Dispose()
        {
            this.ip = null;
            this.port = -1;
            this.name = null;
            this.networkEndPoint = null;
        }

        public override bool Equals(object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()) || this.port < 0 || this.ip == null)
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

        /// <summary>
        /// Gets the underlying IpEndPoint filled with the stored information.
        /// </summary>
        public System.Net.IPEndPoint NetworkEndPoint
        {
            get
            {
                if (this.networkEndPoint == null)
                {
                    try
                    {
                        this.networkEndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse(this.ip), this.port);
                    }
                    catch (System.Exception)
                    {
                        ///If anything wrong happens a generic IpEndPoint is created.
                        this.networkEndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Any, 0);
                    }
                }
                return this.networkEndPoint;
            }
        }

        /// <summary>
        /// Clones its content to the target reference.
        /// </summary>
        /// <param name="target"></param>
        public void Clone(ref ServerInformation target)
        {
            if (target == null)
            {
                target = new ServerInformation();
            }
            target.ip = this.ip;
            target.name = this.name;
            target.port = this.port;
            target.networkEndPoint = this.networkEndPoint;
        }
    }
}
