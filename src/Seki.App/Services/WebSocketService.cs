using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using NetCoreServer;
using Windows.ApplicationModel.DataTransfer;
using Seki.App.Data.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Seki.App.Services
{
    public class SekiSession : WsSession
    {
        public SekiSession(WsServer server) : base(server) { }

        public override void OnWsConnected(HttpRequest request)
        {
            System.Diagnostics.Debug.WriteLine($"WebSocket session with Id {Id} connected!");
            SendMessage(SocketMessageType.Response, new Response { Content = "Connected" });
            SendMessage(SocketMessageType.Notification, new Notification
            {
                AppName = "Whatsapp",
                Actions = ["Like", "Reply"],
                NotificationContent = "myre ODI va"
            });
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
                SocketMessage message = JsonSerializer.Deserialize<SocketMessage>(jsonMessage, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                HandleMessage(message);
            }
            catch (JsonException)
            {
                SendMessage(SocketMessageType.Response, new Response { Content = "Invalid message format" });
            }

            // If the buffer starts with '!' then disconnect the current session
            if (jsonMessage == "!")
                Close(1000);
        }

        public void HandleMessage(SocketMessage message)
        {
            switch (message.Type)
            {
                case SocketMessageType.Link:
                    // Handle Links
                    break;
                case SocketMessageType.Clipboard:
                    // Handle Clipboard
                    ClipboardMessage clipboardMessage = JsonSerializer.Deserialize<ClipboardMessage>(message.Content);
                    var clipboardData = new DataPackage();
                    clipboardData.SetText(clipboardMessage.Content);
                    Clipboard.SetContent(clipboardData);
                    //SendMessage(SocketMessageType.Clipboard, new ClipboardMessage { Content = clipboardData});
                    break;
                case SocketMessageType.Notification:
                    var notification = JsonSerializer.Deserialize<Notification>(message.Content);
                    // Handle notification
                    System.Diagnostics.Debug.WriteLine($"Received notification: {notification.AppName}, {notification.NotificationContent}");
                    break;
                case SocketMessageType.Permission:
                    // Handle Permission
                    //SendMessage(new Response { Content = "Permission request received" });
                    break;
                case SocketMessageType.Message:
                    // Handle Message
                    //SendMessage(new Response { Content = "Message received" });
                    break;
                case SocketMessageType.Media:
                    // Handle Media Control
                    //SendMessage(new Response { Content = "Media control received" });
                    break;
                default:
                    //SendMessage(new Response { Content = "Unknown message type" });
                    break;
            }
        }

        public void SendMessage(string type, object content)
        {
            string jsonResponse = JsonSerializer.Serialize(content);
            System.Diagnostics.Debug.WriteLine($"{jsonResponse}");
            ((SekiServer)Server).MulticastText(jsonResponse);
        }

        protected override void OnError(SocketError error)
        {
            System.Diagnostics.Debug.WriteLine($"WebSocket session caught an error with code {error}");
        }
    }

    public class SekiServer : WsServer
    {
        public SekiServer(IPAddress address, int port) : base(address, port) { }

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
