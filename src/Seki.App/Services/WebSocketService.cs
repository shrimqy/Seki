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
using Windows.UI.Notifications;
using System.Linq;
using Microsoft.Windows.AppNotifications.Builder;
using Microsoft.Windows.AppNotifications;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.Storage;
using Windows.Services.Maps;


namespace Seki.App.Services
{
    public class SekiSession(WsServer server) : WsSession(server)
    {

        public Devices? Device { get; private set; }

        public override void OnWsConnected(HttpRequest request)
        {
            System.Diagnostics.Debug.WriteLine($"WebSocket session with Id {Id} connected!");
            SendMessage(new Response { ResType = "Status", Content = "Connected" });
        }
            
        public override void OnWsDisconnected()
        {
            System.Diagnostics.Debug.WriteLine($"WebSocket session with Id {Id} disconnected!");
        }

        public override void OnWsReceived(byte[] buffer, long offset, long size)
        {
            string jsonMessage = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
            System.Diagnostics.Debug.WriteLine("json message: " + jsonMessage);

            try
            {
                SocketMessage message = SocketMessageSerializer.DeserializeMessage(jsonMessage);
                HandleMessageReceived(message);
            }
            catch (JsonException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                System.Diagnostics.Debug.WriteLine($"JSON deserialization error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                SendMessage(new Response { Content = "Invalid message format: " + ex.Message });
            }
        }

        public void HandleMessage(SocketMessage message)
        {
            switch (message.Type)
            {
                case SocketMessageType.Clipboard:
                    if (message is ClipboardMessage clipboardMessage)
                    {
                        var clipboardData = new DataPackage();
                        clipboardData.SetText(clipboardMessage.Content);
                        Clipboard.SetContent(clipboardData);
                    }
                    break;
                case SocketMessageType.Notification:
                    if (message is NotificationMessage notificationMessage)
                    {
                        // Handle notification
                        //System.Diagnostics.Debug.WriteLine($"Received notification: {notificationMessage.AppName}, {notificationMessage.Content}");
                        // You can add more specific handling for the notification here
                    }
                    break;
                case SocketMessageType.Response:
                    if (message is Response responseMessage)
                    {
                        // Handle response
                        System.Diagnostics.Debug.WriteLine($"Received response: {responseMessage.ResType}, {responseMessage.Content}");
                    }
                    break;
                case SocketMessageType.DeviceInfo:
                    if (message is DeviceInfo deviceInfo)
                    {
                        // Create and store device information
                        Device = new Devices
                        {
                            Id = deviceInfo.DeviceId,
                            Name = deviceInfo.DeviceName,
                            LastConnected = DateTime.Now
                        };
                    }
                    break;
                default:
                    SendMessage(new Response { Content = "Unknown message type" });
                    break;
            }
        }

        public void HandleMessageReceived(SocketMessage message)
        {
            switch (message.Type)
            {
                case SocketMessageType.Clipboard:
                    if (message is ClipboardMessage clipboardMessage)
                    {
                        var clipboardData = new DataPackage();
                        clipboardData.SetText(clipboardMessage.Content);
                        Clipboard.SetContent(clipboardData);
                    }
                    break;
                case SocketMessageType.Notification:
                    if (message is NotificationMessage notificationMessage)
                    {
                        // Handle notification
                        ShowDesktopNotification(notificationMessage);
                        System.Diagnostics.Debug.WriteLine($"Received notification: {notificationMessage}");
                        // You can add more specific handling for the notification here
                    }
                    break;
                case SocketMessageType.Response:
                    if (message is Response responseMessage)
                    {
                        // Handle response
                        System.Diagnostics.Debug.WriteLine($"Received response: {responseMessage.ResType}, {responseMessage.Content}");
                    }
                    break;
                case SocketMessageType.DeviceInfo:
                    if (message is DeviceInfo deviceInfo)
                    {
                        // Handle DeviceInfo
                        System.Diagnostics.Debug.WriteLine($"Received response: {deviceInfo.DeviceName}");
                    }
                    break;
                default:
                    System.Diagnostics.Debug.WriteLine($"Unknown message type: {message.Type}");
                    SendMessage(new Response { Content = "Unknown message type" });
                    break;
            }
        }

        private static List<AppNotification> notificationHistory = new List<AppNotification>();

        private static Stream Base64ToStream(string base64)
        {
            byte[] bytes = Convert.FromBase64String(base64);
            return new MemoryStream(bytes);
        }
        private static async void ShowDesktopNotification(NotificationMessage notificationMessage)
        {
            if (notificationMessage.Title != null && notificationMessage.NotificationType == "NEW")
            {
                string tag = $"{notificationMessage.Tag}";
                string group = $"{notificationMessage.GroupKey}";
                var builder = new AppNotificationBuilder()
                    .AddText(notificationMessage.AppName, new AppNotificationTextProperties().SetMaxLines(1))
                    .AddText(notificationMessage.Title)
                    .AddText(notificationMessage.Text)
                    //.SetTag(tag)
                    .SetGroup(group);

                // Add large icon
                if (!string.IsNullOrEmpty(notificationMessage.LargeIcon))
                {
                    using (var stream = Base64ToStream(notificationMessage.LargeIcon))
                    {
                        var randomAccessStream = await ConvertToRandomAccessStreamAsync(stream);
                        builder.SetAppLogoOverride(new Uri("ms-appdata:///local/largeicon.png"), AppNotificationImageCrop.Circle);
                        await SaveStreamToFileAsync(randomAccessStream, "largeicon.png");
                    }
                }
                else if (!string.IsNullOrEmpty(notificationMessage.AppIcon))
                {
                    using (var stream = Base64ToStream(notificationMessage.AppIcon))
                    {
                        var randomAccessStream = await ConvertToRandomAccessStreamAsync(stream);
                        builder.SetAppLogoOverride(new Uri("ms-appdata:///local/appicon.png"), AppNotificationImageCrop.Circle);
                        await SaveStreamToFileAsync(randomAccessStream, "appicon.png");
                    }
                }

                // Add big picture
                //if (!string.IsNullOrEmpty(notificationMessage.BigPicture))
                //{
                //    using (var stream = Base64ToStream(notificationMessage.BigPicture))
                //    {
                //        var randomAccessStream = await ConvertToRandomAccessStreamAsync(stream);
                //        builder.AddImage(new Uri("ms-appdata:///local/bigpicture.png"));
                //        await SaveStreamToFileAsync(randomAccessStream, "bigpicture.png");
                //    }
                //}

                var appNotification = builder.BuildNotification();

                appNotification.ExpiresOnReboot = true;
                AppNotificationManager.Default.Show(appNotification);
                notificationHistory.Add(appNotification);
            }
            System.Diagnostics.Debug.WriteLine($"Showed or updated notification: {notificationMessage}");
        }

        private static async Task<IRandomAccessStream> ConvertToRandomAccessStreamAsync(Stream stream)
        {
            var randomAccessStream = new InMemoryRandomAccessStream();
            var outputStream = randomAccessStream.GetOutputStreamAt(0);
            await RandomAccessStream.CopyAsync(stream.AsInputStream(), outputStream);
            await outputStream.FlushAsync();
            return randomAccessStream;
        }

        private static async Task SaveStreamToFileAsync(IRandomAccessStream stream, string fileName)
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFile file = await localFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
            {
                await RandomAccessStream.CopyAsync(stream, fileStream);
                await fileStream.FlushAsync();
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
        private Dictionary<string, DeviceInfo> _connectedDevices = new Dictionary<string, DeviceInfo>();

        protected override TcpSession CreateSession() { return new SekiSession(this); }

        protected override void OnError(SocketError error)
        {
            System.Diagnostics.Debug.WriteLine($"WebSocket server caught an error with code {error}");
        }

        protected override void OnConnected(TcpSession session)
        {
            base.OnConnected(session);
        }

    }

    public class WebSocketService
    {
        private static WebSocketService? _instance;
        private SekiServer _webSocketServer;
        private ClipboardService _clipboardService;
        private bool _isRunning;

        // Private constructor for singleton pattern
        private WebSocketService()
        {
            string ipAddress = GetLocalIPAddress();
            _webSocketServer = new SekiServer(IPAddress.Parse(ipAddress), 5149);
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
                System.Diagnostics.Debug.WriteLine($"WebSocket server started at ws://{_webSocketServer.Address}:{_webSocketServer.Port}");
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

        private void OnClipboardContentChanged(object? sender, string? content)
        {
            // Log detailed information for debugging
            System.Diagnostics.Debug.WriteLine($"Clipboard: {content}");
            if (content != null)
            {
                var clipboardMessage = new ClipboardMessage
                {
                    Type = SocketMessageType.Clipboard,
                    Content = content
                };
                string jsonMessage = SocketMessageSerializer.Serialize(clipboardMessage);
                _webSocketServer.MulticastText(jsonMessage);
            }

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
