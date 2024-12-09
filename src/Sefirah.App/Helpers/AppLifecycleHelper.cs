using Microsoft.Extensions.Hosting;
using Sefirah.App.Utils;
using Seki.App.ViewModels.Settings;
using Serilog;
using Windows.ApplicationModel;
using Sefirah.App.RemoteStorage;
using Sefirah.App;
using Sefirah.App.ViewModels;
using Sefirah.App.Data.Contracts;
using Sefirah.App.ViewModels.Settings;
using Sefirah.App.Services;
using Sefirah.App.RemoteStorage.Shell;
using Sefirah.App.RemoteStorage.Worker;

namespace Sefirah.App.Helpers;


/// <summary>
/// Provides static helper to manage app lifecycle.
/// </summary>
public static class AppLifecycleHelper
{
    internal static void CloseApp()
    {
        MainWindow.Instance.Close();
    }

    /// <summary>
    /// Gets application package version.
    /// </summary>
    public static Version AppVersion { get; } =
        new(Package.Current.Id.Version.Major, Package.Current.Id.Version.Minor, Package.Current.Id.Version.Build, Package.Current.Id.Version.Revision);


    /// <summary>
    /// Initializes the app components.
    /// </summary>
    public static async Task InitializeAppComponentsAsync()
    {
        Debug.WriteLine("Starting server...");
        var mdnsService = Ioc.Default.GetRequiredService<IMdnsService>();
        var socketService = Ioc.Default.GetRequiredService<ISocketService>();
        var playbackService = Ioc.Default.GetRequiredService<IPlaybackService>();
        // Start socket server first
        await socketService.StartServerAsync();
        // Then start mDNS service after socket server is ready
        await Task.WhenAll(
            mdnsService.AdvertiseServiceAsync(),
            // Finally initialize playback
            playbackService.InitializeAsync()
        );
        mdnsService.StartDiscovery();
    }


    /// <summary>
    /// Configures DI (dependency injection) container.
    /// </summary>
    public static IHost ConfigureHost()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) => services

                // Remote Storage
                .AddSftpRemoteServices()
                .AddCloudSyncWorker()

                // Shell
                .AddCommonClassObjects()
                .AddSingleton<ShellRegistrar>()
                .AddHostedService<ShellWorker>()

                .AddHostedService<SyncProviderWorker>()
                .AddSingleton<ISftpService, SftpService>()

                // Logging
                .AddSingleton<Utils.ILogger>(new SerilogWrapperLogger(Log.Logger))
                // Services
                .AddSingleton<IDeviceManager, DeviceManager>()
                .AddSingleton<ISocketService, SocketService>()
                .AddSingleton(sp => (ISessionManager)sp.GetRequiredService<ISocketService>())
                .AddSingleton<IMdnsService, MdnsService>()
                .AddSingleton<IClipboardService, ClipboardService>()
                .AddSingleton<IPlaybackService, PlaybackService>()
                .AddSingleton<INotificationService, NotificationService>()

                .AddScoped<IMessageHandler, MessageHandler>()
                .AddSingleton<Func<IMessageHandler>>(sp => () => sp.GetRequiredService<IMessageHandler>())

                // ViewModels
                .AddSingleton<MainPageViewModel>()
                .AddSingleton<HomeViewModel>()
                .AddSingleton<GeneralViewModel>()
                .AddSingleton<DevicesViewModel>()
                .AddSingleton<CastWindowViewModel>()
                .AddSingleton<FeaturesViewModel>()
            ).Build();
    }
}
