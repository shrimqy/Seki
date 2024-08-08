using Seki.App.Extensions;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Seki.App.Services;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using System;
using Seki.App.Data.Models;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using CommunityToolkit.Mvvm.DependencyInjection;
using Seki.App.Helpers;

namespace Seki.App
{
    public partial class App : Application
    {

        public static bool HandleClosedEvents { get; set; } = true;
        public static TaskCompletionSource<bool>? SplashScreenLoadingTCS { get; private set; }


        public new static App Current
             => (App)Application.Current;

        private ClipboardService? _clipboardService;
        private WebSocketService? _webSocketService;
        private MdnsService? _mdnsService;
        public App()
        {
            InitializeComponent();
        }
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {

            _ = ActivateAsync();

            async Task ActivateAsync()
            {
                // Get AppActivationArguments
                var activatedEventArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
                var isStartupTask = activatedEventArgs.Data is Windows.ApplicationModel.Activation.IStartupTaskActivatedEventArgs;

                // Initialize and activate MainWindow
                MainWindow.Instance.Activate();

                // Wait for the Window to fully initialize
                await Task.Delay(10);

                // Show Splash Screen
                SplashScreenLoadingTCS = new TaskCompletionSource<bool>();
                MainWindow.Instance.ShowSplashScreen();

                // Configure the DI container
                var host = AppLifeCycleHelper.ConfigureHost();
                Ioc.Default.ConfigureServices(host.Services);

                _mdnsService = new MdnsService();
                _mdnsService.AdvertiseService();

                _webSocketService = WebSocketService.Instance;
                _webSocketService.Start();

                WebSocketService.Instance.DeviceInfoReceived += OnDeviceInfoReceived;
                _clipboardService = new ClipboardService();

                var hasSavedDevices = await CheckForSavedDevicesAsync();
                if (hasSavedDevices != null)
                {
                    // Initialize the main application after splash screen completes
                    _ = MainWindow.Instance.InitializeApplicationAsync(activatedEventArgs.Data);
                }
                System.Diagnostics.Debug.WriteLine("await for task started");
                await SplashScreenLoadingTCS.Task;
                if (SplashScreenLoadingTCS.Task.Result)
                {
                    System.Diagnostics.Debug.WriteLine("inside if of result");
                    _ = MainWindow.Instance.InitializeApplicationAsync(activatedEventArgs.Data);
                }
                // Hook events for the window
                EnsureWindowIsInitialized();
            }
        }

        private void EnsureWindowIsInitialized()
        {
            MainWindow.Instance.Closed += (sender, args) =>
            {
                if (HandleClosedEvents)
                {
                    args.Handled = true;
                    MainWindow.Instance.AppWindow.Hide();
                }
            };
            MainWindow.Instance.Activated += Window_Activated;
            //Window.Closed += Window_Closed;
        }

        public async Task OnActivatedAsync(AppActivationArguments activatedEventArgs)
        {
            var activatedEventArgsData = activatedEventArgs.Data;
            // Called from Program class

            // InitializeApplication accesses UI, needs to be called on UI thread
            await MainWindow.Instance.DispatcherQueue.EnqueueAsync(()
                => MainWindow.Instance.InitializeApplicationAsync(activatedEventArgsData));
        }

        private void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (args.WindowActivationState == WindowActivationState.CodeActivated ||
                args.WindowActivationState == WindowActivationState.PointerActivated)
                return;
            ApplicationData.Current.LocalSettings.Values["INSTANCE_ACTIVE"] = -Environment.ProcessId;
        }
        private void Window_Closed(object sender, WindowEventArgs args)
        {
            // Cache the window instead of closing it
            MainWindow.Instance.AppWindow.Hide();

            Thread.Yield();
        }


        private static JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private void OnDeviceInfoReceived(DeviceInfo? deviceInfo)
        {
            // Complete the splash screen loading task
            SplashScreenLoadingTCS?.SetResult(true);
            System.Diagnostics.Debug.WriteLine("event triggered");
        }

        private static async Task<DeviceInfo?> CheckForSavedDevicesAsync()
        {
            try
            {
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                StorageFile deviceInfoFile = await localFolder.GetFileAsync("deviceInfo.json");
                string json = await FileIO.ReadTextAsync(deviceInfoFile);
                return JsonSerializer.Deserialize<DeviceInfo>(json, options);
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }
    }

}
