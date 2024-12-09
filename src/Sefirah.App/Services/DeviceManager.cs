using Sefirah.App.Data.Contracts;
using Sefirah.App.Data.LocalDatabase;
using Sefirah.App.Data.Models;
using Sefirah.App.Utils;
using Windows.Storage;

namespace Sefirah.App.Services;

public class DeviceManager : IDeviceManager
{
    private readonly ILogger _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public DeviceManager(
        ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };
    }

    public async Task<List<Device>> GetDeviceListAsync()
    {
        try
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var deviceListFile = await localFolder.TryGetItemAsync("deviceList.json") as StorageFile;

            if (deviceListFile == null)
            {
                _logger.Info("Device list file not found, returning empty list");
                return [];
            }

            var json = await FileIO.ReadTextAsync(deviceListFile);
            if (string.IsNullOrWhiteSpace(json))
            {
                return [];
            }

            var devices = JsonSerializer.Deserialize<List<Device>>(json, _jsonOptions);
            return devices ?? [];
        }
        catch (Exception ex)
        {
            _logger.Error("Error reading device list", ex);
            return [];
        }
    }

    public Task<Device> GetDeviceInfoAsync(string deviceId)
    {
        throw new NotImplementedException();
    }

    public Task RemoveDevice(Device device)
    {
        throw new NotImplementedException();
    }

    public Task<Device> AddDevice(DeviceInfo device)
    {
        throw new NotImplementedException();
    }

    public Task UpdateDevice(DeviceInfo device)
    {
        throw new NotImplementedException();
    }

    public Task UpdateDeviceStatus(DeviceStatus deviceStatus)
    {
        throw new NotImplementedException();
    }

    public async Task<Device?> VerifyDevice(DeviceInfo device)
    {
        try
        {
            var existingDevice = await DataAccess.GetDeviceById(device.DeviceId);
            var hashedKey = Convert.FromBase64String(device.HashedSecret!.Trim());
            if (existingDevice != null)
            {
                _logger.Debug("Found existing device, comparing keys: {Expected} vs {Received}",
                    existingDevice.HashedKey, device.HashedSecret);

                if (existingDevice.HashedKey.SequenceEqual(hashedKey))
                {
                    return existingDevice;
                }
            }
            else
            {
                if (ECDHHelper.VerifyDevice(device.PublicKey, hashedKey))
                {
                    return await DataAccess.AddOrUpdateDeviceDetail(
                        device.DeviceId,
                        device.DeviceName,
                        hashedKey,
                        DateTime.Now);
                }
                _logger.Error("Device {0} verification key does not match. Expected: {1}, Received: {2}",
                    device.DeviceId, hashedKey, device.PublicKey);
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.Error("Error verifying device: {0}. Exception details: {1}",
                device.DeviceId, ex.ToString());
            return null;
        }
    }
}