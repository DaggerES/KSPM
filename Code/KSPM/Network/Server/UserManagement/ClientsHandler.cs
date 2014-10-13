using System.Collections.Generic;
using KSPM.Network.Common;
using KSPM.Network.Common.Messages;

namespace KSPM.Network.Server.UserManagement
{
    /// <summary>
    /// A class that should handle the clients and store them into a data structure. It is threadsafe.
    /// </summary>
    public class ClientsHandler
    {
        /// <summary>
        /// List to provide a fast way to iterate through the server's clients.
        /// </summary>
        protected List<NetworkEntity> clients;

        /// <summary>
        /// Queue of free game user ids.
        /// </summary>
        protected Queue<byte> freeGameUserIds;

        /// <summary>
        /// Dictionary to privide a fast way to search among the server's clients.
        /// </summary>
        protected Dictionary<System.Guid, NetworkEntity > clientsEngine;

        /// <summary>
        /// Dictionary to provide a fast way to search a single NetworkEntity knowing its User id.
        /// </summary>
        protected Dictionary<int, NetworkEntity> clientsEngineByUserId;

        /// <summary>
        /// Holds how many connected users the system can support.
        /// </summary>
        protected byte supportedClients;

        /// <summary>
        /// Pool of NetworkEntity lists to be used on the selective broadcast methods.
        /// </summary>
        protected Queue<List<NetworkEntity>> BroadcastListsPool;

        /// <summary>
        /// Createas a new ClientsHandler reference and creates the user ids to be assigned to each user.
        /// Those ids are defined by [0:31].
        /// </summary>
        /// <param name="supportedClients">Amount of connected users supported by the system.</param>
        public ClientsHandler(byte supportedClients)
        {
            this.clients = new List<NetworkEntity>();
            this.clientsEngine = new Dictionary<System.Guid,NetworkEntity>();
            this.clientsEngineByUserId = new Dictionary<int, NetworkEntity>();
            this.supportedClients = supportedClients;
            this.freeGameUserIds = new Queue<byte>(this.supportedClients);
            ///Creating the pool multiplying the amount of supported users by 4 in order to avoid starvation.
            this.BroadcastListsPool = new Queue<List<NetworkEntity>>(supportedClients * 4);
            ///Writing the available user ids.
            for( byte i = 1; i <= this.supportedClients ; i++ )
            {
                this.freeGameUserIds.Enqueue(i);
                ///Adding a list to the pool.
                this.BroadcastListsPool.Enqueue(new List<NetworkEntity>(supportedClients));
            }
        }

        /// <summary>
        /// Locks the underlaying structures to add the NetworkEntity to the data structures, check if the network entity is not already stored.
        /// </summary>
        /// <param name="referredEntity"></param>
        public void AddNewClient(NetworkEntity referredEntity)
        {
            lock (this.clientsEngine)
            {
                if (!clientsEngine.ContainsKey(referredEntity.Id))
                {
                    this.clients.Add(referredEntity);
                    this.clientsEngine.Add(referredEntity.Id, referredEntity);
                }
            }
        }

        /// <summary>
        /// Removes a client from the data structures and calls the Release method on it.
        /// </summary>
        /// <param name="referredEntity"></param>
        public void RemoveClient(NetworkEntity referredEntity)
        {
            lock (this.clientsEngine)
            {
                if (clientsEngine.ContainsKey(referredEntity.Id))
                {
                    this.clients.Remove(referredEntity);
                    this.clientsEngine.Remove(referredEntity.Id);
                    referredEntity.Release();
                }
            }
        }

        /// <summary>
        /// Register a new GameUser into the system, making it available to be broadcasted through its id.
        /// </summary>
        /// <param name="referredGameUser"></param>
        public void RegisterNewUserClient( Game.GameUser referredGameUser)
        {
            lock( this.clientsEngine)
            {
                ///Verifying if the GameUser is already stored in the system, and proceeds to add it to the broadcasting queue.
                if( this.clientsEngine.ContainsKey(referredGameUser.Parent.Id))
                {
                    this.clientsEngineByUserId.Add(referredGameUser.Id, referredGameUser.Parent);
                }
            }
        }

