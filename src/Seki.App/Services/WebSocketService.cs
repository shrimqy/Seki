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

namespace Seki.App.Services
{
    public class WebSocketService
    {
        private static WebSocketService? _instance;
        private SekiServer _webSocketServer;
        private bool _isRunning;

        public event Action<DeviceStatus>? DeviceStatusReceived;
        public event Action<DeviceInfo>? DeviceInfoReceived;

        private WebSocketService()
        {
            string ipAddress = NetworkHelper.GetLocalIPAddress();
            _webSocketServer = new SekiServer(IPAddress.Parse(ipAddress), 5149);
            _webSocketServer.DeviceStatusReceived += OnDeviceStatusReceived;
            _webSocketServer.DeviceInfoReceived += OnDeviceInfoReceived;

        }

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

        public void SendMessage(string message)
        {
            if (_isRunning)
            {
                _webSocketServer.MulticastText(message);
            }
        }

        public bool IsRunning => _isRunning;

        private void OnDeviceStatusReceived(DeviceStatus deviceStatus)
        {
            DeviceStatusReceived?.Invoke(deviceStatus);
        }

        private void OnDeviceInfoReceived(DeviceInfo deviceInfo)
        {
            DeviceInfoReceived?.Invoke(deviceInfo);
        }
    }

    public class SekiServer(IPAddress address, int port) : WsServer(address, port)
    {
        public event Action<DeviceStatus>? DeviceStatusReceived;
        public event Action<DeviceInfo>? DeviceInfoReceived;

        protected override TcpSession CreateSession() { return new SekiSession(this); }

        protected override void OnError(SocketError error)
        {
            System.Diagnostics.Debug.WriteLine($"WebSocket server caught an error with code {error}");
        }

        public void OnDeviceStatusReceived(DeviceStatus deviceStatus)
        {
            DeviceStatusReceived?.Invoke(deviceStatus);
        }

        public void OnDeviceInfoReceived(DeviceInfo deviceInfo)
        {
            DeviceInfoReceived?.Invoke(deviceInfo);
        }
    }

    public class SekiSession(SekiServer server) : WsSession(server)
    {
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
                MessageHandler.HandleMessage(message, this);
            }
            catch (JsonException ex)
            {
                System.Diagnostics.Debug.WriteLine($"JSON deserialization error: {ex.Message}");
                SendMessage(new Response { Content = "Invalid message format: " + ex.Message });
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
}
