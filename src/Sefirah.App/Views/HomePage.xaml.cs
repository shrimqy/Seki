using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Sefirah.App.ViewModels;

namespace Sefirah.App.Views;

public sealed partial class HomePage : Page
{
    public HomeViewModel ViewModel { get; }
    public HomePage()
    {
        this.InitializeComponent();
        ViewModel = Ioc.Default.GetRequiredService<HomeViewModel>();
        this.DataContext = ViewModel; 
    }
}
