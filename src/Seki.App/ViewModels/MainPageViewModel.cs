using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media.Imaging;
using Seki.App.Data.Models;
using Seki.App.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Seki.App.ViewModels
{
    public sealed class MainPageViewModel : ObservableObject
    {
        private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;
        private DeviceInfo _deviceInfo = new();
        private DeviceStatus _deviceStatus = new();
        private ObservableCollection<NotificationMessage> _recentNotifications = [];

        public DeviceInfo DeviceInfo
        {
            get => _deviceInfo;
            set => SetProperty(ref _deviceInfo, value);
        }

        public DeviceStatus DeviceStatus
        {
            get => _deviceStatus;
            set => SetProperty(ref _deviceStatus, value);
        }

        public ObservableCollection<NotificationMessage> RecentNotifications
        {
            get => _recentNotifications;
            set => SetProperty(ref _recentNotifications, value);
        }

        public MainPageViewModel()
        {
            _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            WebSocketService.Instance.DeviceStatusReceived += OnDeviceStatusReceived;
            WebSocketService.Instance.DeviceInfoReceived += OnDeviceInfoReceived;
            NotificationService.NotificationReceived += OnNotificationReceived;
            _ = LoadDeviceInfoAsync();
        }

        private void OnDeviceStatusReceived(DeviceStatus deviceStatus)
        {
            _dispatcher.TryEnqueue(() => DeviceStatus = deviceStatus);
        }

        private void OnDeviceInfoReceived(DeviceInfo? deviceInfo)
        {
            if (deviceInfo != null)
            {
                _dispatcher.TryEnqueue(() => DeviceInfo = deviceInfo);
            }
        }

        private void OnNotificationReceived(object? sender, NotificationMessage notification)
        {
            _dispatcher.TryEnqueue(async () =>
            {
                if (notification.Icon == null && !string.IsNullOrEmpty(notification.IconBase64))
                {
                    notification.Icon = await Base64ToBitmapImage(notification.IconBase64);
                }
                RecentNotifications.Insert(0, notification);
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

        private async Task LoadDeviceInfoAsync()
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var deviceInfoFile = await localFolder.TryGetItemAsync("deviceInfo.json") as StorageFile;
            if (deviceInfoFile != null)
            {
                string json = await FileIO.ReadTextAsync(deviceInfoFile);
                var loadedDeviceInfo = JsonSerializer.Deserialize<DeviceInfo>(json);
                if (loadedDeviceInfo != null)
                {
                    DeviceInfo = loadedDeviceInfo;
                }
            }
        }
    }
}
