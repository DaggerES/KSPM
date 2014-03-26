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
        /// Delegate prototype of the method that can be set and called when the time comes.
        /// </summary>
        /// <param name="caller">NetworkEntity who performs the call.</param>
        /// <param name="arg">object which can hold any parameter, in case that it could be needed.</param>
        public delegate void MessageSentCallback(NetworkEntity caller, object arg);

        /// <summary>
        /// Set the method to the underlaying callback reference.
        /// </summary>
        /// <param name="method">Method to be called.</param>
        public void SetMessageSentCallback(MessageSentCallback method)
        {
            this.messageSentCallback += method;
        }

        /// <summary>
        /// Reference to an MessageSentCallback.
        /// </summary>
        protected MessageSentCallback messageSentCallback;

        /// <summary>
        /// Invoke a call over the MessageSentCallback reference.<b>If the reference is null, nothing is performed at all.</b> Once the method is invoked the callback reference is set to null.
        /// </summary>
        /// <param name="caller"></param>
        /// <param name="arg"></param>
        public void MessageSent( NetworkEntity caller, object arg)
        {
            if (this.messageSentCallback != null)
            {
                this.messageSentCallback(caller, arg);
                this.messageSentCallback = null;
            }
        }


        /// <summary>
        /// Constructs a new NetworkEntity
        /// </summary>
        /// <param name="entityOwner">Reference (ref) to the socket who will be the owner of this object.</param>
        public NetworkEntity(ref Socket entityOwner)
            : base(ref entityOwner)
        {
            this.messageSentCallback = null;
        }

        /// <summary>
        /// Creates an empty NetworkEntity
        /// </summary>
        protected NetworkEntity()
            : base()
        {
            this.messageSentCallback = null;
        }

        /// <summary>
        /// Sets to null each member.
        /// </summary>
        public void Dispose()
        {
            //this.ownerNetworkCollection.Dispose();
            this.ownerNetworkCollection = null;
        }

        /// <summary>
        /// Releases all the resources on the NetworkEntity reference. <b>Do not confuse this method with the Dispose one.</b>
        /// </summary>
        public override void Release()
        {
            if (this.ownerNetworkCollection.socketReference != null && this.ownerNetworkCollection.socketReference.Connected)
            {
                this.ownerNetworkCollection.socketReference.Disconnect(false);
                this.ownerNetworkCollection.socketReference.Close();
            }
            this.ownerNetworkCollection.Dispose();
            this.timer.Reset();
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

        public override int GetHashCode()
        {
            return this.id.GetHashCode();
        }

        public override bool IsAlive()
        {
            return true;
        }
    }
}