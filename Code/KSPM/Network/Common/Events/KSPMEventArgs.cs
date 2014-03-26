using KSPM.Network.Common;

namespace KSPM.Network.Common.Events
{
    public delegate void UserConnectedEventHandler( object sender, KSPMEventArgs e );
    public delegate void UserDisconnectedEventHandler( object sender, KSPMEventArgs e );
    
    public class KSPMEventArgs : System.EventArgs
    {
        public NetworkBaseCollection target;
    }
}
