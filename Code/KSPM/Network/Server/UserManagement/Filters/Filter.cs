using KSPM.Network.Common;

namespace KSPM.Network.Server.UserManagement.Filters
{
    /// <summary>
    /// Filter to be used by the UserManagementSystem clases. If you want to implement a new filter you must inherit from this class.
    /// </summary>
    public abstract class Filter
    {
        public enum FilterMode : byte { Whitelist = 0, Blacklist };

        /// <summary>
        /// Test the given NetworkEntity and applies the filter on it.
        /// </summary>
        /// <param name="entityToBeTested">Reference to a NetworkEntity to be tested.</param>
        /// <returns>True if the NetworkEntity maches, false otherwise.</returns>
        public abstract bool Match( FilterMode filteringMode, ref NetworkEntity entityToBeTested);
    }
}
