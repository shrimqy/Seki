using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Sefirah.App.Utils;
using Sefirah.App.Views;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using WinUIEx;

namespace Sefirah.App;

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
        PersistenceId = "SefirahMainWindow";

        // Set minimum sizes
        MinHeight = 416;
        MinWidth = 516;

        AppWindow.Title = "Sefirah";
        AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
        AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        var theme = Application.Current.RequestedTheme;
        string iconPath;

        // Set icon path based on light/dark theme
        if (theme == ApplicationTheme.Dark)
        {
            iconPath = "Assets/SekiDark.ico";
        }
        else
        {
            iconPath = "Assets/SekiLight.ico";
        }

        AppWindow.SetIcon(iconPath);

        // Listen for theme changes
        if (Content is FrameworkElement frameworkElement)
        {
            frameworkElement.ActualThemeChanged += OnThemeChanged;
        }

        // Workaround for full screen window messing up the taskbar
        // https://github.com/microsoft/microsoft-ui-xaml/issues/8431
        //InteropHelpers.SetPropW(WindowHandle, "NonRudeHWND", new IntPtr(1));
    }

    private void OnThemeChanged(FrameworkElement sender, object args)
    {
        SetWindowIconBasedOnTheme();
    }

    private void SetWindowIconBasedOnTheme()
    {
        var theme = Application.Current.RequestedTheme;
        string iconPath;

        // Set icon path based on light/dark theme
        if (theme == ApplicationTheme.Dark)
        {
            iconPath = "Assets/SekiDark.ico";
        }
        else
        {
            iconPath = "Assets/SekiLight.ico";
        }

        AppWindow.SetIcon(iconPath);
    }

    public void ShowSplashScreen()
    {
        var rootFrame = EnsureWindowIsInitialized();

        rootFrame.Navigate(typeof(Views.SplashScreen));
    }


    public Task InitializeApplicationAsync(object activatedEventArgs)
    {
        var logger = Ioc.Default.GetRequiredService<ILogger>();

        logger.Debug("Debugging");

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