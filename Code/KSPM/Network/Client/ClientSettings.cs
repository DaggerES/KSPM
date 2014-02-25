
namespace KSPM.Network.Client
{
    public class ClientSettings : KSPM.Network.Common.AbstractSettings
    {
        public static readonly int ClientBufferSize = 1024 * 1;

        /// <summary>
        /// Sets in which port the client will be working with TCP packets.
        /// </summary>
        public static readonly int ClientTCPPort = 4800;

        /// <summary>
        /// Sets in which port the client will be working with UDP packets.
        /// </summary>
        public static readonly int ClientUDPPort = 4801;
    }
}
