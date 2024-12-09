using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Sefirah.App.Views.Settings;

public sealed partial class AboutPage : Page
{
    public AboutPage()
    {
        this.InitializeComponent();
    }

    private async void OpenGitHubRepo_Click(object sender, RoutedEventArgs e)
    {
        var uri = new Uri("https://github.com/shrimqy/Seki");
        await Windows.System.Launcher.LaunchUriAsync(uri);
    }

}
