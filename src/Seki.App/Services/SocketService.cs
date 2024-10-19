using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Windows.Networking.Connectivity;
using Instances;
using Seki.App.Utils;
using Seki.App.Data.Models;
using System.Text;
using Seki.App.Helpers;
using System.IO;

namespace Seki.App.Services
{
    public class SocketService
    {
        private static SocketService? instance;
        private TcpListener? _listener;
        private SekiSession? _currentSession;
        private bool _isRunning;
        private int _port;

        public event EventHandler<bool>? ClientConnectionStatusChanged;

        private SocketService()
        {
            _isRunning = false;
        }

        public static SocketService Instance => instance ??= new SocketService();

        public async Task StartServerAsync(int port = 5149)
        {
            if (_isRunning)
            {
                throw new InvalidOperationException("Server is already running.");
            }

            string ipAddress = NetworkHelper.GetLocalIPAddress();
            _port = await FindAvailablePortAsync(port);

            try
            {
                _listener = new TcpListener(IPAddress.Parse(ipAddress), _port);
                _listener.Start();
                _isRunning = true;

                Debug.WriteLine($"Server started on {ipAddress}:{_port}");

                _listener.BeginAcceptTcpClient(HandleTcpClientAsync, _listener); 

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error starting server: {ex.Message}");
                _isRunning = false;
                throw;
            }
        }

        private async void HandleTcpClientAsync(IAsyncResult ar)
        {
            if (!_isRunning) return;

            var listener = ar.AsyncState as TcpListener;
            TcpClient client = listener.EndAcceptTcpClient(ar);  // Finalize accepting the connection

            // close previous connection or reject the new one
            _currentSession?.Close();

            _currentSession = new SekiSession(client);
            OnClientConnectionStatusChanged(true);

            try
            {
                using NetworkStream stream = client.GetStream();
                using StreamReader reader = new(stream, Encoding.UTF8);
                using MemoryStream memoryStream = new(); // To hold the binary data

                // Buffer to hold incoming data
                byte[] buffer = new byte[1024*1024]; // You can adjust the size as needed
                int bytesRead;

                while (client.Connected)
                {
                    // First, check for a JSON message
                    string? messageJson = await reader.ReadLineAsync();
                    if (messageJson != null)
                    {
                        if (IsValidJson(messageJson))
                        {
                            try
                            {
                                SocketMessage message = SocketMessageSerializer.DeserializeMessage(messageJson);
                                MessageHandler.HandleMessage(message, _currentSession);
                            }
                            catch (JsonException ex)
                            {
                                Debug.WriteLine($"JSON deserialization error: {ex.Message}");
                            }
                        }
                        else
                        {
                            // Not valid JSON, handle as binary data
                            memoryStream.SetLength(0); // Reset MemoryStream for new binary data

                            // Read binary data until the stream is closed
                            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                memoryStream.Write(buffer, 0, bytesRead);
                            }

                            // Get the binary data from the MemoryStream
                            byte[] binaryData = memoryStream.ToArray();

                            // Trigger the ScreenDataReceived event
                            MessageHandler.HandleBinaryData(binaryData);
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                // Handle post-disconnection cleanup
                _currentSession?.Close();
                OnClientConnectionStatusChanged(false);
                Debug.WriteLine("Client Disconnected");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error handling client: {ex.Message}");
            }
            finally
            {
                _currentSession?.Close();
                _currentSession = null;
                Debug.WriteLine("Client Disconnected");
                OnClientConnectionStatusChanged(false);
            }

            // Listen for the next connection
            listener.BeginAcceptTcpClient(HandleTcpClientAsync, listener);
        }

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


        public void StopServer()
        {
            if (!_isRunning)
            {
                throw new InvalidOperationException("Server is not running.");
            }

            _isRunning = false;
            _listener?.Stop();
            _currentSession?.Close();
            _currentSession = null;

            Debug.WriteLine("Server stopped.");
        }

        public async Task SendMessage(string message)
        {
            if (_currentSession == null)
            {
                throw new InvalidOperationException("No active session.");
            }
            await _currentSession.SendMessageAsync(message);
        }

        private void OnClientConnectionStatusChanged(bool isConnected)
        {
            ClientConnectionStatusChanged?.Invoke(this, isConnected);
        }

        private async Task<int> FindAvailablePortAsync(int startPort)
        {
            int port = startPort;
            bool isAvailable = false;

            while (!isAvailable)
            {
                try
                {
                    TcpListener testListener = new TcpListener(IPAddress.Any, port);
                    await Task.Run(() => testListener.Start());
                    testListener.Stop();
                    isAvailable = true;
                }
                catch (SocketException)
                {
                    port++;
                }
            }

            return port;
        }

        public int Port => _port;
        public bool IsRunning => _isRunning;
    }

    public class SekiSession(TcpClient client)
    {
        private TcpClient _client = client;

        public async Task SendMessageAsync(string message)
        {
            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(message + "\n");  // Add newline for proper parsing on the client
                NetworkStream stream = _client.GetStream();
                await stream.WriteAsync(buffer, 0, buffer.Length);
                await stream.FlushAsync();  // Ensure the message is sent immediately
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error sending message: {ex.Message}");
                throw;
            }
        }

        public void Close()
        {
            _client.Close();
        }
    }
}