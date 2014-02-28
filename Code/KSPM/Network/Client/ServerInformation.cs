namespace KSPM.Network.Client
{
    public class ServerInformation : System.IDisposable
    {
        /// <summary>
        /// Ip address of the server.
        /// </summary>
        public string ip;

        /// <summary>
        /// Port where the server will be listening for incoming connections.
        /// </summary>
        public int port;

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
