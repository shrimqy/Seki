using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Net.NetworkInformation;


namespace Seki.App.Utils
{
    public static class NetworkHelper
    {
        public static string GetLocalIPAddress()
        {
            var candidateIPs = GetPrioritizedNetworkInterfaces()
                .SelectMany(ni => ni.GetIPProperties().UnicastAddresses
                    .Where(ip => ip.Address.AddressFamily == AddressFamily.InterNetwork &&
                                 !IPAddress.IsLoopback(ip.Address)))
                .Select(ip => ip.Address)
                .ToList();

            if (!candidateIPs.Any())
            {
                throw new Exception("No network adapters with a valid IPv4 address found!");
            }

            // Prefer non-link-local addresses (those not starting with 169.254)
            var nonLinkLocalIP = candidateIPs.FirstOrDefault(ip => !ip.ToString().StartsWith("169.254"));
            if (nonLinkLocalIP != null)
            {
                return nonLinkLocalIP.ToString();
            }

            // If all IPs are link-local, return the first one
            return candidateIPs.First().ToString();
        }

        private static IEnumerable<NetworkInterface> GetPrioritizedNetworkInterfaces()
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up &&
                             (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                              ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet))
                .OrderByDescending(ni => GetInterfacePriority(ni));
        }

        private static int GetInterfacePriority(NetworkInterface ni)
        {
            // Prioritize physical adapters over virtual ones
            if (ni.Description.IndexOf("virtual", StringComparison.OrdinalIgnoreCase) >= 0 ||
                ni.Description.IndexOf("vEthernet", StringComparison.OrdinalIgnoreCase) >= 0 ||
                ni.Description.IndexOf("Hyper-V", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return 0; // Lowest priority for virtual adapters
            }

            // Higher priority for Ethernet
            if (ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
            {
                return 2;
            }

            // Medium priority for Wi-Fi
            if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
            {
                return 1;
            }

            return 0; // Default low priority
        }

        // Optional: Method to get all valid local IP addresses
        public static List<string> GetAllLocalIPAddresses()
        {
            return GetPrioritizedNetworkInterfaces()
                .SelectMany(ni => ni.GetIPProperties().UnicastAddresses
                    .Where(ip => ip.Address.AddressFamily == AddressFamily.InterNetwork &&
                                 !IPAddress.IsLoopback(ip.Address)))
                .Select(ip => ip.Address.ToString())
                .ToList();
        }
    }
}