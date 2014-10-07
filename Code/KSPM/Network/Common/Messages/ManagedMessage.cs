using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KSPM.Network.Common.Messages
{
    /// <summary>
    /// Message capable to hold to which NetworkEntity belongs.
    /// </summary>
    public class ManagedMessage : Message
    {
        /// <summary>
        /// A network entity which is owner of the message.
        /// </summary>
        protected NetworkEntity messageOwner;

        /// <summary>
        /// Index to tell where the bodymessage starts.
        /// </summary>
        protected uint startsAt;

        /// <summary>
        /// Creates an instance and set the NetworkEntity owner of this message.<b>The bodyMessage is set to Null, BE CAREFUL WITH THAT.</b>
        /// </summary>
        /// <param name="commandType"></param>
        /// <param name="messageOwner"></param>
        public ManagedMessage(CommandType commandType, NetworkEntity messageOwner) : base( commandType )
        {
            this.messageOwner = messageOwner;
            this.startsAt = 0;
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
            this.MessageId = 0;
            this.Priority = Globals.KSPMSystem.PriorityLevel.Disposable;
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

        /// <summary>
        /// Releases each property of the message, this method verifies if the ManagedMessage.broadcasted flag is set to true, so in that case the bodyMessage property will be kept,
        /// otherwise the bodyMessage is released too.
        /// </summary>
        public override void Dispose()
        {
            this.messageOwner = null;
            this.messageRawLength = 0;
            if (!this.broadcasted)
            {
                ///Releasing the body message is passed to the BroadcastMessage, so you don't have to worry abou it.
                this.bodyMessage = null;
            }
            this.broadcasted = false;
            this.MessageId = 0;
            this.Priority = Globals.KSPMSystem.PriorityLevel.Disposable;
        }

        /// <summary>
        /// Gets the index posision where the message starts inside the byte array.
        /// </summary>
        public uint StartsAt
        {
            get
            {
                return this.startsAt;
            }
            set
            {
                this.startsAt = value;
            }
        }
    }
}
