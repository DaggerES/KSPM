
namespace KSPM.Network.Common
{
    /// <summary>
    /// Interface to be used when you want to receive asynchronous packets, but you already are implementing the AsyncReceiver interface.
    /// </summary>
    public interface IAsyncTCPReceiver
    {
        /// <summary>
        /// Method used to receive Messages through the TCP socket.
        /// </summary>
        /// <param name="result">Holds a reference to this object.</param>
        void AsyncTCPReceiver(System.IAsyncResult result);
    }
}
