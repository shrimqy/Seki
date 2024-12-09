using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Sefirah.App;
using Sefirah.App.Data.Models;
using Sefirah.App.ViewModels.Settings;
using Seki.App.ViewModels.Settings;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Notifications;

namespace Sefirah.App.Views.Settings;

public sealed partial class FeaturesPage : Page
{
    public FeaturesViewModel ViewModel { get; }

    private readonly ApplicationDataContainer localSettings;
    private bool _isInitializing;
    public FeaturesPage()
    {
        this.InitializeComponent();

        // Get the application local settings
        localSettings = ApplicationData.Current.LocalSettings;

        InitializeSettings();
        LoadSavedLocation();
        ViewModel = Ioc.Default.GetRequiredService<FeaturesViewModel>();
    }

    // Load the stored preferences when the page is loaded
    private void InitializeSettings()
    {
        _isInitializing = true; // Flag to indicate we're in the initialization phase

        if (localSettings.Values.TryGetValue("ClipboardSync", out object? clipboard))
        {
            ClipboardSyncToggleSwitch.IsOn = (bool)clipboard;
        }

        if (localSettings.Values.TryGetValue("ClipboardFiles", out object? clipboardFiles))
        {
            ClipboardFilesCheckBox.IsChecked = (bool)clipboardFiles;
        }

        _isInitializing = false; // Initialization is done, event handlers can now react
    }

    private void StartupToggleSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;

        var toggleSwitch = sender as ToggleSwitch;

        // Save the preference in the application settings
        localSettings.Values["Startup"] = toggleSwitch.IsOn;
    }

    private void ClipboardSyncToggleSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;

        var toggleSwitch = sender as ToggleSwitch;

        // Save the preference in the application settings
        localSettings.Values["ClipboardSync"] = toggleSwitch!.IsOn;
    }

    private void ClipboardFilesToggle(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;

        var checkBox = sender as CheckBox;

        // Save the preference in the application settings
        localSettings.Values["ClipboardFiles"] = checkBox!.IsChecked;

    }


    private void OnMenuFlyoutItemClick(object sender, RoutedEventArgs e)
    {
        var menuItem = sender as MenuFlyoutItem;

        if (menuItem?.Tag is NotificationPreferences settings)
        {

            ViewModel.ChangeNotificationFilter(settings);
        }
    }

    private void LoadSavedLocation()
    {
        // Try to load the saved path from local settings
        if (localSettings.Values.TryGetValue("SaveLocation", out object savedPath) && savedPath is string folderPath && !string.IsNullOrEmpty(folderPath))
        {
            // Set the saved location to the TextBlock if it exists
            SelectedLocationTextBlock.Text = folderPath;
        }
        else
        {
            // Default to the Downloads folder if no location is saved
            SelectedLocationTextBlock.Text = UserDataPaths.GetDefault().Downloads;

            // Optionally, you can also save the Downloads folder as the default
            localSettings.Values["SaveLocation"] = UserDataPaths.GetDefault().Downloads;
        }
    }

    private async void SelectSaveLocation_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FolderPicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary
        };
        picker.FileTypeFilter.Add("*");  // Add this to avoid an exception

        // Get the current window handle and associate it with the picker
        var window = MainWindow.Instance;
        IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        StorageFolder folder = await picker.PickSingleFolderAsync();
        if (folder != null)
        {
            // Save the selected folder's path in the local settings
            localSettings.Values["SaveLocation"] = folder.Path;
            SelectedLocationTextBlock.Text = folder.Path;
        }
        else
        {
            SelectedLocationTextBlock.Text = "No location selected";
        }
    }

    private void NotificationToasts_Toggled(object sender, RoutedEventArgs e)
    {
        // Handle logic for enabling or disabling notification toasts
    }

    private void NotificationBadge_Toggled(object sender, RoutedEventArgs e)
    {
        // Handle logic for enabling or disabling taskbar badge
    }

    private void NotificationLaunch_Toggled(object sender, RoutedEventArgs e)
    {
        // Handle logic for launching cast window on notification launch
    }

    private void NotificationToggleSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        // Handle logic for toggling notifications
    }


    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        // Reinitialize settings when navigating back to the page
        InitializeSettings();
    }
}
