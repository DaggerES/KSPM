﻿using System.Collections.Generic;
using KSPM.Network.Common;

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
        /// Dictionary to privide a fast way to search among the server's clients.
        /// </summary>
        protected Dictionary<System.Guid, NetworkEntity > clientsEngine;

        public ClientsHandler()
        {
            this.clients = new List<NetworkEntity>();
            this.clientsEngine = new Dictionary<System.Guid,NetworkEntity>();
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
                    referredEntity.Release();
                    this.clients.Remove(referredEntity);
                    this.clientsEngine.Remove(referredEntity.Id);
                }
            }
        }

        /// <summary>
        /// Clears the structures and calls the ReleaseMethod of each NetworkEntity reference.
        /// </summary>
        public void Clear()
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
        }

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
    }
}