using System.Net;

namespace MakingWaves.Tools.HyperVManagerHelper
{
    public class ForwardedPort
    {
        public int HostPort { get; set; }
        public IPAddress VmIpAddress { get; set; }
        public int VmPort { get; set; }
    }
}