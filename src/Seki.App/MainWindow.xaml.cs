using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Seki.App.Views;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using WinUIEx;
namespace Seki.App
{
    public sealed partial class MainWindow : WindowEx
    {

        private static MainWindow? _Instance;
        public static MainWindow Instance => _Instance ??= new();

        public IntPtr WindowHandle { get; }

        private MainWindow()
        {
            WindowHandle = this.GetWindowHandle();

            InitializeComponent();

            EnsureEarlyWindow();
        }

        private void EnsureEarlyWindow()
        {
            // Set PersistenceId
            PersistenceId = "SekiMainWindow";

            // Set minimum sizes
            MinHeight = 416;
            MinWidth = 516;

            AppWindow.Title = "Seki";
            AppWindow.SetIcon("ms-appx:///Assets/logo-winui.png");
            AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

            // Workaround for full screen window messing up the taskbar
            // https://github.com/microsoft/microsoft-ui-xaml/issues/8431
            //InteropHelpers.SetPropW(WindowHandle, "NonRudeHWND", new IntPtr(1));
        }

        public void ShowSplashScreen()
        {
            var rootFrame = EnsureWindowIsInitialized();

            rootFrame.Navigate(typeof(Views.SplashScreen));
        }


        public Task InitializeApplicationAsync(object activatedEventArgs)
        {
            var rootFrame = EnsureWindowIsInitialized();

            switch (activatedEventArgs)
            {
                case ILaunchActivatedEventArgs launchArgs:
                    if (launchArgs != null)
                    {
                        rootFrame.Navigate(typeof(MainPage), null, new SuppressNavigationTransitionInfo());
                    }
                    break;
                default:
                    // Just launch the app with no arguments
                    rootFrame.Navigate(typeof(MainPage), null, new SuppressNavigationTransitionInfo());
                    break;
            }

            if (!AppWindow.IsVisible)
            {
                // When resuming the cached instance
                AppWindow.Show();
                Activate();
            }

            return Task.CompletedTask;
        }

        public Frame EnsureWindowIsInitialized()
        {
            //  NOTE:
            //  Do not repeat app initialization when the Window already has content,
            //  just ensure that the window is active
            if (Instance.Content is not Frame rootFrame)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new() { CacheSize = 1 };
                rootFrame.NavigationFailed += OnNavigationFailed;

                // Place the frame in the current Window
                Instance.Content = rootFrame;
            }

            return rootFrame;
        }
        private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
            => new Exception("Failed to load Page " + e.SourcePageType.FullName);

    }
}