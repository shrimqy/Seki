using Sefirah.App.Data.Contracts;
using Sefirah.App.Data.Models;
using Sefirah.App.Services;
using Windows.Storage;

namespace Sefirah.App.ViewModels.Settings
{
    public class DevicesViewModel : ObservableObject
    {
        private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;
        private readonly IMdnsService _mdnsService;

        // This collection is for devices you already have connected.
        public ObservableCollection<Device?> ConnectedDevices { get; } = [];

        // This list is for discovered devices, not yet connected.
        public ObservableCollection<DiscoveredDevice> DiscoveredDevices { get; } = [];

        public DevicesViewModel(IMdnsService mdnsService)
        {
            _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            _mdnsService = mdnsService;
            _mdnsService.DeviceDiscovered += OnDeviceDiscovered;
            _mdnsService.DeviceLost += OnDeviceLost;

            // Load devices when the ViewModel is initialized
            _ = LoadDeviceInfoAsync();
        }

        private void OnDeviceInfoReceived(object? sender, Device deviceInfo)
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

                Debug.WriteLine($"Device info updated: {deviceInfo.Name}");
            });
        }

        private void OnDeviceDiscovered(object? sender, DiscoveredDevice discoveredDevice)
        {
            if (discoveredDevice == null) return;

            _dispatcher.TryEnqueue(() =>
            {
                var existingDevice = DiscoveredDevices.FirstOrDefault(d => d.ServiceName == discoveredDevice.ServiceName);

                if (existingDevice == null)
                {
                    DiscoveredDevices.Add(discoveredDevice);
                    Debug.WriteLine($"Discovered new device: {discoveredDevice.ServiceName}");
                }
                else if (existingDevice.HashedKey != discoveredDevice.HashedKey)
                {
                    // Directly update the existing device's key
                    int index = DiscoveredDevices.IndexOf(existingDevice);
                    DiscoveredDevices[index] = discoveredDevice;
                    Debug.WriteLine($"Updated device key for: {discoveredDevice.ServiceName}");
                }
            });
        }

        private void OnDeviceLost(object? sender, string device)
        {
            if (device == null) return;

            _dispatcher.TryEnqueue(() =>
            {
                var existingDevice = DiscoveredDevices.FirstOrDefault(d => d.ServiceName == device);
                if (existingDevice != null)
                {
                    Debug.WriteLine($"Removing device: {existingDevice.DeviceName}");
                    DiscoveredDevices.Remove(existingDevice);
                }
            });
        }

        public void Cleanup()
        {
            _mdnsService.DeviceDiscovered -= OnDeviceDiscovered;
            _mdnsService.DeviceLost -= OnDeviceLost;
        }

        private async Task LoadDeviceInfoAsync()
        {
            try
            {
                // Get the local folder where the app stores its data
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;

                // Check if the file exists first to avoid FileNotFoundException
                IStorageItem deviceInfoFileItem = await localFolder.TryGetItemAsync("deviceList.json");

                if (deviceInfoFileItem == null)
                {
                    // File does not exist, no need to proceed further
                    Debug.WriteLine("Device info file not found. This is expected if no devices have been added yet.");
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
                        Debug.WriteLine($"Devices ViewModel, Error deserializing device info: {ex.Message}");
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
                Debug.WriteLine($"Error loading device info: {ex.Message}");
            }
        }
    }
}