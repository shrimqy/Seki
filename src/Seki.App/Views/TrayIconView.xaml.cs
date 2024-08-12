using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;


namespace Seki.App.Views
{
    [ObservableObject]
    public sealed partial class TrayIconView : UserControl
    {
        [ObservableProperty]
        private bool _isWindowVisible;

        public TrayIconView()
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
                window.AppWindow.Hide();
            }
            else
            {
                window.AppWindow.Show();
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