        /// <summary>
        /// Removes a GameUser from the broadcasted quueue.<b>THIS IS NOT THREAD SAFE.</b>
        /// </summary>
        /// <param name="referredGameUser">GameUser reference to be deleted.</param>
        public void UnregisterUserClient( Game.GameUser referredGameUser)
        {
            this.clientsEngineByUserId.Remove(referredGameUser.Id);
            this.RecycleUserId(referredGameUser.Id);
        }

        /// <summary>
        /// Clears the structures and calls the ReleaseMethod of each NetworkEntity reference.
        /// </summary>
        public void Release()
        {
            lock (this.clients)
            {
                for (int i = 0; i < this.clients.Count; i++)
                {
                    this.clients[i].Release();
                }
                this.clients.Clear();
                this.clientsEngine.Clear();
            }
            this.clients = null;
            this.clientsEngine = null;
        }

        /// <summary>
        /// Gets the connected clients.
        /// </summary>
        public int ConnectedClients
        {
            get
            {
                lock (this.clients)
                {
                    return this.clients.Count;
                }
            }
        }

        /// <summary>
        /// Gets a complete list of the remote clients who are connected to the system.
        /// </summary>
        public List<NetworkEntity> RemoteClients
        {
            get
            {
                lock (this.clients)
                {
                    return this.clients;
                }
            }
        }

        /// <summary>
        /// Disconnect all the connected clients.
        /// </summary>
        internal void DisconnectAll()
        {
            NetworkEntity entity;
            lock (this.clientsEngine)
            {
                for (int i = 0; i < this.clients.Count; i++)
                {
                    entity = this.clients[i];
                    this.clients.Remove(entity);
                    this.clientsEngine.Remove(entity.Id);
                    entity.Release();
                    entity = null;
                }
            }
        }

        /// <summary>
        /// Gets a new Id in a cycling count.
        /// </summary>
        /// <returns>A free Id, -1 otherwise.</returns>
        public int NextUserId()
        {
            int newId = -1;
            if (this.freeGameUserIds.Count > 0)
            {
                lock (this.freeGameUserIds)
                {
                    newId = this.freeGameUserIds.Dequeue();
                }
            }
            return newId;
        }

        /// <summary>
        /// Takes the given user id and put them into the free user ids.
        /// </summary>
        /// <param name="disposedId"></param>
        public void RecycleUserId(int disposedId)
        {
            lock(this.freeGameUserIds)
            {
                this.freeGameUserIds.Enqueue((byte)disposedId);
            }
        }

        #region Pooling

        /// <summary>
        /// Borrows a list from the pool.<b>It creates a new list reference if the pool is empty.</b>
        /// </summary>
        /// <returns>A list reference.</returns>
        protected List<NetworkEntity> BorrowList()
        {
            List<NetworkEntity> list = null;
            lock( this.BroadcastListsPool)
            {
                if( this.BroadcastListsPool.Count > 0)
                {
                    list = this.BroadcastListsPool.Dequeue();
                }
            }
            if( list == null)
            {
                ///Means that the pool is empty.
                list = new List<NetworkEntity>(this.supportedClients);
            }
            return list;
        }

        /// <summary>
        /// Recycles a given list and push it back into the pool.
        /// </summary>
        /// <param name="referredList"></param>
        protected internal void RecycleList( List<NetworkEntity> referredList)
        {
            ///Cleaning the list erasing its content.
            referredList.Clear();
            lock( this.BroadcastListsPool)
            {
                this.BroadcastListsPool.Enqueue(referredList);
            }
        }

        #endregion

        #region Broadcasting

        /// <summary>
        /// Broadcast all connected clients using the UDP channel.
        /// </summary>
        /// <param name="messageToSend">Message reference to be broadcasted.</param>
        public void UDPBroadcastClients(Message messageToSend)
        {
            Message outgoingMessage = null;
            lock (this.clients)
            {
                for (int i = 0; i < this.clients.Count; i++)
                {
                    outgoingMessage = ((ServerSideClient)this.clients[i]).IOUDPMessagesPool.BorrowMessage;
                    ((RawMessage)outgoingMessage).LoadWith(messageToSend.bodyMessage, 0, messageToSend.MessageBytesSize);
                    outgoingMessage.IsBroadcast = true;
                    if (!((ServerSideClient)this.clients[i]).outgoingPackets.EnqueueCommandMessage(ref outgoingMessage))
                    {
                        ///If this code is reached means the outgoing queue is full.
                        ((ServerSideClient)this.clients[i]).IOUDPMessagesPool.Recycle(outgoingMessage);
                    }
                    else
                    {
                        ((ServerSideClient)this.clients[i]).SendUDPDatagram();
                    }
                    //((ServerSideClient)this.clients[i]).SendAsDatagram(messageToSend);
                }
            }
        }

