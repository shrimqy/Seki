using Sefirah.App.Data.Models;

namespace Sefirah.App.Data.Contracts;

public interface IDeviceManager
{
    Task<Device> GetDeviceInfoAsync(string deviceId);
    Task<List<Device>> GetDeviceListAsync();
    Task RemoveDevice(Device device);
    Task UpdateDevice(DeviceInfo device);

    /// <summary>
    /// Updates the device status.
    /// </summary>
    Task UpdateDeviceStatus(DeviceStatus deviceStatus);

    /// <summary>
    /// Returns the device if it get's successfully verified and added to the database.
    /// </summary>
    Task<Device?> VerifyDevice(DeviceInfo device);
}