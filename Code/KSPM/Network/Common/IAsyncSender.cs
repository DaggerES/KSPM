namespace KSPM.Network.Common
{
    /// <summary>
    /// Interface to be implemented by each class who needs to perform an async sending.
    /// </summary>
    public interface IAsyncSender
    {
        /// <summary>
        /// Sends data in an async way.
        /// </summary>
        /// <param name="result"></param>
        void AsyncSenderCallback(System.IAsyncResult result);
    }
}
