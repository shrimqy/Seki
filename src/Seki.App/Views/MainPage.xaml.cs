using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Seki.App.ViewModels;


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
        }
    }
    


}
