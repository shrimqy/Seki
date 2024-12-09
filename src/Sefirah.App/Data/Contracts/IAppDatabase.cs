using Sefirah.App.Data.Enums;
using Sefirah.App.Data.Models;

namespace Sefirah.App.Data.Contracts;
public interface IAppDatabase
{
    Task<NotificationFilter> AppPreference(string appPackage, string appName, string? appIconBase64);
    Task UpdateNotificationFilter(string appPackage, NotificationFilter filter);
    Task<List<NotificationPreferences>> GetNotificationPreferences();
    Task RemoveDevice(Device device);
    Task UpdateDevice(DeviceInfo device);
    Task<Device> AddDevice(DeviceInfo device);
}
