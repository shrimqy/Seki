using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Sefirah.App;
using Sefirah.App.ViewModels;
using Sefirah.App.ViewModels.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Sefirah.App.Views.Settings;

public sealed partial class GeneralPage : Page
{
    private ApplicationDataContainer localSettings;
    private bool _isInitializing;

    public GeneralPage()
    {
        this.InitializeComponent();

        // Get the application local settings
        localSettings = ApplicationData.Current.LocalSettings;

        // Initialize the toggle switches based on saved preferences
        InitializeSettings();
        LoadSavedLocation();
    }

    // Load the stored preferences when the page is loaded
    private void InitializeSettings()
    {
        _isInitializing = true; // Flag to indicate we're in the initialization phase

        if (localSettings.Values.TryGetValue("Startup", out object? startup))
        {
            StartupToggleSwitch.IsOn = (bool)startup;
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
        localSettings.Values["ClipboardSync"] = toggleSwitch.IsOn;
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

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        // Reinitialize settings when navigating back to the page
        InitializeSettings();
    }
}
