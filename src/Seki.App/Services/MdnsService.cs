using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using MeaMod.DNS.Multicast;
using Seki.App.Utils;


namespace Seki.App.Services
{
    public class MdnsService
    {
        private ServiceProfile? _serviceProfile;
        private ServiceDiscovery? _serviceDiscovery;

        public async Task AdvertiseServiceAsync(bool isPairing)
        {

            var currentUserInfo = new CurrentUserInformation();
            var (username, _) = await currentUserInfo.GetCurrentUserInfoAsync();

            // Create the service profile
            _serviceProfile = new ServiceProfile(username, "_foo._tcp", 1024);

            _serviceProfile.AddProperty("ipAddress", NetworkHelper.GetLocalIPAddress());
            _serviceProfile.AddProperty("pairingCode", GenerateRandomPairingCode());


            // Initialize the service discovery
            _serviceDiscovery = new ServiceDiscovery();

            // Advertise the service
            _serviceDiscovery.Advertise(_serviceProfile);

            
            Debug.WriteLine($"advertising service for {_serviceProfile.InstanceName}");
        }

        public void UnAdvertiseService()
        {
            if (_serviceDiscovery != null && _serviceProfile != null)
            {
                System.Diagnostics.Debug.WriteLine($"Un-advertising service for {_serviceProfile.InstanceName}");

                // Unadvertise the service
                _serviceDiscovery.Unadvertise(_serviceProfile);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Service not advertised or already unadvertised.");
            }
        }

        public void StartDiscovery()
        {
            if (_serviceDiscovery != null)
            {
                _serviceDiscovery.ServiceInstanceDiscovered += (sender, args) =>
                {
                    Debug.WriteLine($"Service found: {args.ServiceInstanceName}");
                };

                _serviceDiscovery.ServiceInstanceShutdown += (sender, args) =>
                {
                    Debug.WriteLine($"Service lost: {args.ServiceInstanceName}");
                };
            }
        }

        // Utility to generate 6-digit random number
        public static string GenerateRandomPairingCode()
        {
            Random random = new();
            return random.Next(100000, 999999).ToString();
        }
    }
}