        /// <summary>
        /// Creates a broadcast message taking the messageToSend as base.
        /// </summary>
        /// <param name="targets"></param>
        /// <param name="messageToSend"></param>
        public void TCPBroadcastTo(List<NetworkEntity> targets, Message messageToSend)
        {
            Message outgoingMessage = null;
            BroadcastMessage outgoingBroadcast = null;
            outgoingMessage = KSPM.Globals.KSPMGlobals.Globals.KSPMServer.broadcastMessagesPool.BorrowMessage;
            outgoingBroadcast = (BroadcastMessage)outgoingMessage;
            outgoingBroadcast.LoadWith(messageToSend.bodyMessage, ((BufferedMessage)messageToSend).StartsAt, messageToSend.MessageBytesSize);
            if(targets == null)
            {
                targets = this.clients;
            }

            ///Cloning the clients list, as fast as it is posible.
            for (int i = 0; i < targets.Count; i++ )
            {
                outgoingBroadcast.Targets.Add(targets[i]);
            }
            outgoingMessage.IsBroadcast = true;
                /*
                BroadcastMessage outgoingBroadcast = new BroadcastMessage(messageToSend.Command, targets);
                outgoingBroadcast.SetBodyMessage(messageToSend.bodyMessage, ((ManagedMessage)messageToSend).StartsAt, messageToSend.MessageBytesSize);
                outgoingMessage = outgoingBroadcast;
                outgoingMessage.IsBroadcast = true;
                */
            if (!KSPM.Globals.KSPMGlobals.Globals.KSPMServer.outgoingMessagesQueue.EnqueueCommandMessage(ref outgoingMessage))
            {
                KSPM.Globals.KSPMGlobals.Globals.KSPMServer.broadcastMessagesPool.Recycle(outgoingMessage);
                outgoingBroadcast = null;
            }
        }

        /// <summary>
        /// Broadcast the NetworkEntities especified by te user Ids flag.
        /// </summary>
        /// <param name="userIds"></param>
        /// <param name="messageToSend"></param>
        public void TCPSelectiveBroadcast(int userIds, Message messageToSend)
        {
            Message outgoingMessage = null;
            BroadcastMessage outgoingBroadcast = null;
            outgoingMessage = KSPM.Globals.KSPMGlobals.Globals.KSPMServer.broadcastMessagesPool.BorrowMessage;
            outgoingBroadcast = (BroadcastMessage)outgoingMessage;

            NetworkEntity targetEntity;
            int flag = 1;
            int targetId;

            ///Creating the clients list using the ids stored in the userIds flag.
            for( int i = 0 ; i < this.supportedClients ; i++)
            {
                flag = 1 << i;
                targetId = flag & userIds;
                if( targetId != 0 )
                {
                    if (this.clientsEngineByUserId.TryGetValue((i + 1), out targetEntity))
                    {
                        outgoingBroadcast.Targets.Add(targetEntity);
                    }
                }
            }

            outgoingBroadcast.LoadWith(messageToSend.bodyMessage, 0, messageToSend.MessageBytesSize);
            outgoingMessage.IsBroadcast = true;
            if (!KSPM.Globals.KSPMGlobals.Globals.KSPMServer.outgoingMessagesQueue.EnqueueCommandMessage(ref outgoingMessage))
            {
                KSPM.Globals.KSPMGlobals.Globals.KSPMServer.broadcastMessagesPool.Recycle(outgoingMessage);
                outgoingBroadcast = null;
            }
        }

