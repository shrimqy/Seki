using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Seki.App.ViewModels.Settings;

namespace Seki.App.Views.Settings
{
    public sealed partial class DevicesPage : Page
    {

        public DevicesViewModel ViewModel { get; }
        public DevicesPage()
        {
            this.InitializeComponent();
            ViewModel = Ioc.Default.GetRequiredService<DevicesViewModel>();
        }
    }
}
