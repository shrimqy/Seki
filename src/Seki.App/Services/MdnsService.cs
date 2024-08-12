using System;
using MeaMod.DNS.Multicast;

namespace Seki.App.Services
{
    public class MdnsService
    {
        public void AdvertiseService()
        {
            string computerName = Environment.MachineName;
            System.Diagnostics.Debug.WriteLine(computerName);
            var service = new ServiceProfile(computerName, "_foo._tcp", 1024);
            var sd = new ServiceDiscovery();
            sd.Advertise(service);
        }
    }
}