        /// <summary>
        /// Broadcast the message throug the clients.<b>The ClientsId flag is took from the message itself, so if you use this method the message must have the flag starting on the 15th byte.</b>
        /// </summary>
        /// <param name="messageToSend">Message to send.</param>
        public void TCPSelectiveBroadcast(Message messageToSend)
        {
            int targetsIds = 0;
            Message outgoingMessage = null;
            BroadcastMessage outgoingBroadcast = null;
            outgoingMessage = KSPM.Globals.KSPMGlobals.Globals.KSPMServer.broadcastMessagesPool.BorrowMessage;
            outgoingBroadcast = (BroadcastMessage)outgoingMessage;

            NetworkEntity targetEntity;
            int flag = 1;
            int targetId;

            ///Getting the ids from the message's buffer.
            targetsIds = System.BitConverter.ToInt32(messageToSend.bodyMessage, (int)((BufferedMessage)messageToSend).StartsAt + 14);

            ///Creating the clients list using the ids stored in the userIds flag.
            for (int i = 0; i < this.supportedClients; i++)
            {
                flag = 1 << i;
                targetId = flag & targetsIds;
                if (targetId != 0)
                {
                    if (this.clientsEngineByUserId.TryGetValue((i + 1), out targetEntity))
                    {
                        outgoingBroadcast.Targets.Add(targetEntity);
                    }
                }
            }

            outgoingBroadcast.LoadWith(messageToSend.bodyMessage, 0, messageToSend.MessageBytesSize);
            outgoingMessage.IsBroadcast = true;
            if (!KSPM.Globals.KSPMGlobals.Globals.KSPMServer.outgoingMessagesQueue.EnqueueCommandMessage(ref outgoingMessage))
            {
                KSPM.Globals.KSPMGlobals.Globals.KSPMServer.broadcastMessagesPool.Recycle(outgoingMessage);
                outgoingBroadcast = null;
            }
        }

        /// <summary>
        /// Broadcasts the network entities specified by the user id flag, using the UDP channel.
        /// </summary>
        /// <param name="userIds">Flag with the User's id.</param>
        /// <param name="messageToSend">Message to be send.</param>
        public void UDPSelectiveBroacast(int userIds, Message messageToSend)
        {
            Message outgoingMessage = null;

            NetworkEntity targetEntity;
            int flag = 1;
            int targetId;

            lock (this.clients)
            {

                ///Creating the clients list using the ids stored in the userIds flag.
                for (int i = 0; i < this.supportedClients; i++)
                {
                    flag = 1 << i;
                    targetId = flag & userIds;
                    if (targetId != 0)
                    {
                        if (this.clientsEngineByUserId.TryGetValue((i + 1), out targetEntity))
                        {
                            outgoingMessage = ((ServerSideClient)targetEntity).IOUDPMessagesPool.BorrowMessage;
                            ((RawMessage)outgoingMessage).LoadWith(messageToSend.bodyMessage, 0, messageToSend.MessageBytesSize);
                            outgoingMessage.IsBroadcast = true;
                            if (!((ServerSideClient)targetEntity).outgoingPackets.EnqueueCommandMessage(ref outgoingMessage))
                            {
                                ///If this code is reached means the outgoing queue is full.
                                ((ServerSideClient)targetEntity).IOUDPMessagesPool.Recycle(outgoingMessage);
                            }
                            else
                            {
                                ((ServerSideClient)targetEntity).SendUDPDatagram();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="messageToSend"></param>
        public void UDPSelectiveBroacast(Message messageToSend)
        {
            int targetsIds = 0;
            Message outgoingMessage = null;

            NetworkEntity targetEntity;
            int flag = 1;
            int targetId;
            ///Getting the ids from the message's buffer.
            targetsIds = System.BitConverter.ToInt32(messageToSend.bodyMessage, 14);

            lock (this.clients)
            {

                ///Creating the clients list using the ids stored in the userIds flag.
                for (int i = 0; i < this.supportedClients; i++)
                {
                    flag = 1 << i;
                    targetId = flag & targetsIds;
                    if (targetId != 0)
                    {
                        if (this.clientsEngineByUserId.TryGetValue((i + 1), out targetEntity))
                        {
                            outgoingMessage = ((ServerSideClient)targetEntity).IOUDPMessagesPool.BorrowMessage;
                            ((RawMessage)outgoingMessage).LoadWith(messageToSend.bodyMessage, 0, messageToSend.MessageBytesSize);
                            outgoingMessage.IsBroadcast = true;
                            if (!((ServerSideClient)targetEntity).outgoingPackets.EnqueueCommandMessage(ref outgoingMessage))
                            {
                                ///If this code is reached means the outgoing queue is full.
                                ((ServerSideClient)targetEntity).IOUDPMessagesPool.Recycle(outgoingMessage);
                            }
                            else
                            {
                                ((ServerSideClient)targetEntity).SendUDPDatagram();
                            }
                        }
                    }
                }
            }
        }

        #endregion
    }
}
