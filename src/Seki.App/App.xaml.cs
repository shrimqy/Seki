﻿using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Seki.App.Data.Models;
using Seki.App.Helpers;
using Seki.App.Services;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Storage;

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

        public FileTransferService _fileTransferService = new FileTransferService();

        public static ClipboardService ClipboardService => ClipboardService.Instance;
        public App()
        {
            InitializeComponent();
            CheckStartupSetting();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _ = ActivateAsync();

            async Task ActivateAsync()
            {
                // Get AppActivationArgumentsTask
                var activatedEventArgs = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().GetActivatedEventArgs();
                System.Diagnostics.Debug.WriteLine(activatedEventArgs);
                // Handle share activation
                var kind = activatedEventArgs.Kind;
                if (kind == ExtendedActivationKind.ShareTarget)
                {
                    System.Diagnostics.Debug.WriteLine("Share Target Received");
                    HandleShareTargetActivation(activatedEventArgs.Data as ShareTargetActivatedEventArgs);
                }

                var isStartupTask = activatedEventArgs.Data is Windows.ApplicationModel.Activation.IStartupTaskActivatedEventArgs;


                // Hook events for the window
                EnsureWindowIsInitialized();

                // Only activate if not already open
                if (!MainWindow.Instance.AppWindow.IsVisible)
                {
                    MainWindow.Instance.Activate();
                }

                // Wait for the Window to fully initialize
                await Task.Delay(10);


                _ = ClipboardService;

                // Show Splash Screen
                SplashScreenLoadingTCS = new TaskCompletionSource<bool>();
                MainWindow.Instance.ShowSplashScreen();

                // Hook events for the window
                //MainWindow.Instance.Closed += Window_Closed;
                MainWindow.Instance.Activated += Window_Activated;

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
                // Initialize the main application after splash screen completes
                _ = MainWindow.Instance.InitializeApplicationAsync(activatedEventArgs.Data);

                System.Diagnostics.Debug.WriteLine("await for task started");
                await SplashScreenLoadingTCS.Task;
                if (SplashScreenLoadingTCS.Task.Result)
                {
                    System.Diagnostics.Debug.WriteLine("inside if of result");
                    _ = MainWindow.Instance.InitializeApplicationAsync(activatedEventArgs.Data);
                }
            }
        }

        public async Task HandleShareTargetActivation(ShareTargetActivatedEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine("Share Target Received");
            var shareOperation = args.ShareOperation;


            await MainWindow.Instance.DispatcherQueue.EnqueueAsync(async () =>
            {
                await _fileTransferService.ProcessShareAsync(shareOperation);
            });
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

        private void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (args.WindowActivationState == WindowActivationState.CodeActivated ||
                args.WindowActivationState == WindowActivationState.PointerActivated)
                return;
            ApplicationData.Current.LocalSettings.Values["INSTANCE_ACTIVE"] = -Environment.ProcessId;
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

        private void OnDeviceInfoReceived(Device? deviceInfo)
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

            // Check if ClipboardSync is enabled in local settings
            var isClipboardSyncEnabled = (bool?)ApplicationData.Current.LocalSettings.Values["ClipboardSync"] ?? false;
            if (!isClipboardSyncEnabled)
            {
                System.Diagnostics.Debug.WriteLine("ClipboardSync is disabled.");
                return; // Do not proceed if ClipboardSync is disabled
            }

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
                System.Diagnostics.Debug.WriteLine(jsonMessage);
                _webSocketService.SendMessage(jsonMessage);
            }
        }

        private static async Task<List<Device>?> CheckForSavedDevicesAsync()
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            var deviceInfoFile = await localFolder.TryGetItemAsync("deviceInfo.json") as StorageFile;

            // Check if the file exists
            if (deviceInfoFile == null)
            {
                // Create the file with default content
                deviceInfoFile = await localFolder.CreateFileAsync("deviceInfo.json", CreationCollisionOption.OpenIfExists);
                await FileIO.WriteTextAsync(deviceInfoFile, "[]"); // Initialize with an empty JSON array
            }

            // Read the file's contents
            string jsonData = await FileIO.ReadTextAsync(deviceInfoFile);

            // Deserialize the JSON into a List<Device>
            if (!string.IsNullOrWhiteSpace(jsonData))
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = true
                };

                var deviceList = JsonSerializer.Deserialize<List<Device>>(jsonData, options);
                return deviceList ?? new List<Device>(); // Return the list or an empty list if null
            }

            return new List<Device>(); // Return an empty list if jsonData is empty or null
        }
    }

}
