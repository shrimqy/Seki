using System;
using MeaMod.DNS.Multicast;

namespace Seki.App.Services
{
    public class MdnsService
    {
        private ServiceProfile? _serviceProfile;
        private ServiceDiscovery? _serviceDiscovery;

        public async Task AdvertiseServiceAsync()
        {

            var currentUserInfo = new CurrentUserInformation();
            var (username, _) = await currentUserInfo.GetCurrentUserInfoAsync();

            // Create the service profile
            _serviceProfile = new ServiceProfile(username, "_foo._tcp", 1024);

            _serviceProfile.AddProperty("ipAddress", NetworkHelper.GetLocalIPAddress());

            // Initialize the service discovery
            _serviceDiscovery = new ServiceDiscovery();

            // Advertise the service
            _serviceDiscovery.Advertise(_serviceProfile);

            System.Diagnostics.Debug.WriteLine($"advertising service for {_serviceProfile.InstanceName}");
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
    }
}
