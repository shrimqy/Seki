using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Seki.App.Data.Models;
using Seki.App.Helpers;
using Seki.App.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Notifications;

namespace Seki.App.ViewModels
{
    public sealed class MainPageViewModel : ObservableObject
    {
        private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;
        private Device _deviceInfo = new();
        private DeviceStatus _deviceStatus = new();
        private bool _connectionStatus = false;
        private ObservableCollection<NotificationMessage> _recentNotifications = [];
        
        public Device DeviceInfo
        {
            get => _deviceInfo;
            set => SetProperty(ref _deviceInfo, value);
        }

        public DeviceStatus DeviceStatus
        {
            get => _deviceStatus;
            set => SetProperty(ref _deviceStatus, value);
        }

        public bool ConnectionStatus
        {
            get => _connectionStatus;
            set
            {
                if (SetProperty(ref _connectionStatus, value))
                {
                    OnPropertyChanged(nameof(ConnectionButtonText));
                }
            }
        }

        public string ConnectionButtonText => ConnectionStatus ? "Connected" : "Disconnected";

        public ICommand ToggleConnectionCommand { get; }

        // Command to clear notifications
        public ICommand ClearAllNotificationsCommand { get; }

        public ObservableCollection<NotificationMessage> RecentNotifications
        {
            get => _recentNotifications;
            set => SetProperty(ref _recentNotifications, value);
        }
        public MainPageViewModel()
        {
            _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            WebSocketService.Instance.ConnectionStatusChange += OnConnectionStatusChange;
            WebSocketService.Instance.DeviceStatusReceived += OnDeviceStatusReceived;
            WebSocketService.Instance.DeviceInfoReceived += OnDeviceInfoReceived;
            NotificationService.NotificationReceived += OnNotificationReceived;
            _ = LoadDeviceInfoAsync();

            ToggleConnectionCommand = new RelayCommand(ToggleConnection);

            // Initialize the ClearAllNotificationsCommand
            ClearAllNotificationsCommand = new RelayCommand(ClearAllNotifications);
        }

        private void ClearAllNotifications()
        {
            // Clear the notifications list
            RecentNotifications.Clear();
        }

        private void ToggleConnection()
        {
            if (ConnectionStatus) 
            {
                WebSocketService.Instance.DisconnectAll();
            }
        }
        private void OnConnectionStatusChange(bool connectionStatus)
        {
            System.Diagnostics.Debug.WriteLine($"connection status changed");

            // If the connection is re-established (changing from false to true)
            if (!_connectionStatus && connectionStatus || connectionStatus)
            {
                _dispatcher.TryEnqueue(() => RecentNotifications.Clear()); // Clear recent notifications
            }

            // Only update the ConnectionStatus property if the status changes
            if (_connectionStatus != connectionStatus)
            {
                _dispatcher.TryEnqueue(() => ConnectionStatus = connectionStatus);
            }
        }

        private void OnDeviceStatusReceived(DeviceStatus deviceStatus)
        {
            _dispatcher.TryEnqueue(() => DeviceStatus = deviceStatus);
        }

        private void OnDeviceInfoReceived(Device? deviceInfo)
        {
            if (deviceInfo != null)
            {
                _dispatcher.TryEnqueue(() => DeviceInfo = deviceInfo);
            }
        }

        public void RemoveNotification(string notificationKey)
        {
            var notificationToRemove = RecentNotifications.FirstOrDefault(n => n.NotificationKey == notificationKey);

            if (notificationToRemove != null)
            {
                notificationToRemove.NotificationType = "REMOVE";
                string jsonMessage = SocketMessageSerializer.Serialize(notificationToRemove);
                //WebSocketService.Instance.SendMessage(jsonMessage);
                RecentNotifications.Remove(notificationToRemove);
                System.Diagnostics.Debug.WriteLine($"Removed notification from RecentNotifications: {notificationToRemove.NotificationKey}");
            }
        }

        private void OnNotificationReceived(object? sender, NotificationMessage? notification)
        {
            _dispatcher.TryEnqueue(async () =>
            {
                if (notification == null) return;
                var existingNotification = RecentNotifications.FirstOrDefault(n => n.NotificationKey == notification.NotificationKey);

                if (notification.NotificationType == "REMOVED")
                {
                    // Find and remove the notification from the RecentNotifications list
                    if (existingNotification != null)
                    {
                        RecentNotifications.Remove(existingNotification);
                        System.Diagnostics.Debug.WriteLine($"Removed notification from RecentNotifications: {notification.NotificationKey}");
                    }
                }
                else
                {
                    // Handle adding new notifications
                    if (notification.Icon == null && !string.IsNullOrEmpty(notification.IconBase64))
                    {
                        notification.Icon = await Base64ToBitmapImage(notification.IconBase64);
                    }
                    if (existingNotification != null)
                    {
                        RecentNotifications.Remove(existingNotification);
                    }
                    RecentNotifications.Insert(0, notification);

                    // Sort the notifications by timestamp (descending)
                    var sortedNotifications = RecentNotifications.OrderByDescending(n => n.TimeStamp).ToList();

                    // Replace the collection with the sorted one
                    RecentNotifications.Clear();
                    foreach (var sortedNotification in sortedNotifications)
                    {
                        RecentNotifications.Add(sortedNotification);
                    }
                }
            });
        }

        private async Task<BitmapImage> Base64ToBitmapImage(string base64String)
        {
            byte[] bytes = Convert.FromBase64String(base64String);
            using (InMemoryRandomAccessStream stream = new())
            {
                await stream.WriteAsync(bytes.AsBuffer());
                stream.Seek(0);

                BitmapImage image = new();
                await image.SetSourceAsync(stream);
                return image;
            }
        }

        public void Cleanup()
        {
            WebSocketService.Instance.DeviceStatusReceived -= OnDeviceStatusReceived;
            WebSocketService.Instance.DeviceInfoReceived -= OnDeviceInfoReceived;
            NotificationService.NotificationReceived -= OnNotificationReceived;
        }

        private static async Task<List<Device>?> CheckForSavedDevicesAsync()
        {
            try
            {
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                StorageFile deviceInfoFile = await localFolder.GetFileAsync("deviceInfo.json");

                // Read the file's contents
                string json = await FileIO.ReadTextAsync(deviceInfoFile);

                // Deserialize the JSON into a List<Devices>
                if (!string.IsNullOrWhiteSpace(json))
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        WriteIndented = true
                    };

                    var deviceList = JsonSerializer.Deserialize<List<Device>>(json, options);
                    return deviceList ?? new List<Device>();  // Return the list or an empty list if null
                }

                return null; // Return null if the file is empty
            }
            catch (FileNotFoundException)
            {
                return null; // Return null if the file does not exist
            }
            catch (JsonException ex)
            {
                // Log or handle the deserialization error
                System.Diagnostics.Debug.WriteLine($"Error deserializing device info: {ex.Message}");
                return null; // Return null if there is a deserialization error
            }
            catch (Exception ex)
            {
                // Catch any other exceptions
                System.Diagnostics.Debug.WriteLine($"Unexpected error: {ex.Message}");
                return null;
            }
        }

        private async Task LoadDeviceInfoAsync()
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            List<Device>? devices = await CheckForSavedDevicesAsync();
            if (devices != null && devices.Any())
            {
                // Find the last connected device
                var lastConnected = devices.OrderByDescending(d => d.LastConnected).FirstOrDefault();
                DeviceInfo = lastConnected;
            }
        }
    }
}
