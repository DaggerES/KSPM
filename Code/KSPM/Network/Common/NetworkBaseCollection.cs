using System.Net.Sockets;

namespace KSPM.Network.Common
{
    /// <summary>
    /// Holds the basic properties to create a network connection.
    /// </summary>
    public class NetworkBaseCollection : System.IDisposable
    {
        public Socket socketReference;
        public byte[] rawBuffer;
        public byte[] secondaryRawBuffer;

        /// <summary>
        /// Initializes the buffers, but the Socket is set to null.
        /// <see cref="Socket"/>
        /// </summary>
        /// <param name="buffersSize">Size used to alloc the memory buffers.</param>
        public NetworkBaseCollection(int buffersSize)
        {
            this.rawBuffer = new byte[buffersSize];
            this.secondaryRawBuffer = new byte[buffersSize];
            this.socketReference = null;
        }

        /// <summary>
        /// Creates an Empty reference, so it must be initialized before.
        /// Each property is set to null.
        /// </summary>
        public NetworkBaseCollection()
        {
            this.rawBuffer = null;
            this.secondaryRawBuffer = null;
            this.socketReference = null;
        }

        /// <summary>
        /// Releases the buffers and set the Socket to null, so It has to be shut down by you.
        /// <see cref="Socket"/>
        /// </summary>
        public void Dispose()
        {
            this.rawBuffer = null;
            this.secondaryRawBuffer = null;
            this.socketReference = null;
        }

        public virtual void Clone(out NetworkBaseCollection newReference)
        {
            newReference = this;
            newReference.rawBuffer = this.rawBuffer;
            newReference.secondaryRawBuffer = this.secondaryRawBuffer;
            newReference.socketReference = this.socketReference;
        }
    }
}
