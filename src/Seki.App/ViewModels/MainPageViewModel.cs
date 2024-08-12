using CommunityToolkit.Mvvm.ComponentModel;
using Seki.App.Data.Models;
using Seki.App.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;

namespace Seki.App.ViewModels
{
    public sealed class MainPageViewModel : ObservableObject
    {
        private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;

        private DeviceInfo _deviceInfo;
        private DeviceStatus _deviceStatus;

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

        public MainPageViewModel()
        {
            // To Access the UI thread later.
            _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            WebSocketService.Instance.DeviceStatusReceived += OnDeviceStatusReceived;
            WebSocketService.Instance.DeviceInfoReceived += OnDeviceInfoReceived;
            Task task = LoadDeviceInfoAsync();
        }

        private void OnDeviceStatusReceived(DeviceStatus deviceStatus)
        {
            _dispatcher.TryEnqueue(() =>
            {
                DeviceStatus = deviceStatus;
            });
        }

        private void OnDeviceInfoReceived(DeviceInfo? deviceInfo)
        {
            _dispatcher.TryEnqueue(() =>
            {
                DeviceInfo = deviceInfo;
            });
        }

        public void Cleanup()
        {
            WebSocketService.Instance.DeviceStatusReceived -= OnDeviceStatusReceived;
        }

        private async Task LoadDeviceInfoAsync()
        {

            System.Diagnostics.Debug.WriteLine("LoadDeviceFrom ViewModel");
            var localFolder = ApplicationData.Current.LocalFolder;
            var deviceInfoFile = await localFolder.TryGetItemAsync("deviceInfo.json") as StorageFile;

            if (deviceInfoFile != null)
            {
                System.Diagnostics.Debug.WriteLine("File Found from viewModel");
                string json = await FileIO.ReadTextAsync(deviceInfoFile);
                DeviceInfo = JsonSerializer.Deserialize<DeviceInfo>(json);
            }
            else
            {
                // Handle the scenario where the file doesn't exist
                DeviceInfo = new DeviceInfo(); // Or some other default value
            }
        }
    }
}
