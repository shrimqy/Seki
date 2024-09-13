using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Seki.App.Data.Models;
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
            var timeStamp = FindChild<TextBlock>(border, "TimeStampTextBlock");
            if (closeButton != null && timeStamp != null)
            {
                timeStamp.Visibility = Visibility.Collapsed;
                closeButton.Visibility = Visibility.Visible;
                closeButton.Opacity = 1;
                closeButton.IsHitTestVisible = true;

            }
        }

        private void OnDownloadButtonClick(TeachingTip sender, object args)
        {
            // Open the Android app download link
            var uri = new Uri("https://github.com/shrimqy/Sekia");
            var success = Windows.System.Launcher.LaunchUriAsync(uri);
        }

        private void OnTeachingTipClosed(TeachingTip sender, TeachingTipClosedEventArgs args)
        {
            // You can perform any additional logic after the TeachingTip is closed, if needed
        }

        private void OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            var border = sender as Border;
            var closeButton = FindChild<Button>(border, "CloseButton");
            var timeStamp = FindChild<TextBlock>(border, "TimeStampTextBlock");
            if (closeButton != null && timeStamp != null)
            {
                timeStamp.Visibility = Visibility.Visible;
                closeButton.Opacity = 0;
                closeButton.IsHitTestVisible = false;
            }
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


        private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            var selectedItem = (NavigationViewItem)args.SelectedItem;
            switch (selectedItem.Tag.ToString())
            {
                case "Settings":
                    ContentFrame.Navigate(typeof(SettingsPage));
                    break;
                case "DevicesPage":
                    ContentFrame.Navigate(typeof(Settings.DevicesPage));
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
