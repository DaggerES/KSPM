using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net;
using System.Net.Sockets;

/*
 * Server is handled as an entity, because the cliente itself would not have any idea to whom is sending the package.
 */

namespace KSPM.Network.Server
{
    /// <summary>
    /// Represents an abstract interpretation of the server and the clients, having only the essential information to know what kind of Entity represents, either the server or the client.
    /// </summary>
    public abstract class NetworkEntity
    {
        /// <summary>
        /// This represents the IP address and the port of the entity.
        /// </summary>
        protected IPEndPoint networkEndPoint;

        /// <summary>
        /// The GUID for the entity
        /// </summary>
        protected Guid id;

        /// <summary>
        /// This string will be the identifier by each entity
        /// </summary>
        protected string hashIdentifier;

        public NetworkEntity(string hashedIdentifier)
        {
            this.hashIdentifier = hashedIdentifier;
            this.networkEndPoint = null;
        }
    }
}
