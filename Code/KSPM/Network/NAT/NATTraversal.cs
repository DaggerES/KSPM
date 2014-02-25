using KSPM.Network.Common;
using System.Net.Sockets;

namespace KSPM.Network.NAT
{
    public abstract class NATTraversal
    {

        public enum NATStatus : byte { Connecting, Connected, Error };

        /// <summary>
        /// Tries to connect to the specified ip using the given port.
        /// </summary>
        /// <param name="client">Socket reference used to work.</param>
        /// <param name="ip">Remote ip.</param>
        /// <param name="port">Remote port.</param>
        /// <returns></returns>
        public abstract Error.ErrorType Punch( ref Socket client, string ip, int port);

        protected NATStatus currentStatus;

        public NATStatus Status
        {
            get
            {
                return this.currentStatus;
            }
        }

    }
}
