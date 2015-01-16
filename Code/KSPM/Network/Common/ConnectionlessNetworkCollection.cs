namespace KSPM.Network.Common
{
    /// <summary>
    /// Class representing Non-oriented network connections, those which uses UDP or some other similar protocol.
    /// </summary>
    public class ConnectionlessNetworkCollection : NetworkBaseCollection, System.IDisposable
    {
        /// <summary>
        /// Remote endpoint holding the information.
        /// </summary>
        public System.Net.EndPoint remoteEndPoint;

        /// <summary>
        /// Creates an empty object.
        /// </summary>
        /// <param name="bufferSize"></param>
        public ConnectionlessNetworkCollection(int bufferSize)
            : base(bufferSize)
        {
            this.remoteEndPoint = new System.Net.IPEndPoint(0, 0);
        }

        /// <summary>
        /// Creates an Empty reference, so each property must be initializided before.
        /// </summary>
        public ConnectionlessNetworkCollection() : base()
        {
            this.remoteEndPoint = null;
        }

        /// <summary>
        /// Overrides the Dispose method from NetworkBaseCollection.
        /// </summary>
        new public void Dispose()
        {
            base.Dispose();
            this.remoteEndPoint = null;
        }

        /// <summary>
        /// Clones this NetworkCollection to the another;
        /// </summary>
        /// <param name="newReference"></param>
        public override void Clone(out NetworkBaseCollection newReference)
        {
            newReference = this;
            ConnectionlessNetworkCollection reference = (ConnectionlessNetworkCollection)newReference;
            newReference.rawBuffer = this.rawBuffer;
            newReference.secondaryRawBuffer = this.secondaryRawBuffer;
            newReference.socketReference = this.socketReference;
            reference.remoteEndPoint = this.remoteEndPoint;
        }
    }
}
