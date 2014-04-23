namespace KSPM.Network.Server.UserManagement
{
    /// <summary>
    /// Class that will provide the basic account management.
    /// </summary>
    public class AccountManager : UserManagementSystem
    {
        public override bool Query(Common.NetworkEntity entityToValidate)
        {
            ServerSideClient clientReference = (ServerSideClient)entityToValidate;
            return true;
        }
    }
}
