using System.Net;
using System.Net.Sockets;
using System.Diagnostics;

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

        /// <summary>
        /// A timer to tells how much time has been alive this object reference.
        /// </summary>
        protected Stopwatch timer;

        protected NetworkRawEntity()
        {
            this.ownerNetworkCollection = null;
            this.id = System.Guid.NewGuid();
            this.timer = new Stopwatch();
            this.timer.Start();
        }

        public NetworkRawEntity(ref Socket owner)
        {
            this.id = System.Guid.NewGuid();
            this.ownerNetworkCollection = new NetworkBaseCollection(KSPM.Network.Server.ServerSettings.ServerBufferSize);
            this.ownerNetworkCollection.socketReference = owner;
            this.timer = new Stopwatch();
            this.timer.Start();
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
        /// Gets the amount of miliseconds which this reference has been alive.
        /// </summary>
        public long AliveTime
        {
            get
            {
                return this.timer.ElapsedMilliseconds;
            }
        }

        /// <summary>
        /// Abstract method that should be used to release all the resources ocupied by the object itself, such as the socket and the arrays.
        /// </summary>
        public abstract void Release();

        public abstract bool IsAlive();
    }
}
