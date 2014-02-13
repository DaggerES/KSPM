using System.Net;
using System.Net.Sockets;

/*
 * Server is handled as an entity, because the cliente itself would not have any idea to whom is sending the package.
 */

namespace KSPM.Network.Common
{
    /// <summary>
    /// Represents an interpretation of the server and the clients, having only the essential information to know what kind of Entity represents, either the server or the client.
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
        /// Virtual method called once a message is sent, you should override it if you want to perform some task.
        /// </summary>
        public virtual void MessageSent() { }

        /// <summary>
        /// Sets to null each member.
        /// </summary>
        public void Dispose()
        {
            this.ownerSocket = null;
            this.rawBuffer = null;
            this.secondaryRawBuffer = null;
        }

        /// <summary>
        /// Releases all the resources on the NetworkEntity reference. <b>Do not confuse this method with the Dispose one.</b>
        /// </summary>
        public override void Release()
        {
            this.rawBuffer = null;
            this.secondaryRawBuffer = null;
            if (this.ownerSocket != null && this.ownerSocket.Connected)
            {
                this.ownerSocket.Disconnect(false);
                this.ownerSocket.Shutdown(SocketShutdown.Both);
                this.ownerSocket.Close();
            }
            this.ownerSocket = null;
        }

        public override bool Equals(object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }
            else
            {
                NetworkEntity reference = (NetworkEntity)obj;
                return reference.id == this.id;
            }
        }
    }
}