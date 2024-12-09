using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Sefirah.App.Data.Contracts;
using Sefirah.App.Data.Enums;
using Sefirah.App.Data.LocalDatabase;
using Sefirah.App.Data.Models;
using Sefirah.App.Utils;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Sefirah.App.Services;

public class NotificationService : INotificationService
{
    private readonly ILogger _logger;
    private readonly ISessionManager _sessionManager;
    private readonly DispatcherQueue _dispatcher;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly ObservableCollection<NotificationMessage> _notifications;

    public ReadOnlyObservableCollection<NotificationMessage> NotificationHistory { get; }
    public event EventHandler<NotificationMessage>? NotificationReceived;

    public NotificationService(
        ILogger logger,
        ISessionManager sessionManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        _dispatcher = DispatcherQueue.GetForCurrentThread();
        _notifications = [];
        NotificationHistory = new ReadOnlyObservableCollection<NotificationMessage>(_notifications);
        _logger.Debug("NotificationService constructed successfully");
    }

    public async Task HandleNotificationMessage(NotificationMessage message)
    {
        await _semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            _logger.Debug("Processing notification message from {0}", message.AppName!);
            var filter = DataAccess.GetNotificationFilter(message.AppPackage!)
                         ?? DataAccess.AddAppPreference(message.AppPackage!, message.AppName!, message.AppIcon);

            if (filter == NotificationFilter.DISABLED) return;


            if (message.Title != null)
            {
                if (message.NotificationType == nameof(NotificationType.ACTIVE) && filter == NotificationFilter.FEED || filter == NotificationFilter.TOASTEDFEED)
                {
                    await _dispatcher.EnqueueAsync(() =>
                    {
                        _notifications.Add(message);

                    });
                }
                else if (message.NotificationType == nameof(NotificationType.NEW) && filter == NotificationFilter.TOASTEDFEED)
                {
                    await _dispatcher.EnqueueAsync(() =>
                    {
                        _notifications.Add(message);

                    });
                    await ShowWindowsNotification(message);
                }
                else if (message.NotificationType == nameof(NotificationType.REMOVED))
                {
                    await _dispatcher.EnqueueAsync(() =>
                    {
                        _notifications.Remove(message);
                    });
                }
                else
                {
                    _logger.Warn("Notification from {0} does not meet criteria for Windows feed display", message.AppName);
                }
            }
            else
            {
                _logger.Warn("Notification from {0} does not meet criteria for Windows feed display", message.AppName);
            }
        }
        catch (Exception ex)
        {
            _logger.Error("Error handling notification message", ex);
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task ShowWindowsNotification(NotificationMessage message)
    {
        try
        {
            var builder = new AppNotificationBuilder()
                .AddText(message.AppName, new AppNotificationTextProperties().SetMaxLines(1))
                .AddText(message.Title)
                .AddText(message.Text)
                .SetTag(message.Tag ?? string.Empty)
                .SetGroup(message.GroupKey ?? string.Empty);

            if (!string.IsNullOrEmpty(message.LargeIcon))
            {
                await SetNotificationIcon(builder, message.LargeIcon, "largeIcon.png");
            }
            else if (!string.IsNullOrEmpty(message.AppIcon))
            {
                await SetNotificationIcon(builder, message.AppIcon, "appIcon.png");
            }

            var notification = builder.BuildNotification();
            notification.ExpiresOnReboot = true;
            AppNotificationManager.Default.Show(notification);
            _logger.Debug("Windows notification shown for {0}", message.AppName);
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to show Windows notification for {0}", message.AppName, ex);
            throw;
        }
    }

    private async Task SetNotificationIcon(AppNotificationBuilder builder, string iconBase64, string fileName)
    {
        try
        {
            // Save file and get URI in one operation
            var fileUri = await SaveBase64ToFileAsync(iconBase64, fileName);
            builder.SetAppLogoOverride(fileUri, AppNotificationImageCrop.Circle);
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to set notification icon", ex);
        }
    }

    private static async Task<Uri> SaveBase64ToFileAsync(string base64, string fileName)
    {
        var bytes = Convert.FromBase64String(base64);
        var localFolder = ApplicationData.Current.LocalFolder;
        var file = await localFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);

        using (var stream = await file.OpenAsync(FileAccessMode.ReadWrite))
        {
            using var dataWriter = new DataWriter(stream);
            dataWriter.WriteBytes(bytes);
            await dataWriter.StoreAsync();
        }

        return new Uri($"ms-appdata:///local/{fileName}");
    }

    public async Task RemoveNotification(string notificationKey, bool isRemote)
    {
        await _dispatcher.EnqueueAsync(async () =>
        {
            try
            {
                var notificationToRemove = _notifications.FirstOrDefault(n =>
                    n.NotificationKey == notificationKey);

                if (notificationToRemove != null)
                {
                    _notifications.Remove(notificationToRemove);
                    _logger.Debug("Removed notification with key: {0}", notificationKey);

                    if (!string.IsNullOrEmpty(notificationToRemove.Tag))
                    {
                        await AppNotificationManager.Default.RemoveByTagAsync(notificationToRemove.Tag);
                        _logger.Debug("Removed Windows notification by tag: {0}", notificationToRemove.Tag);
                    }
                    else if (!string.IsNullOrEmpty(notificationToRemove.GroupKey))
                    {
                        await AppNotificationManager.Default.RemoveByGroupAsync(notificationToRemove.GroupKey);
                        _logger.Debug("Removed Windows notification by group: {0}", notificationToRemove.GroupKey);
                    }

                    if (!isRemote)
                    {
                        notificationToRemove.NotificationType = nameof(NotificationType.REMOVED);
                        string jsonMessage = SocketMessageSerializer.Serialize(notificationToRemove);
                        _sessionManager.SendMessage(jsonMessage);
                        _logger.Debug("Sent notification removal message to remote device");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error removing notification", ex);
                throw;
            }
        });
    }

    public async Task ClearAllNotification()
    {
        await _dispatcher.EnqueueAsync(async () =>
        {
            try
            {
                _notifications.Clear();
                var command = new Command { CommandType = nameof(CommandType.CLEAR_NOTIFICATIONS) };
                string jsonMessage = SocketMessageSerializer.Serialize(command);
                _sessionManager.SendMessage(jsonMessage);
                _logger.Info("Cleared all notifications");
            }
            catch (Exception ex)
            {
                _logger.Error("Error clearing all notifications", ex);
                throw;
            }
        });
    }

    public async Task ClearHistory()
    {
        await _dispatcher.EnqueueAsync(() =>
        {
            try
            {
                _notifications.Clear();
            }
            catch (Exception ex)
            {
                _logger.Error("Error clearing history", ex);
                throw;
            }
        });
    }
}
