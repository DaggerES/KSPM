namespace KSPM.Network.Server.UserManagement
{
    /// <summary>
    /// Class that will provide the basic account management.
    /// </summary>
    public class AccountManager : UserManagementSystem
    {
        public override bool Query(ref Common.NetworkEntity entityToValidate)
        {
            return true;
        }
    }
}
