using System.Net;
using System.Net.Sockets;
using System.Threading;
using KSPM.Network.Common;


namespace KSPM.Network.Server
{
    /// <summary>
    /// Represents a client handled by the server.
    /// </summary>
    public class ServerSideClient : NetworkEntity
    {
        public enum ClientStatus : byte { Handshaking = 0, Handshaked, Authenticating, UDPConnecting, Connected};

        /// <summary>
        /// Thread to run the main body of the thread.
        /// </summary>
        protected Thread mainThread;

        /// <summary>
        /// Constrols the mainThread lifecycle.
        /// </summary>
        protected bool aliveFlag;

        protected ClientStatus currentStatus;

        protected ServerSideClient() : base()
        {
            this.currentStatus = ClientStatus.Handshaking;
            this.mainThread = new Thread(new ThreadStart(this.HandleMainBodyMethod));
        }

        /// <summary>
        /// Creates a ServerSideCliente object from a NetworkEntity reference and then disclose the network entity.
        /// </summary>
        /// <param name="baseNetworkEntity">Reference (ref) to the NetwrokEntity used as a base to create the new ServerSideClient object.</param>
        /// <param name="ssClient">New server side clint out reference.</param>
        /// <returns></returns>
        public static Error.ErrorType CreateFromNetworkEntity(ref NetworkEntity baseNetworkEntity, out ServerSideClient ssClient )
        {
            ssClient = null;
            if (baseNetworkEntity == null)
            {
                return Error.ErrorType.InvalidNetworkEntity;
            }
            ssClient = new ServerSideClient();
            ssClient.ownerSocket = baseNetworkEntity.ownerSocket;
            ssClient.rawBuffer = baseNetworkEntity.rawBuffer;
            baseNetworkEntity.Dispose();
            return Error.ErrorType.Ok;
        }

        /// <summary>
        /// Handles the main behaviour of the server side clien.
        /// </summary>
        protected void HandleMainBodyMethod()
        {
            while (this.aliveFlag)
            {
                switch (this.currentStatus)
                {
                    case ClientStatus.Handshaking:
                        break;
                }
            }
        }
    }
}
