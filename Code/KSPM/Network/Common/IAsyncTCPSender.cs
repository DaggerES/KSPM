using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KSPM.Network.Common
{
    /// <summary>
    /// Interface to be used when you want to send asynchronous packets, but you already are implementing the AsyncSender interface.
    /// </summary>
    interface IAsyncTCPSender
    {
        /// <summary>
        /// Method used to send Messages through the TCP socket.
        /// </summary>
        /// <param name="result">Holds a reference to this object.</param>
        void AsyncTCPSender(System.IAsyncResult resul);
    }
}
