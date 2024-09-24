using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;


namespace Seki.App.Views
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            this.InitializeComponent();
        }

        private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            var selectedItem = (NavigationViewItem)args.SelectedItem;
            switch (selectedItem.Tag.ToString())
            {
                case "GeneralPage":
                    SettingsContentFrame.Navigate(typeof(Settings.GeneralPage));
                    break;
                case "DevicesPage":
                    SettingsContentFrame.Navigate(typeof(Settings.DevicesPage));
                    break;
                case "AboutPage":
                    SettingsContentFrame.Navigate(typeof(Settings.AboutPage));
                    break;
            }
        }
    }
}
