using H.NotifyIcon;
using Microsoft.UI.Xaml.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;


namespace Seki.App.Views.SystemTray
{
    [ObservableObject]
    public sealed partial class SystemTray : UserControl
    {

        [ObservableProperty]
        private bool _isWindowVisible;


        public SystemTray()
        {
            InitializeComponent();
        }

        [RelayCommand]
        public void ShowHideWindow()
        {
            var window = MainWindow.Instance;
            if (window == null)
            {
                return;
            }

            if (window.Visible)
            {
                window.Hide();
            }
            else
            {
                window.Show();
            }
            IsWindowVisible = window.Visible;
        }

        [RelayCommand]
        public void ExitApplication()
        {
            App.HandleClosedEvents = false;
            TrayIcon.Dispose();
            MainWindow.Instance?.Close();
        }
    }

}
