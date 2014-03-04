namespace KSPM.Network.Server.UserManagement.Filters
{
    /// <summary>
    /// Class with no filter.
    /// </summary>
    public class NoneFilter : Filter
    {
        /// <summary>
        /// Static reference to an NoneFilter object.
        /// </summary>
        public static NoneFilter Filter = new NoneFilter();

        /// <summary>
        /// Protected constructor to avoid user create more than one instance of this class.
        /// </summary>
        protected NoneFilter() { }

        public override bool Match( FilterMode filteringMode, ref KSPM.Network.Common.NetworkEntity entityToBeTested)
        {
            return true;
        }
    }
}
