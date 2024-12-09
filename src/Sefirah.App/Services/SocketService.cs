using Microsoft.Extensions.Options;
using NetCoreServer;
using Sefirah.App.Data.Contracts;
using Sefirah.App.Data.EventArguments;
using Sefirah.App.Data.Models;
using Sefirah.App.Utils;
using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;

namespace Sefirah.App.Services;

public class SocketService(
    IOptions<SocketOptions> options,
    Func<IMessageHandler> messageHandlerFactory,
    IDeviceManager deviceManager,
    ILogger logger) : ISocketService, ISessionManager, IDisposable
{
    private readonly SocketOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    private readonly Lazy<IMessageHandler> _messageHandler = new(messageHandlerFactory);
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IDeviceManager _deviceManager = deviceManager ?? throw new ArgumentNullException(nameof(deviceManager));
    private SekiServer? _server;
    private bool _isRunning;
    private int _port;
    private SekiSession? _currentSession;
    private bool _disposed;

    public event EventHandler<ConnectedSessionArgs>? ClientConnectionStatusChanged;

    public int Port => _port;
    public bool IsRunning => _isRunning;

    /// <inheritdoc/>
    public async Task<bool> StartServerAsync(int? port = null)
    {
        if (_isRunning)
        {
            _logger.Warn("Server is already running");
            return false;
        }

        try
        {
            _logger.Info("Starting server...");
            string ipAddress = NetworkHelper.GetLocalIPAddress();
            _port = await FindAvailablePortAsync(port ?? 5941);

            var certificate = await CertificateLoader.LoadCertificateAsync(
                 "Sefirah.App.server.pfx",
                "1864thround");

            var context = new SslContext(SslProtocols.Tls12, certificate);
            _server = new SekiServer(context, IPAddress.Parse(ipAddress), _port, this, _logger);

            _isRunning = _server.Start();

            if (_isRunning)
            {
                _logger.Info("Server started successfully on port {0}", _port);
                return true;
            }

            _logger.Error("Failed to start server");
            return false;
        }
        catch (Exception ex)
        {
            _logger.Error("Error starting server", ex);
            return false;
        }
    }

    /// <inheritdoc/>
    public void StopServer()
    {
        if (!_isRunning || _server == null)
        {
            _logger.Debug("Stop server called when server was not running");
            return;
        }

        try
        {
            DisconnectSession();
            _server.Stop();
            _isRunning = false;
            _logger.Info("Server stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.Error("Error stopping server", ex);
            throw;
        }
    }

    /// <inheritdoc/>
    public void SendMessage(string message)
    {
        if (!_isRunning)
        {
            _logger.Warn("Cannot send message - server not running");
            return;
        }

        if (_currentSession == null)
        {
            _logger.Warn("Cannot send message - no active session");
            return;
        }

        try
        {
            _logger.Debug(message);
            _currentSession.SendJsonMessage(message);
            _logger.Debug("Message sent successfully");
        }
        catch (Exception ex)
        {
            _logger.Error("Error sending message", ex);
            throw;
        }
    }


    public async Task<Device?> VerifyDevice(DeviceInfo device)
    {
        return await _deviceManager.VerifyDevice(device);
    }

    private async Task<int> FindAvailablePortAsync(int startPort)
    {
        int port = startPort;
        const int maxPortNumber = 65535;

        while (port <= maxPortNumber)
        {
            try
            {
                using var testListener = new TcpListener(IPAddress.Any, port);
                await Task.Run(() =>
                {
                    testListener.Start();
                    testListener.Stop();
                });

                _logger.Debug("Found available port: {0}", port);
                return port;
            }
            catch (SocketException)
            {
                port++;
            }
        }

        var error = "No available ports found";
        _logger.Error(error);
        throw new InvalidOperationException(error);
    }

    public void OnConnected(ConnectedSessionArgs args, SekiSession session)
    {
        _logger.Info("Client connected: {0}", args.SessionId);
        _currentSession = session;
        ClientConnectionStatusChanged?.Invoke(this, args);
    }

    public void OnDisconnected(ConnectedSessionArgs args)
    {
        _logger.Info("Client disconnected: {0}", args.SessionId);
        ClientConnectionStatusChanged?.Invoke(this, args);
    }

    public void HandleJsonMessage(SocketMessage message, SekiSession session)
    {
        _logger.Debug("Handling JSON message from session {0}", session.Id);
        _messageHandler.Value.HandleJsonMessage(message, session);
    }

    public void HandleBinaryData(byte[] data)
    {
        _logger.Debug("Handling binary data of size {0} bytes", data.Length);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _logger.Info("Disposing SocketService");
            StopServer();
            _server?.Dispose();
        }

        _disposed = true;
    }

    /// <inheritdoc/>
    public void DisconnectSession()
    {
        if (_currentSession != null)
        {
            _currentSession.Disconnect();
            _currentSession = null;
            _logger.Info("Disconnected current session");
        }
    }
}

public class SekiSession(SslServer server, SocketService socketService, ILogger logger) : SslSession(server)
{
    private string _bufferedData = string.Empty;
    private bool _isFirstMessage = true;
    private bool _isVerified;

    protected override void OnConnected()
    {
        try
        {
            // Reset state on new connection
            _bufferedData = string.Empty;
            _isFirstMessage = true;
            _isVerified = false;
            logger.Info("Session {0} connected", Id);
        }
        catch (Exception ex)
        {
            logger.Error("Error in OnConnected for session {0}", Id, ex);
            throw;
        }
    }

