using System.Net;

namespace MakingWaves.Tools.HyperVPortForwarding
{
    public class ForwardedPort
    {
        public int HostPort { get; set; }
        public IPAddress VmIpAddress { get; set; }
        public int VmPort { get; set; }
    }
}