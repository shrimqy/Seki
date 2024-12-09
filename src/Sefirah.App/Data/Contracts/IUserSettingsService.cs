using Sefirah.App.Data.EventArguments;

namespace Sefirah.App.Data.Contracts;
public interface IUserSettingsService
{
    event EventHandler<SettingChangedEventArgs> OnSettingChangedEvent;

    Task<object?> GetSettingAsync(string key);

    Task SetSettingAsync(string key, object? value);
}
