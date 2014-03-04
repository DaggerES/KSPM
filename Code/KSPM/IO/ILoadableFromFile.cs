using KSPM.Network.Common;

namespace KSPM.IO
{
    interface ILoadableFromFile
    {
        /// <summary>
        /// Reads a file and tries to inflate a object.
        /// </summary>
        /// <param name="fileName">The filename who contains the filter definition.</param>
        /// <returns></returns>
        Error.ErrorType InflateFromFile(string fileName);
    }
}
