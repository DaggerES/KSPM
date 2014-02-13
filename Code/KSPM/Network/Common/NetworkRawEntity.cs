using System.Net;
using System.Net.Sockets;

namespace KSPM.Network.Common
{
    /// <summary>
    /// Represents a basic network object used as wrapper to the async socket methods.
    /// In the future it should be able to convert one this objects to another object, such a serverside client or such.
    /// </summary>
    public abstract class NetworkRawEntity
    {
        /// <summary>
        /// The byte array which will used as a buffer to send/receive methods.
        /// </summary>
        public byte[] rawBuffer;

        /// <summary>
        /// Secondary array which should be used as a complement to the send/receive methods, because if there are simultaneous send/receive operations, the buffer is overwritten.
        /// </summary>
        public byte[] secondaryRawBuffer;

        /// <summary>
        /// The socket which is the owner of the entity;
        /// </summary>
        public Socket ownerSocket;

        /// <summary>
        /// An unique ID.
        /// </summary>
        protected System.Guid id;

        protected NetworkRawEntity()
        {
            this.rawBuffer = null;
            this.secondaryRawBuffer = null;
            this.ownerSocket = null;
            this.id = System.Guid.NewGuid();
        }

        public NetworkRawEntity(ref Socket owner)
        {
            this.id = System.Guid.NewGuid();
            this.ownerSocket = owner;
            this.rawBuffer = new byte[ KSPM.Network.Server.ServerSettings.ServerBufferSize];
            this.secondaryRawBuffer = new byte[KSPM.Network.Server.ServerSettings.ServerBufferSize];
        }

        /// <summary>
        /// Returnt he ID as readonly.
        /// </summary>
        public System.Guid Id
        {
            get
            {
                return this.id;
            }
        }

        /// <summary>
        /// Abstract method that should be used to release all the resources ocupied by the object itself, such as the socket and the arrays.
        /// </summary>
        public abstract void Release();
    }
}
