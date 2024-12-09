using CommunityToolkit.WinUI;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Sefirah.App.Data.LocalDatabase;
using Serilog;
using System.IO;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Storage;

namespace Sefirah.App;

public partial class App : Application
{

    public static bool HandleClosedEvents { get; set; } = true;
    public static TaskCompletionSource<bool>? SplashScreenLoadingTCS { get; private set; }

    public new static App Current
         => (App)Application.Current;
    public App()
    {
        InitializeComponent();
        Log.Logger = GetSerilogLogger();
        CheckStartupSetting();
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        _ = ActivateAsync();

        async Task ActivateAsync()
        {
            // Get AppActivationArgumentsTask
            var activatedEventArgs = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().GetActivatedEventArgs();
            // Handle share activation
            var kind = activatedEventArgs.Kind;
            if (kind == ExtendedActivationKind.ShareTarget)
            {
                Debug.WriteLine("Share Target Received");
                HandleShareTargetActivation(activatedEventArgs.Data as ShareTargetActivatedEventArgs);
            }

            var isStartupTask = activatedEventArgs.Data is IStartupTaskActivatedEventArgs;


            // Hook events for the window
            EnsureWindowIsInitialized();

            // Only activate if not already open
            if (!MainWindow.Instance.AppWindow.IsVisible)
            {
                MainWindow.Instance.Activate();
            }



            // Wait for the Window to fully initialize
            await Task.Delay(10);


            // Show Splash Screen
            SplashScreenLoadingTCS = new TaskCompletionSource<bool>();
            MainWindow.Instance.ShowSplashScreen();

            Debug.WriteLine("Hooked Windows...");

            // Hook events for the window
            //MainWindow.Instance.Closed += Window_Closed;
            MainWindow.Instance.Activated += Window_Activated;
            // Configure the DI container
            var host = AppLifecycleHelper.ConfigureHost();
            Ioc.Default.ConfigureServices(host.Services);

            await host.StartAsync();

            Debug.WriteLine("Initializing app components...");
            await AppLifecycleHelper.InitializeAppComponentsAsync();

            // Initialize database after DI is setup
            await DataAccess.InitializeDatabase();

            var localSettings = ApplicationData.Current.LocalSettings;

            // Initialize the main application after splash screen completes
            _ = MainWindow.Instance.InitializeApplicationAsync(activatedEventArgs.Data);

            //await SplashScreenLoadingTCS.Task;
            //if (SplashScreenLoadingTCS.Task.Result)
            //{
            //    _ = MainWindow.Instance.InitializeApplicationAsync(activatedEventArgs.Data);
            //}


        }
    }


    private void EnsureWindowIsInitialized()
    {
        MainWindow.Instance.Closed += (sender, args) =>
        {
            if (HandleClosedEvents)
            {
                // If HandleClosedEvents is true, we hide the window (tray icon exit logic can change this)
                args.Handled = true;
                MainWindow.Instance.AppWindow.Hide();
            }
        };
    }


    private void Window_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState == WindowActivationState.CodeActivated ||
            args.WindowActivationState == WindowActivationState.PointerActivated)
            return;
        ApplicationData.Current.LocalSettings.Values["INSTANCE_ACTIVE"] = -Environment.ProcessId;
    }

    /// <summary>
    /// Gets invoked when the application is activated.
    /// </summary>
    public async Task OnActivatedAsync(AppActivationArguments activatedEventArgs)
    {
        var activatedEventArgsData = activatedEventArgs.Data;

        // InitializeApplication accesses UI, needs to be called on UI thread
        await MainWindow.Instance.DispatcherQueue.EnqueueAsync(()
            => MainWindow.Instance.InitializeApplicationAsync(activatedEventArgsData));
    }


    public async Task HandleShareTargetActivation(ShareTargetActivatedEventArgs args)
    {
        System.Diagnostics.Debug.WriteLine("Share Target Received");
        var shareOperation = args.ShareOperation;


        await MainWindow.Instance.DispatcherQueue.EnqueueAsync(async () =>
        {
            //await _fileTransferService.ProcessShareAsync(shareOperation);
        });
    }


    private static Serilog.ILogger GetSerilogLogger()
    {
        string logFilePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "Seki.Logs/Log.log");

        var logger = new LoggerConfiguration()
            .MinimumLevel
#if DEBUG
            .Verbose()
#else
				.Error()
#endif
            .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
            .WriteTo.Debug()
				.CreateLogger();

			return logger;
		}

    private async void CheckStartupSetting()
    {
        // Fetch the startup setting from the local settings
        if (ApplicationData.Current.LocalSettings.Values.TryGetValue("Startup", out var isStartupEnabled))
        {
            bool enableStartup = (bool)isStartupEnabled;

            // Call HandleStartupTaskAsync with the user's preference
            await HandleStartupTaskAsync(enableStartup);
        }
        else
        {
            // If there's no setting, set it to false by default (do not start at startup)
            ApplicationData.Current.LocalSettings.Values["Startup"] = false;
            await HandleStartupTaskAsync(false);
        }
    }

    private static async Task HandleStartupTaskAsync(bool isStartupTask)
    {
        // Only proceed if the startup task should be enabled
        if (isStartupTask)
        {
            StartupTask startupTask = await StartupTask.GetAsync("8B5D3E3F-9B69-4E8A-A9F7-BFCA793B9AF0");
            // Ensure the startup task is enabled
            if (startupTask.State == StartupTaskState.DisabledByUser || startupTask.State == StartupTaskState.Disabled)
            {
                await startupTask.RequestEnableAsync();
            }
        }
    }
}
