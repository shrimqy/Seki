using Sefirah.App.Data.Contracts;
using Sefirah.App.Data.Enums;
using Sefirah.App.Data.LocalDatabase;
using Sefirah.App.Data.Models;
using Sefirah.App.Utils;

namespace Sefirah.App.Services;

public class MessageHandler(
    ILogger logger,
    INotificationService notificationService,
    IClipboardService clipboardService,
    IPlaybackService playbackService,
    ISftpService sftpServer) : IMessageHandler
{
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly INotificationService _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
    private readonly IClipboardService _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
    private readonly IPlaybackService _playbackService = playbackService ?? throw new ArgumentNullException(nameof(playbackService));
    private readonly ISftpService _sftpService = sftpServer ?? throw new ArgumentNullException(nameof(sftpServer));

    public async void HandleJsonMessage(SocketMessage message, SekiSession session)
    {
        _logger.Debug("Handling message: {0}", message);
        try
        {
            switch (message.Type)
            {
                case SocketMessageType.Clipboard:
                    _logger.Debug("Handling clipboard message");
                    await _clipboardService.SetContentAsync(((ClipboardMessage)message).Content);
                    break;

                case SocketMessageType.Notification:
                    _logger.Debug("Handling notification message");
                    await _notificationService.HandleNotificationMessage((NotificationMessage)message);
                    break;

                case SocketMessageType.DeviceInfo:
                    _logger.Debug("Received device info");
                    break;

                case SocketMessageType.DeviceStatus:
                    _logger.Debug("Received device status update");
                    break;

                case SocketMessageType.PlaybackData:
                    _logger.Debug("Handling playback data message");
                    await _playbackService.HandleLocalMediaActionAsync((PlaybackData)message);
                    break;

                case SocketMessageType.CommandType:
                    var cmd = (Command)message;
                    _logger.Debug("Received command: {0}", cmd.CommandType);
                    break;

                case SocketMessageType.StorageInfo:
                    _logger.Debug("Handling storage info message");
                    break;

                case SocketMessageType.ApplicationInfo:
                    _logger.Debug("Handling application info message");
                    var applicationInfo = (ApplicationInfo)message;
                    DataAccess.AddAppPreference(applicationInfo.PackageName, applicationInfo.AppName, applicationInfo.AppIcon);
                    break;
                case SocketMessageType.SftpServerInfo:
                    await _sftpService.InitializeAsync((SftpServerInfo)message); 
                    break;

                case SocketMessageType.ScreenData:
                    _logger.Debug("Handling screen data message");
                    break;

                default:
                    _logger.Warn("Unknown message type received: {0}", message.Type);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.Error("Error handling message of type {0}", message.Type, ex);
        }
    }

    public Task HandleBinaryData(byte[] data)
    {
        try
        {
            _logger.Debug("Handling binary data of size: {0} bytes", data.Length);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.Error("Error handling binary data", ex);
            throw;
        }
    }
}
