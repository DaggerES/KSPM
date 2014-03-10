using KSPM.Network.Server.UserManagement.Filters;

namespace KSPM.Network.Server.UserManagement
{
    /// <summary>
    /// Base of every user management system.
    /// </summary>
    public abstract class UserManagementSystem
    {
        /// <summary>
        /// Filter to be used in the Query method.
        /// </summary>
        protected Filter filter;

        /// <summary>
        /// Initializes the filter to NoneFilter.
        /// </summary>
        public UserManagementSystem()
        {
            this.filter = NoneFilter.Filter;
        }

        /// <summary>
        /// Make a query into the sytem and tells if the given network entity matches with the filter.
        /// </summary>
        /// <param name="entityToValidate"></param>
        /// <returns></returns>
        public abstract bool Query(ref Network.Common.NetworkEntity entityToValidate);

        /// <summary>
        /// Replaces the current filter with the new one.
        /// </summary>
        /// <param name="newFilter"></param>
        public void SetFilter(ref Filter newFilter)
        {
            if( newFilter != null )
                this.filter = newFilter;
        }
    }
}
