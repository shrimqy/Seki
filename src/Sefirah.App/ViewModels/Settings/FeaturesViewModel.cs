using Microsoft.UI.Dispatching;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Input;
using CommunityToolkit.WinUI;
using Sefirah.App.Data.Models;
using Sefirah.App.Data.LocalDatabase;

namespace Seki.App.ViewModels.Settings
{
    public partial class FeaturesViewModel : ObservableObject
    {
        public ObservableCollection<NotificationPreferences> NotificationPreferences { get; } = [];
        private readonly DispatcherQueue _dispatcher;

        public FeaturesViewModel()
        {
            _dispatcher = DispatcherQueue.GetForCurrentThread();
            // Load notification preferences from the database
            _ = LoadNotificationPreferencesAsync();
        }

        private async Task LoadNotificationPreferencesAsync()
        {
            var preferences = await DataAccess.GetNotificationPreferences(); // Await the task to get the result
            _dispatcher.TryEnqueue(() =>
            {
                NotificationPreferences.Clear();
                foreach (var preference in preferences)
                {
                    NotificationPreferences.Add(preference);
                }
            });
        }

        public void ChangeNotificationFilter(object parameter)
        {
            if (parameter is NotificationPreferences preferences)
            {
                // Update the NotificationFilter property
                var newFilter = preferences.NotificationFilter;
                DataAccess.UpdateNotificationPreference(preferences.AppPackage, newFilter);

                // Find and update the item in the collection
                var existingItem = NotificationPreferences.FirstOrDefault(
                    p => p.AppPackage == preferences.AppPackage);

                if (existingItem != null)
                {
                    _dispatcher.TryEnqueue(() =>
                    {
                        existingItem.NotificationFilter = preferences.NotificationFilter;
                    });
                }
            }
        }
    }
}
