using System.Net.Sockets;
using KSPM.Network.Common;
using KSPM.Diagnostics;

namespace KSPM.Network.NAT
{
    public class NATNone : NATTraversal
    {
        public NATNone()
            : base()
        {
        }

        public override Error.ErrorType Punch(ref Socket client, string ip, int port)
        {
            Error.ErrorType error = Error.ErrorType.Ok;
            try
            {
				this.currentStatus = NATStatus.Connecting;
				///Due to an specific Berkeley socket description which regards that a connectionless socket does not support to call sendTo on an already connected socket.
				/// So to avoid that Socket errorcode (10056) the Socket.Connect method is only called when it is not using a connectionless protocol.
				if( client.ProtocolType != ProtocolType.Udp )
				{
                	client.Connect(ip, port);
				}
                this.currentStatus = NATStatus.Connected;
            }
            catch (System.Exception ex)
            {
                if (ex.GetType().Equals(typeof(SocketException)))
                {
                    if (((SocketException)ex).ErrorCode == 10048)
                    {
                        error = Error.ErrorType.NATAdrressInUse;
                        this.currentStatus = NATStatus.AddresInUse;
                    }
                }
                else
                {
                    //KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo(ex.Message);
                    error = Error.ErrorType.ClientUnableToConnect;
                    this.currentStatus = NATStatus.Error;
                }
                if( client != null )
                    client.Close();
            }
            return error;
        }
    }
}
