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

        protected NetworkRawEntity()
        {
            this.rawBuffer = null;
            this.secondaryRawBuffer = null;
            this.ownerSocket = null;
        }

        public NetworkRawEntity(ref Socket owner)
        {
            this.ownerSocket = owner;
            this.rawBuffer = new byte[ KSPM.Network.Server.ServerSettings.ServerBufferSize];
            this.secondaryRawBuffer = new byte[KSPM.Network.Server.ServerSettings.ServerBufferSize];
        }
    }
}
