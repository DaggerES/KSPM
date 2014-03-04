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
        /// Holds the main references to work with, Socket, MainBuffer, SecondaryBuffer.
        /// </summary>
        public NetworkBaseCollection ownerNetworkCollection;

        /// <summary>
        /// An unique ID.
        /// </summary>
        protected System.Guid id;

        protected NetworkRawEntity()
        {
            this.ownerNetworkCollection = null;
            this.id = System.Guid.NewGuid();
        }

        public NetworkRawEntity(ref Socket owner)
        {
            this.id = System.Guid.NewGuid();
            this.ownerNetworkCollection = new NetworkBaseCollection(KSPM.Network.Server.ServerSettings.ServerBufferSize);
            this.ownerNetworkCollection.socketReference = owner;
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
