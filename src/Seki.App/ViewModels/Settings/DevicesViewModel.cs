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
                // Get the local folder where the app stores its data
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;

                // Check if the file exists first to avoid FileNotFoundException
                IStorageItem deviceInfoFileItem = await localFolder.TryGetItemAsync("deviceInfo.json");

                if (deviceInfoFileItem == null)
                {
                    // File does not exist, no need to proceed further
                    System.Diagnostics.Debug.WriteLine("Device info file not found. This is expected if no devices have been added yet.");
                    return;
                }

                // Cast the found item to a StorageFile
                StorageFile deviceInfoFile = (StorageFile)deviceInfoFileItem;

                // Read the file content as text
                string json = await FileIO.ReadTextAsync(deviceInfoFile);

                // Check if the file contains data
                if (!string.IsNullOrWhiteSpace(json))
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        WriteIndented = true
                    };

                    // Attempt to deserialize the JSON into a list of Device objects
                    List<Device>? deviceList = null;
                    try
                    {
                        deviceList = JsonSerializer.Deserialize<List<Device>>(json, options);
                    }
                    catch (JsonException ex)
                    {
                        // Handle JSON parsing errors
                        System.Diagnostics.Debug.WriteLine($"Devices ViewModel, Error deserializing device info: {ex.Message}");
                    }

                    // If deserialization is successful, add devices to the connected list
                    if (deviceList != null)
                    {
                        foreach (var device in deviceList)
                        {
                            ConnectedDevices.Add(device);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Catch any unexpected errors
                System.Diagnostics.Debug.WriteLine($"Error loading device info: {ex.Message}");
            }
        }
    }
}