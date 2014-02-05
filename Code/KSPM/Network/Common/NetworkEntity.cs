using System.Net;
using System.Net.Sockets;

/*
 * Server is handled as an entity, because the cliente itself would not have any idea to whom is sending the package.
 */

namespace KSPM.Network.Common
{
    /// <summary>
    /// Represents an abstract interpretation of the server and the clients, having only the essential information to know what kind of Entity represents, either the server or the client.
    /// </summary>
    public class NetworkEntity : NetworkRawEntity, System.IDisposable
    {
        /// <summary>
        /// Representes a loopback network entity, in other words an empty object.
        /// </summary>
        public static NetworkEntity LoopbackNetworkEntity = new NetworkEntity();

        /// <summary>
        /// Constructs a new NetworkEntity
        /// </summary>
        /// <param name="entityOwner">Reference (ref) to the socket who will be the owner of this object.</param>
        public NetworkEntity(ref Socket entityOwner)
            : base(ref entityOwner)
        {
        }

        /// <summary>
        /// Creates an empty NetworkEntity
        /// </summary>
        protected NetworkEntity()
            : base()
        {
        }

        /// <summary>
        /// Sets to null each member.
        /// </summary>
        public void Dispose()
        {
            this.ownerSocket = null;
            this.rawBuffer = null;
        }
    }
}
