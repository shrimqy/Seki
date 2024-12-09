using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;


namespace Sefirah.App.Utils;

public static class NetworkHelper
{
    public static string GetLocalIPAddress()
    {
        foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            // Filter out virtual adapters and ensure the network interface is active
            if ((ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet) &&
                ni.OperationalStatus == OperationalStatus.Up &&
                !IsVirtualAdapter(ni))
            {
                // Get all unicast IPs (IPv4) for the selected interface
                foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip.Address))
                    {
                        // Return the first valid IPv4 address found
                        return ip.Address.ToString();
                    }
                }
            }
        }

        throw new Exception("No network adapters with a valid IPv4 address found!");
    }

    private static bool IsVirtualAdapter(NetworkInterface ni)
    {
        // Filter out adapters with "vEthernet" or other virtual identifiers in their name
        return ni.Name.Contains("vEthernet", StringComparison.OrdinalIgnoreCase) ||
               ni.Description.Contains("Virtual", StringComparison.OrdinalIgnoreCase) ||
               ni.Description.Contains("Hyper-V", StringComparison.OrdinalIgnoreCase) ||
               ni.Description.Contains("VMware", StringComparison.OrdinalIgnoreCase);
    }
}