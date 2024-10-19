using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Seki.App.Data.Models;
using Seki.App.Helpers;
using Seki.App.Services;
using Seki.App.ViewModels;
using System;


namespace Seki.App.Views
{

    public sealed partial class MainPage : Page
    {
        public MainPageViewModel ViewModel { get; }


        public MainPage()

        {
            this.InitializeComponent();

            Window window = MainWindow.Instance;
            window.ExtendsContentIntoTitleBar = true;  // enable custom titlebar
            window.SetTitleBar(AppTitleBar);      // set user ui element as titlebar

            ViewModel = Ioc.Default.GetRequiredService<MainPageViewModel>();


            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (localSettings.Values["HasOpenedBefore"] == null)
            {
                // Show the TeachingTip if it's the first time
                FirstTimeTeachingTip.IsOpen = true;

                // Set the flag to indicate the app has been opened
                localSettings.Values["HasOpenedBefore"] = true;
            }
        }

        private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            var border = sender as Border;
            var closeButton = FindChild<Button>(border, "CloseButton");
            var moreButton = FindChild<Button>(border, "MoreButton");
            var timeStamp = FindChild<TextBlock>(border, "TimeStampTextBlock");
            if (closeButton != null && timeStamp != null && moreButton != null)
            {
                timeStamp.Visibility = Visibility.Collapsed;
                closeButton.Visibility = Visibility.Visible;
                moreButton.Opacity = 1;
                moreButton.IsHitTestVisible = true;
                closeButton.Opacity = 1;
                closeButton.IsHitTestVisible = true;

            }
        }

        private void OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            var border = sender as Border;
            var closeButton = FindChild<Button>(border, "CloseButton");
            var moreButton = FindChild<Button>(border, "MoreButton");
            var timeStamp = FindChild<TextBlock>(border, "TimeStampTextBlock");

            // Check if MoreButton or its Flyout is open
            if (closeButton != null && timeStamp != null && moreButton != null)
            {
                // If the flyout is open, don't hide the buttons
                var flyout = moreButton.Flyout as MenuFlyout;
                if (flyout != null && flyout.IsOpen)
                {
                    // Flyout is open, so don't change the opacity or hit testing
                    return;
                }

                // Reset the buttons when the flyout is not open
                timeStamp.Visibility = Visibility.Visible;
                closeButton.Opacity = 0;
                closeButton.IsHitTestVisible = false;
                moreButton.Opacity = 0;
                moreButton.IsHitTestVisible = false;
            }
        }

        private void MoreButtonFlyoutClosed(object sender, object e)
        {
            // The sender is the Flyout itself, so first get its parent button
            var flyout = sender as MenuFlyout;
            if (flyout != null)
            {
                // Now get the MoreButton that owns the flyout
                var moreButton = flyout.Target as Button;
                if (moreButton != null)
                {
                    // Find the CloseButton within the same parent (e.g., the same StackPanel or Border)
                    var parent = VisualTreeHelper.GetParent(moreButton) as FrameworkElement;
                    var closeButton = FindChild<Button>(parent, "CloseButton");
                    var timeStamp = FindChild<TextBlock>(parent, "TimeStampTextBlock");

                    if (closeButton != null)
                    {
                        // Reset the opacity and hit testing after the flyout is closed
                        closeButton.Opacity = 0;
                        closeButton.IsHitTestVisible = false;
                        moreButton.Opacity = 0;
                        moreButton.IsHitTestVisible = false;
                        timeStamp.Visibility = Visibility.Visible;
                    }
                }
            }
        }

        private void OnNotificationFilterClick(object sender, RoutedEventArgs e)
        {
            // Retrieve the AppPackage from the Flyout's Tag or DataContext (depending on your binding)
            var menuItem = sender as MenuFlyoutItem;
            if (menuItem != null)
            {
                string? appName = menuItem.Tag as string;  // Assume AppPackage is set as Tag or DataContext

                if (!string.IsNullOrEmpty(appName))
                {
                    // Update the database to set NotificationFilter to DISABLED
                    Seki.App.Data.DataAccess.UpdateNotificationPreference(appName, NotificationFilter.DISABLED);

                    // Optionally, give feedback to the user (e.g., update UI or show a message)
                }
            }
        }

        private void OnDownloadButtonClick(TeachingTip sender, object args)
        {
            // Open the Android app download link
            var uri = new Uri("https://github.com/shrimqy/Seki");
            var success = Windows.System.Launcher.LaunchUriAsync(uri);
        }

        private void OnTeachingTipClosed(TeachingTip sender, TeachingTipClosedEventArgs args)
        {
            // You can perform any additional logic after the TeachingTip is closed, if needed
        }




        private void OnCloseButtonClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var notification = button?.Tag as string;
            System.Diagnostics.Debug.WriteLine(button);
            if (notification != null)
            {
                ViewModel.RemoveNotification(notification);
            }
        }

        private CastWindow? _castWindow;

        private void OnCastWindowClick(object sender, RoutedEventArgs e)
        {
            var command = new Command
            { CommandType = nameof(CommandType.MIRROR) };

            string jsonMessage = SocketMessageSerializer.Serialize(command);
            SocketService.Instance.SendMessage(jsonMessage);
            if (_castWindow == null || _castWindow.AppWindow == null)
            {
                _castWindow = new CastWindow();
                _castWindow.Activate();
            }

        }


        private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            var selectedItem = (NavigationViewItem)args.SelectedItem;
            switch (selectedItem.Tag.ToString())
            {
                case "Home":
                    ContentFrame.Navigate(typeof(HomePage));
                    break;
                case "Settings":
                    ContentFrame.Navigate(typeof(SettingsPage));
                    break;
            }
        }

        // Helper method to find a child element by name
        private T FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T && (child as FrameworkElement).Name == childName)
                {
                    return (T)child;
                }

                var childOfChild = FindChild<T>(child, childName);
                if (childOfChild != null)
                {
                    return childOfChild;
                }
            }

            return null;
        }


    }
}
