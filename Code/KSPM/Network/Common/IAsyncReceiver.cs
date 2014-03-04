namespace KSPM.Network.Common
{
    /// <summary>
    /// Interface to be implemented to each class who needs to perform an async receiving.
    /// </summary>
    public interface IAsyncReceiver
    {
        /// <summary>
        /// Receives data in an async way.
        /// </summary>
        /// <param name="result"></param>
        void AsyncReceiverCallback(System.IAsyncResult result);
    }
}
