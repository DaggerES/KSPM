using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KSPM.Network.Common.Messages
{
    public class ManagedMessage : Message
    {
        /// <summary>
        /// A network entity which is owner of the message.
        /// </summary>
        protected NetworkEntity messageOwner;

        /// <summary>
        /// Creates an instance and set the NetworkEntity owner of this message.
        /// </summary>
        /// <param name="commandType"></param>
        /// <param name="messageOwner"></param>
        public ManagedMessage(CommandType commandType, NetworkEntity messageOwner) : base( commandType )
        {
            this.messageOwner = messageOwner;
        }

        /// <summary>
        /// Sets a new NetworkEntity owner for this message.
        /// </summary>
        /// <param name="messageOwner"></param>
        public void SetOwnerMessageNetworkEntity(NetworkEntity messageOwner)
        {
            this.messageOwner = messageOwner;
        }

        /// <summary>
        /// Return the current NetworkEntity owner of this message.
        /// </summary>
        public NetworkEntity OwnerNetworkEntity
        {
            get
            {
                return this.messageOwner;
            }
        }

        /// <summary>
        /// Sets messageOwner to null and he messageRawLength to 0.<b>If you want to clean everythin you have to set to null this reference.</b>
        /// </summary>
        public override void Release()
        {
            this.messageOwner = null;
            this.messageRawLength = 0;
            if (!this.broadcasted)
            {
                ///Releasing the body message is passed to the BroadcastMessage, so you don't have to worry abou it.
                this.bodyMessage = null;
            }
            this.broadcasted = false;
        }

        /// <summary>
        /// Returns a new instance of the same class.
        /// </summary>
        /// <returns></returns>
        public override Message Empty()
        {
            ManagedMessage item = new ManagedMessage(CommandType.Null, null);
            return item;
        }

        public void SwapReceivedBufferToSend(ManagedMessage otherMessage)
        {
            System.Buffer.BlockCopy(otherMessage.OwnerNetworkEntity.ownerNetworkCollection.secondaryRawBuffer, 0, this.OwnerNetworkEntity.ownerNetworkCollection.rawBuffer, 0, (int)otherMessage.messageRawLength);
            this.messageRawLength = otherMessage.messageRawLength;
        }
    }
}
