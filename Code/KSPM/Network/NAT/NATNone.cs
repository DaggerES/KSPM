using System.Net.Sockets;
using KSPM.Network.Common;

namespace KSPM.Network.NAT
{
    public class NATNone : NATTraversal
    {
        public override Error.ErrorType Punch(ref Socket client, string ip, int port)
        {
            Error.ErrorType error = Error.ErrorType.Ok;
            try
            {
                this.currentStatus = NATStatus.Connecting;
                client.Connect(ip, port);
                this.currentStatus = NATStatus.Connected;
            }
            catch (System.Exception)
            {
                error = Error.ErrorType.ClientUnableToConnect;
                this.currentStatus = NATStatus.Error;
            }
            return error;
        }
    }
}
