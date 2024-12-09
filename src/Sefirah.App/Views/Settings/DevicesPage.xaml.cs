using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Sefirah.App.ViewModels.Settings;

namespace Sefirah.App.Views.Settings;

public sealed partial class DevicesPage : Page
{

    public DevicesViewModel ViewModel { get; }
    public DevicesPage()
    {
        this.InitializeComponent();
        ViewModel = Ioc.Default.GetRequiredService<DevicesViewModel>();
    }
}
