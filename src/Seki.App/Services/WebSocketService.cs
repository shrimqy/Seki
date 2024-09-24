using Microsoft.Windows.System;
using NetCoreServer;
using Seki.App.Data.Models;
using Seki.App.Helpers;
using Seki.App.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.ApplicationModel.SocialInfo;
using Windows.ApplicationModel.UserDataAccounts;
using Windows.Storage.Streams;
using Windows.System;
using Windows.System.UserProfile;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Seki.App.Services
{
    public class WebSocketService
    {
        private static WebSocketService? _instance;
        private SekiServer _webSocketServer;

        private bool _isRunning;

        public event Action<DeviceStatus>? DeviceStatusReceived;
        public event Action<Device>? DeviceInfoReceived;
        public event Action<bool>? ConnectionStatusChange;


        private WebSocketService()
        {
            string ipAddress = NetworkHelper.GetLocalIPAddress();
            _webSocketServer = new SekiServer(IPAddress.Parse(ipAddress), 5149);
            _webSocketServer.DeviceStatusReceived += OnDeviceStatusReceived;
            _webSocketServer.DeviceInfoReceived += OnDeviceInfoReceived;
            _webSocketServer.ConnectionStatusChange += OnConnectionStatusChange;
        }

        public static WebSocketService Instance => _instance ??= new WebSocketService();

        public void Start()
        {
            if (!_isRunning)
            {
                _webSocketServer.Start();
                _isRunning = true;
                System.Diagnostics.Debug.WriteLine($"WebSocket server started at ws://{_webSocketServer.Address}:{_webSocketServer.Port}");
                OnConnectionStatusChange(true);
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

        public void DisconnectAll()
        {
            if (_isRunning)
            {
                _webSocketServer.DisconnectAll();
                System.Diagnostics.Debug.WriteLine("Disconnected all sessions.");
            }
        }

        public void SendMessage(string message)
        {
            if (_isRunning)
            {

                System.Diagnostics.Debug.WriteLine(message);
                _webSocketServer.MulticastText(message);
            }
        }

        public bool IsRunning => _isRunning;

        private void OnDeviceStatusReceived(DeviceStatus deviceStatus)
        {
            DeviceStatusReceived?.Invoke(deviceStatus);
        }

        private void OnDeviceInfoReceived(Device deviceInfo)
        {
            DeviceInfoReceived?.Invoke(deviceInfo);
        }


        private void OnConnectionStatusChange(bool connectionStatus)
        {
            _isRunning = connectionStatus;
            ConnectionStatusChange?.Invoke(connectionStatus);
        }
    }

    public class SekiServer(IPAddress address, int port) : WsServer(address, port)
    {

        private SekiSession? _activeSession;

        public event Action<DeviceStatus>? DeviceStatusReceived;
        public event Action<DeviceInfo>? DeviceInfoReceived;
        public event Action<bool>? ConnectionStatusChange;

        protected override TcpSession CreateSession()
        {
            if (_activeSession != null)
            {
                // If there's an active session, close it
                _activeSession.Close();
            }
            _activeSession = new SekiSession(this);
            return _activeSession;
        }



        protected override void OnStarted()
        {
            base.OnStarted();
            OnConnectionStatusChange(true);
        }

        protected override void OnStopped()
        {
            base.OnStopped();
            OnConnectionStatusChange(false);
        }

        public void SessionDisconnected(SekiSession session)
        {
            if (_activeSession == session)
            {
                _activeSession = null;
            }
        }


        public void OnDeviceStatusReceived(DeviceStatus deviceStatus)
        {
            DeviceStatusReceived?.Invoke(deviceStatus);
        }


        public void OnDeviceInfoReceived(Device deviceInfo)
        {
            DeviceInfoReceived?.Invoke(deviceInfo);
        }

        public void OnConnectionStatusChange(bool connectionStatus)
        {
            ConnectionStatusChange?.Invoke(connectionStatus);
        }

    }

    public class SekiSession(SekiServer server) : WsSession(server)
    {
        private readonly SekiServer _server = server;
        public FileTransferService _fileTransferService = new FileTransferService();

        private MdnsService? _mdnsService;
        public override void OnWsConnected(HttpRequest request)
        {
            _mdnsService = new MdnsService();
            _mdnsService.UnAdvertiseService();
            System.Diagnostics.Debug.WriteLine($"WebSocket session with Id {Id} connected!");
            SendMessage(new Response { ResType = "Status", Content = "Connected" });
            _server.OnConnectionStatusChange(true);
        }

        public override void OnWsDisconnected()
        {
            base.OnWsDisconnected();
            _server.SessionDisconnected(this);
            _mdnsService = new MdnsService();
            _ = _mdnsService.AdvertiseServiceAsync();
            System.Diagnostics.Debug.WriteLine($"WebSocket session with Id {Id} disconnected!");
            _server.OnConnectionStatusChange(false);
        }

        public override void OnWsError(string error)
        {
            System.Diagnostics.Debug.WriteLine($"Error: {error}");
            base.OnWsError(error);
        }


        private DateTime lastMessageTime;

        public override void OnWsReceived(byte[] buffer, long offset, long size)
        {
            //var currentTime = DateTime.Now;

            //// Measure time since the last message
            //if (lastMessageTime != default)
            //{
            //    var timeDifference = currentTime - lastMessageTime;
            //    System.Diagnostics.Debug.WriteLine($"Time between messages: {timeDifference.TotalMilliseconds} ms");
            //}

            //lastMessageTime = currentTime; // Update the last message time

            // Decode the message
            string jsonMessage = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);

            // Validate the message to check if it's valid JSON
            if (IsValidJson(jsonMessage))
            {
                HandleJsonMessage(jsonMessage);
            }
            else
            {
                if (size > 0)
                {
                    HandleBinaryMessage(buffer, offset, size);  // Ensure this method processes binary data
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Received empty data.");
                }
            }
        }

        // Helper method to check if a string is valid JSON
        private bool IsValidJson(string jsonString)
        {
            jsonString = jsonString.Trim();
            if ((jsonString.StartsWith("{") && jsonString.EndsWith("}")) || // object
                (jsonString.StartsWith("[") && jsonString.EndsWith("]"))) // array
            {
                try
                {
                    JsonDocument.Parse(jsonString);
                    return true;
                }
                catch (JsonException)
                {
                    // Invalid JSON
                    return false;
                }
            }
            return false;
        }

        private void HandleJsonMessage(string jsonMessage)
        {
            try
            {
                SocketMessage message = SocketMessageSerializer.DeserializeMessage(jsonMessage);
                MessageHandler.HandleMessage(message, this);
            }
            catch (JsonException ex)
            {
                System.Diagnostics.Debug.WriteLine($"JSON deserialization error: {ex.Message}");
                SendMessage(new Response { Content = "Invalid message format: " + ex.Message });
            }
        }



        private void HandleBinaryMessage(byte[] buffer, long offset, long size)
        {
            _fileTransferService.ReceiveFileData(buffer, (int)offset, (int)size);
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
}
