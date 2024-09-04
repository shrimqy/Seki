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
using System.Linq;

namespace Seki.App
{
    public partial class App : Application
    {

        public static bool HandleClosedEvents { get; set; } = true;
        public static TaskCompletionSource<bool>? SplashScreenLoadingTCS { get; private set; }


        public new static App Current
             => (App)Application.Current;
        private WebSocketService? _webSocketService;
        private PlaybackService? _playbackService;
        private MdnsService? _mdnsService;
        public static ClipboardService ClipboardService => ClipboardService.Instance;
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

                // Manage startup task and minimize if necessary
                await HandleStartupTaskAsync(isStartupTask);

                // Hook events for the window
                EnsureWindowIsInitialized();

                // Initialize and activate MainWindow
                MainWindow.Instance.Activate();

                // Wait for the Window to fully initialize
                await Task.Delay(10);

                _ = ClipboardService;

                // Show Splash Screen
                SplashScreenLoadingTCS = new TaskCompletionSource<bool>();
                MainWindow.Instance.ShowSplashScreen();

            // Configure the DI container
            var host = AppLifeCycleHelper.ConfigureHost();
            Ioc.Default.ConfigureServices(host.Services);

            _mdnsService = new MdnsService();
            await _mdnsService.AdvertiseServiceAsync();

            _webSocketService = WebSocketService.Instance;
            _webSocketService.Start();

            _webSocketService.DeviceInfoReceived += OnDeviceInfoReceived;

            await InitializePlaybackServiceAsync();

            ClipboardService.ClipboardContentChanged += OnClipboardContentChanged;

                
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
            MainWindow.Instance.Closed += Window_Closed;
        }


        public static async Task OnActivatedAsync(AppActivationArguments activatedEventArgs)
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
            // Check if MainWindow.Instance and AppWindow are not null
            if (MainWindow.Instance != null && MainWindow.Instance.AppWindow != null)
            {
                // Hide the window instead of closing it
            MainWindow.Instance.AppWindow.Hide();

                // Prevent the default close operation
                args.Handled = true;
            }

            Thread.Yield();
        }

        private async Task InitializePlaybackServiceAsync()
        {
            System.Diagnostics.Debug.WriteLine("Starting PlaybackService initialization");
            await PlaybackService.Instance.InitializeAsync();
            PlaybackService.Instance.PlaybackDataChanged += OnPlaybackDataChanged;
            System.Diagnostics.Debug.WriteLine("PlaybackService initialization completed");
        }


        private static JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private void OnPlaybackDataChanged(object sender, PlaybackData playbackData)
        {
            var playbackMessage = new PlaybackData
            {
                Type = SocketMessageType.PlaybackData,
                AppName = playbackData.AppName,
                Artist = playbackData.Artist,
                IsPlaying = playbackData.IsPlaying,
                Thumbnail = playbackData.Thumbnail,
                TrackTitle = playbackData.TrackTitle,
                Volume = playbackData.Volume
            };
            string jsonMessage = SocketMessageSerializer.Serialize(playbackMessage);
            _webSocketService?.SendMessage(jsonMessage);
        }

        private void OnDeviceInfoReceived(DeviceInfo? deviceInfo)
        {
            if (SplashScreenLoadingTCS?.Task.IsCompleted == false || SplashScreenLoadingTCS?.Task == null)
            {
                // Complete the splash screen loading task
                SplashScreenLoadingTCS?.SetResult(true);
                System.Diagnostics.Debug.WriteLine("event triggered");
            }
        }

        private void OnClipboardContentChanged(object? sender, string? content)
        {
            System.Diagnostics.Debug.WriteLine("Clipboard triggered");
            if (content != null && _webSocketService != null)
            {
                var clipboardMessage = new ClipboardMessage
                {
                    Type = SocketMessageType.Clipboard,
                    Content = content
                };
                System.Diagnostics.Debug.WriteLine("clipboard: " + clipboardMessage.Content);
                string jsonMessage = SocketMessageSerializer.Serialize(clipboardMessage);
                _webSocketService.SendMessage(jsonMessage);
            }
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
