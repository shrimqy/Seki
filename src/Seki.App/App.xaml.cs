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

namespace Seki.App
{
    public partial class App : Application
    {
        public static bool HandleClosedEvents { get; set; } = true;
        public static TaskCompletionSource? SplashScreenLoadingTCS { get; private set; }

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

                // Example of checking saved devices (replace with your logic)
                var hasSavedDevices = await CheckForSavedDevicesAsync();
                // Initialize and activate MainWindow
                MainWindow.Instance.Activate();

                // Wait for the Window to initialize
                await Task.Delay(10);

                SplashScreenLoadingTCS = new TaskCompletionSource();
                MainWindow.Instance.ShowSplashScreen();

                await Task.Delay(1000);
                if (hasSavedDevices != null)
                {
                    // Wait for the UI to update
                    // Complete the splash screen loading task
                    SplashScreenLoadingTCS.SetResult();
                    SplashScreenLoadingTCS = null;


                    _ = MainWindow.Instance.InitializeApplicationAsync(activatedEventArgs.Data);
                }

                // Hook events for the window
                //MainWindow.Instance.Closed += Window_Closed;
                //MainWindow.Instance.Activated += Window_Activated;

            }
           


            // Initialize MainWindow here
            EnsureWindowIsInitialized();


            _mdnsService = new MdnsService();
            _mdnsService.AdvertiseService();

            _webSocketService = WebSocketService.Instance;
            _webSocketService.Start();
            _clipboardService = new ClipboardService();
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

        private static async Task<DeviceInfo?> CheckForSavedDevicesAsync()
        {
            try
            {
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                StorageFile deviceInfoFile = await localFolder.GetFileAsync("deviceInfo.json");
                string json = await FileIO.ReadTextAsync(deviceInfoFile);
                System.Diagnostics.Debug.WriteLine(json);
                return JsonSerializer.Deserialize<DeviceInfo>(json, options);
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }
    }

}
