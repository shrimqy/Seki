using MeaMod.DNS.Multicast;
using NetCoreServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Seki.App.Services
{
    class UdpDiscoveryService
    {
        private UdpServer? server;
        private string? pairingCode;

        class MulticastServer : UdpServer
        {
            public MulticastServer(IPAddress address, int port) : base(address, port) { }

            protected override void OnError(SocketError error)
            {
                Console.WriteLine($"Multicast UDP server caught an error with code {error}");
            }
        }

        public void StartUdpServer()
        {
            // UDP multicast address
            string multicastAddress = "239.255.0.1";

            // UDP multicast port
            int multicastPort = 3334;

            Console.WriteLine($"UDP multicast address: {multicastAddress}");
            Console.WriteLine($"UDP multicast port: {multicastPort}");

            Console.WriteLine();

            // Create a new UDP multicast server
            server = new MulticastServer(IPAddress.Any, 0);


            server.Start(multicastAddress, multicastPort);
            pairingCode = GeneratePairingCode();
            string deviceName = "MyDevice";  // Customize this as per your device name
            string ipAddress = "192.168.1.100";  // Customize this as per your local IP (you can use your `GetLocalIPAddress()` method here)
            int port = 5149;  // Example port for WebSocket server
            // Create a message with device details and the pairing code
            string message = $"Device: {deviceName}, IP: {ipAddress}, Port: {port}, Pairing Code: {pairingCode}";
            while (true)
            {
                server.Send(message);
            }
        }

        public void StopUdpServer()
        {
            if (server != null)
            {
                server.Stop();
            }
        }

        // Generate a new random 6-digit pairing code
        private string GeneratePairingCode()
        {
            Random random = new();
            return random.Next(100000, 999999).ToString();
        }
    }
}