    protected override async void OnReceived(byte[] buffer, long offset, long size)
    {
        try
        {
            string newData = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
            // Only log the first part of potentially large messages
            logger.Debug("Received raw data: {0}...",
                newData.Length > 100 ? newData[..100] + "..." : newData);

            if (!_isVerified && !_isFirstMessage)
            {
                logger.Warn("Unverified session {0} attempted to send message", Id);
                Disconnect();
                return;
            }

            if (_isFirstMessage)
            {
                // Split the incoming data in case multiple messages arrived together
                var messages = newData.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                if (messages.Length == 0) return;

                try
                {
                    // Process only the first message for device verification
                    var firstMessage = messages[0].Trim();
                    logger.Debug("Processing first message: {0}", firstMessage);
                    var deviceInfo = (DeviceInfo)JsonHelper.JsonToSocketMessage(firstMessage);
                    var device = await socketService.VerifyDevice(deviceInfo);

                    if (device != null)
                    {
                        _isFirstMessage = false;
                        _isVerified = true;
                        socketService.OnConnected(new ConnectedSessionArgs
                        {
                            Device = device,
                            SessionId = Id.ToString(),
                            IsConnected = true
                        }, this);

                        // Send the deviceInfo after verification
                        var (deviceID, username, avatar) = await CurrentUserInformation.GetCurrentUserInfoAsync();
                        SendJsonMessage(JsonSerializer.Serialize(new DeviceInfo
                        {
                            DeviceId = deviceID,
                            DeviceName = username,
                            UserAvatar = avatar,
                        }));

                        // Process any remaining messages that came with the first batch
                        if (messages.Length > 1)
                        {
                            _bufferedData = string.Join("\n", messages.Skip(1)) + "\n";
                            ProcessBufferedMessages();
                        }
                    }
                    else
                    {
                        _isFirstMessage = false;
                        Disconnect();
                    }
                }
                catch (Exception ex)
                {
                    logger.Error("Error processing first message for session {0}: {1}", Id, ex);
                    Disconnect();
                }
                return;
            }

            if (!_isVerified)
            {
                logger.Warn("Unverified session {0} attempted to send message", Id);
                Disconnect();
                return;
            }

            // Handle subsequent messages
            _bufferedData += newData;
            ProcessBufferedMessages();
        }
        catch (Exception ex)
        {
            logger.Error("Error in OnReceived for session {0}: {1}", Id, ex);
            Disconnect();
        }
    }

    private void ProcessBufferedMessages()
    {
        try
        {
            // Split into lines
            var messages = _bufferedData.Split('\n');

            // Process all messages except the last one (which might be incomplete)
            for (int i = 0; i < messages.Length - 1; i++)
            {
                var message = messages[i].Trim();
                if (!string.IsNullOrEmpty(message))
                {
                    try
                    {
                        logger.Debug($"Processing individual message: {message[..Math.Min(100, message.Length)]}...");
                        var socketMessage = JsonHelper.JsonToSocketMessage(message);
                        socketService.HandleJsonMessage(socketMessage, this);
                    }
                    catch (JsonException jsonEx)
                    {
                        logger.Error($"Error parsing JSON message: {jsonEx.Message}");
                        continue;
                    }
                }
            }

            // Keep the last (potentially incomplete) message in the buffer
            _bufferedData = messages[^1];
        }
        catch (Exception ex)
        {
            logger.Error("Error processing buffered messages for session {0}: {1}", Id, ex);
        }
    }

    protected override void OnDisconnected()
    {
        try
        {
            // Clear state on disconnect
            _bufferedData = string.Empty;
            _isFirstMessage = true;
            _isVerified = false;

            logger.Info("Session {0} disconnected", Id);
            socketService.OnDisconnected(new ConnectedSessionArgs
            {
                SessionId = Id.ToString(),
                IsConnected = false
            });
        }
        catch (Exception ex)
        {
            logger.Error("Error in OnDisconnected for session {0}", Id, ex);
        }
    }

    public void SendJsonMessage(string message)
    {
        try
        {
            string messageWithNewline = message + "\n";
            byte[] messageBytes = Encoding.UTF8.GetBytes(messageWithNewline);

            Send(messageBytes, 0, messageBytes.Length);
            logger.Debug("JSON message sent successfully for session {0}", Id);
        }
        catch (Exception ex)
        {
            logger.Error("Error sending message for session {0}", Id, ex);
            throw;
        }
    }

    protected override void OnHandshaking()
    {
        logger.Debug("Session {0} handshaking", Id);
        base.OnHandshaking();
    }

    protected override void OnHandshaked()
    {
        try
        {
            logger.Info("SSL session {0} handshaked successfully", Id);
        }
        catch (Exception ex)
        {
            logger.Error("Error in handshaking for session {0}", Id, ex);
            throw;
        }
    }

    protected override void OnError(SocketError error)
    {
        logger.Error("Session {0} encountered error: {1}", Id, error);
    }
}

public class SekiServer : SslServer
{
    private readonly ILogger _logger;
    private readonly SocketService _socketService;

    public SekiServer(SslContext context, IPAddress address, int port, SocketService socketService, ILogger logger)
        : base(context, address, port)
    {
        _socketService = socketService;
        _logger = logger;
    }

    protected override SslSession CreateSession()
    {
        _logger.Debug("Creating new session");
        return new SekiSession(this, _socketService, _logger);
    }

    protected override void OnError(SocketError error)
    {
        _logger.Error("Server encountered error: {0}", error);
    }
}