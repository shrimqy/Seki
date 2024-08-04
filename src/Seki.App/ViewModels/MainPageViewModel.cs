using CommunityToolkit.Mvvm.ComponentModel;
using Seki.App.Data.Models;
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
        private DeviceInfo _deviceInfo;

        public DeviceInfo DeviceInfo
        {
            get => _deviceInfo;
            set => SetProperty(ref _deviceInfo, value);
        }

        public MainPageViewModel()
        {
            LoadDeviceInfoAsync();
        }

        private async Task LoadDeviceInfoAsync()
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var deviceInfoFile = await localFolder.TryGetItemAsync("deviceInfo.json") as StorageFile;
            if (deviceInfoFile != null)
            {
                string json = await FileIO.ReadTextAsync(deviceInfoFile);
                DeviceInfo = JsonSerializer.Deserialize<DeviceInfo>(json);
            }
        }
    }
}
