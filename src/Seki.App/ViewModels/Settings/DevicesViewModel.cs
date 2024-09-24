using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Options;
using Seki.App.Data.Models;
using Seki.App.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;

namespace Seki.App.ViewModels.Settings
{
    public class DevicesViewModel : ObservableObject
    {
        private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;
        public ObservableCollection<Device?> ConnectedDevices { get; } = new ObservableCollection<Device?>();

        public DevicesViewModel()
        {
            _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            WebSocketService.Instance.DeviceInfoReceived += OnDeviceInfoReceived;
            // Load devices when the ViewModel is initialized
            _ = LoadDeviceInfoAsync();

        }

        private void OnDeviceInfoReceived(Device? deviceInfo)
        {
            if (deviceInfo == null) return;

            _dispatcher.TryEnqueue(() =>
            {
                var existingDevice = ConnectedDevices.FirstOrDefault(d => d?.Name == deviceInfo.Name);
                if (existingDevice != null)
                {
                    var index = ConnectedDevices.IndexOf(existingDevice);
                    ConnectedDevices[index] = deviceInfo;
                }
                else
                {
                    ConnectedDevices.Add(deviceInfo);
                }

                System.Diagnostics.Debug.WriteLine($"Device info updated: {deviceInfo.Name}");
            });
        }

        public void Cleanup()
        {
            WebSocketService.Instance.DeviceInfoReceived -= OnDeviceInfoReceived;
        }

        private async Task LoadDeviceInfoAsync()
        {
            try
            {
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                StorageFile deviceInfoFile = await localFolder.GetFileAsync("deviceInfo.json");

                if (deviceInfoFile != null)
                {
                    string json = await FileIO.ReadTextAsync(deviceInfoFile);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            WriteIndented = true
                        };

                        List<Device>? deviceList;
                        try
                        {
                            deviceList = JsonSerializer.Deserialize<List<Device>>(json, options);
                        }
                        catch (FileNotFoundException)
                        {
                            System.Diagnostics.Debug.WriteLine($"File Not Found");
                            deviceList = null;
                        }
                        catch (JsonException ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Devices ViewModel, Error deserializing device info: {ex.Message}");
                            deviceList = new List<Device>();
                        }

                        if (deviceList != null)
                        {
                            foreach (var device in deviceList)
                            {
                                ConnectedDevices.Add(device);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading device info: {ex.Message}");
            }
        }
    }
}