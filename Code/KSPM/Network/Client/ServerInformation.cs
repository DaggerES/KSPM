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
    }
}
