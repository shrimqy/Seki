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
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                // Filter by network interface type (Ethernet or Wireless) and operational status
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    if (ni.OperationalStatus == OperationalStatus.Up)
                    {
                        // Get all unicast IPs (IPv4)
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
            }

            throw new Exception("No network adapters with a valid IPv4 address found!");
        }
    }
}