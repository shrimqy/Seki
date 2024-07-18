using System;
using MeaMod.DNS.Multicast;

namespace Seki.App.Services
{
    public class MdnsService
    {
        public void AdvertiseService()
        {
            var service = new ServiceProfile("Sekia", "_foo._tcp", 1024);
            var sd = new ServiceDiscovery();
            sd.Advertise(service);
            Console.WriteLine("Service advertised.");
        }
    }
}
