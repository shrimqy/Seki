using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using NetCoreServer;
using Windows.ApplicationModel.DataTransfer;

namespace Seki.App.Services
{
    public class MessageType
    {
        public const string Error = "error";
        public const string Link = "link";
        public const string Clipboard = "clipboard";
        public const string Response = "response";
    }
    public class Message
    {
        public string Type { get; set; }
        public string Content { get; set; }
    }

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
            string jsonMessage = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
            System.Diagnostics.Debug.WriteLine("Incoming: " + jsonMessage);
            try
            {
                Message message = JsonSerializer.Deserialize<Message>(jsonMessage);
                HandleMessage(message);
            }
            catch (JsonException)
            {
                SendMessage(MessageType.Error, "Invalid message format");
            }

            // If the buffer starts with '!' then disconnect the current session
            if (jsonMessage == "!")
                Close(1000);
        }

        public void HandleMessage(Message message)
        {
            switch (message.Type)
            {
                case MessageType.Link:
                    // Handle Links
                    SendMessage(MessageType.Response, "Success");
                    break;
                case MessageType.Clipboard:
                    // Handle ClipboardText
                    var ClipBoard = new DataPackage();
                    ClipBoard.SetText(message.Content);
                    Clipboard.SetContent(ClipBoard); 
                    SendMessage(MessageType.Response, "Success");
                    break;
                default:
                    SendMessage(MessageType.Error, "Unknown message type");
                    break;
            }
        }
        public void SendMessage(string type, string content)
        {
            Message response = new() { Type = type, Content = content };
            string jsonResponse = JsonSerializer.Serialize(response);
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
            _webSocketServer.MulticastText(JsonSerializer.Serialize(new Message { Type = MessageType.Clipboard, Content = e }));
        }

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
