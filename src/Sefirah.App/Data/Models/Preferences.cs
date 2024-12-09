using Microsoft.UI.Xaml.Media.Imaging;
using Sefirah.App.Data.Enums;
using System.Runtime.CompilerServices;

namespace Sefirah.App.Data.Models;


public class NotificationPreferences : INotifyPropertyChanged
{
    private NotificationFilter _notificationFilter;
    public string AppPackage { get; set; }
    public string AppName { get; set; }
    public BitmapImage? AppIcon { get; set; }

    public NotificationFilter NotificationFilter
    {
        get => _notificationFilter;
        set
        {
            if (_notificationFilter != value)
            {
                _notificationFilter = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
