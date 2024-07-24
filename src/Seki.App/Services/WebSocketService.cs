using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using NetCoreServer;
using Windows.ApplicationModel.DataTransfer;
using Seki.App.Data.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Seki.App.Helpers;

namespace Seki.App.Services
{
    public class SekiSession(WsServer server) : WsSession(server)
    {
        public override void OnWsConnected(HttpRequest request)
        {
            System.Diagnostics.Debug.WriteLine($"WebSocket session with Id {Id} connected!");
            //var message = new NotificationMessage
            //{
            //    Type = SocketMessageType.Notification,
            //    AppName = "Whatsapp",
            //    Header = "Jerin",
            //    Content = "content",
            //    Actions = { new NotificationAction { ActionId = "0", Label = "Reply" } }
            //};
            
            //SendMessage(message);
            SendMessage(new Response { ResType = "Status", Content = "Connected" });
        }
            
        public override void OnWsDisconnected()
        {
            System.Diagnostics.Debug.WriteLine($"WebSocket session with Id {Id} disconnected!");
        }

        public override void OnWsReceived(byte[] buffer, long offset, long size)
        {
            string jsonMessage = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
            System.Diagnostics.Debug.WriteLine(jsonMessage);

            try
            {
                SocketMessage message = SocketMessageSerializer.DeserializeMessage(jsonMessage);
                HandleMessage(message);
            }
            catch (JsonException ex)
            {
                System.Diagnostics.Debug.WriteLine($"JSON deserialization error: {ex.Message}");
                SendMessage(new Response { Content = "Invalid message format" });
            }

            if (jsonMessage == "!")
                Close(1000);
        }

        public void HandleMessage(SocketMessage message)
        {
            switch (message)
            {
                case ClipboardMessage clipboardMessage:
                    var clipboardData = new DataPackage();
                    clipboardData.SetText(clipboardMessage.Content);
                    Clipboard.SetContent(clipboardData);
                    break;
                case NotificationMessage notificationMessage:
                    // Handle notification
                    System.Diagnostics.Debug.WriteLine($"Received notification: {notificationMessage.AppName}, {notificationMessage.Content}");
                    break;
                // Add more cases for other message types
                default:
                    SendMessage(new Response { Content = "Unknown message type" });
                    break;
            }
        }
        public void SendMessage(object content)
        {
            string jsonResponse = SocketMessageSerializer.Serialize(content);
            System.Diagnostics.Debug.WriteLine($"{jsonResponse}");
            ((SekiServer)Server).MulticastText(jsonResponse);
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
        private ClipboardService _clipboardService;
        private bool _isRunning;

        // Private constructor for singleton pattern
        private WebSocketService()
        {
            string ipAddress = GetLocalIPAddress();
            _webSocketServer = new SekiServer(IPAddress.Parse(ipAddress), 8080);
            _clipboardService = new ClipboardService();
            _clipboardService.ClipboardContentChanged += OnClipboardContentChanged;
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

        private void OnClipboardContentChanged(object sender, string e)
        {
            System.Diagnostics.Debug.WriteLine("Clipboard changed: " + e);
            var clipboardMessage = new ClipboardMessage { Content = e };
            _webSocketServer.MulticastText(JsonSerializer.Serialize(clipboardMessage));
        }

        private static string GetLocalIPAddress()
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
