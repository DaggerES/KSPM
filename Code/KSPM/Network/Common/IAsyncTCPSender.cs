namespace KSPM.Network.Common
{
    /// <summary>
    /// Interface to be used when you want to send asynchronous packets, but you already are implementing the AsyncSender interface.
    /// </summary>
    public interface IAsyncTCPSender
    {
        /// <summary>
        /// Method used to send Messages through the TCP socket.
        /// </summary>
        /// <param name="result">Holds a reference to this object.</param>
        void AsyncTCPSender(System.IAsyncResult result);
    }
}
