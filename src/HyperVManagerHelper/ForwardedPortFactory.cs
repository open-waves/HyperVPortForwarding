using System;
using System.Linq;
using System.Net;

namespace MakingWaves.Tools.HyperVManagerHelper
{
    public class ForwardedPortFactory
    {
        public ForwardedPort Create(string line)
        {
            var array = line.Split(' ').Where(s => !String.IsNullOrEmpty(s) && s != "*").ToArray();

            var forwardedPort = new ForwardedPort
            {
                HostPort = int.Parse(array[0]),
                VmIpAddress = IPAddress.Parse(array[1]),
                VmPort = int.Parse(array[2]),
            };

            return forwardedPort;
        } 
    }
}