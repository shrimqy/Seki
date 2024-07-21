using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NetCoreServer;

namespace Seki.App.Services
{
    public class SekiSession(WsServer server) : WsSession(server)
    {
        public override void OnWsConnected(HttpRequest request)
        {
            System.Diagnostics.Debug.WriteLine($"WebSocket session with Id {Id} connected!");

            // Send invite message
            string message = "Hello from Seki WebSocket! Please send a message or '!' to disconnect the client!";
            SendTextAsync(message);
        }

        public override void OnWsDisconnected()
        {
            System.Diagnostics.Debug.WriteLine($"WebSocket session with Id {Id} disconnected!");
        }

        public override void OnWsReceived(byte[] buffer, long offset, long size)
        {
            string message = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
            System.Diagnostics.Debug.WriteLine("Incoming: " + message);

            // Multicast message to all connected sessions
            ((WsServer)Server).MulticastText(message);

            // If the buffer starts with '!' then disconnect the current session
            if (message == "!")
                Close(1000);
        }

        protected override void OnError(SocketError error)
        {
            System.Diagnostics.Debug.WriteLine($"WebSocket session caught an error with code {error}");
        }
    }

    public class SekiServer(IPAddress address, int port) : WsServer(address, port)
    {
        protected override TcpSession CreateSession() { return new SekiSession(this); }

        protected override void OnError(SocketError error)
        {
            System.Diagnostics.Debug.WriteLine($"WebSocket server caught an error with code {error}");
        }
    }

    public class WebSocketService
    {
        private static WebSocketService _instance;
        private SekiServer _webSocketServer;
        private bool _isRunning;

        // Private constructor for singleton pattern
        private WebSocketService()
        {
            string ipAddress = GetLocalIPAddress();
            _webSocketServer = new SekiServer(IPAddress.Parse(ipAddress), 8080);
        }

        // Singleton instance
        public static WebSocketService Instance => _instance ??= new WebSocketService();

        public void Start()
        {
            if (!_isRunning)
            {
                _webSocketServer.Start();
                _isRunning = true;
                System.Diagnostics.Debug.WriteLine($"WebSocket server started at ws://{_webSocketServer.Address}:{_webSocketServer.Port}/SekiService");
            }
        }

        public void Stop()
        {
            if (_isRunning)
            {
                _webSocketServer.Stop();
                _isRunning = false;
                System.Diagnostics.Debug.WriteLine("WebSocket server stopped.");
            }
        }

        public bool IsRunning => _isRunning;

        static private string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }
    }
}
