using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Seki.App.Data.Models;
using Microsoft.UI.Windowing;
using Seki.App.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinUIEx;
using Microsoft.UI;
using Seki.App.Helpers;
using Seki.App.Services;
using Windows.System;
using Windows.UI.Core;
using Microsoft.UI.Input;
using Windows.UI.WindowManagement;
using System.Runtime.InteropServices;

namespace Seki.App.Views
{
    public sealed partial class CastWindow : Window
    {
        private static CastWindow? _Instance;
        public static CastWindow Instance => _Instance ??= new();

        public IntPtr WindowHandle { get; }
        public CastWindow()
        {

            WindowHandle = this.GetWindowHandle();
            this.InitializeComponent();

            this.ExtendsContentIntoTitleBar = true;  // enable custom titlebar
            this.SetTitleBar(AppTitleBar);      // set user ui element as titlebar

            MessageHandler.ScreenDataReceived += OnScreenDataReceived;
            MessageHandler.ScreenTimeFrameReceived += OnScreenTimeFrameReceived;

            // Subscribe to the closed event to release resources
            this.Closed += OnWindowClosed;


            // Subscribe to pointer events
            PhoneScreen.PointerPressed += OnPointerPressed;
            PhoneScreen.PointerReleased += OnPointerReleased;
            PhoneScreen.PointerWheelChanged += OnPointerWheelChanged;
            PhoneScreen.PointerMoved += OnPointerMoved;
            PhoneScreen.PointerExited += OnPointerReleased;

            // Hook into the keyboard events
            MainGrid.KeyDown += OnKeyDownHandler;
            MainGrid.KeyUp += OnKeyUpHandler;
            //Window.KeyUp += OnKeyUpHandler;
            SetWindowProperties();

            // Initialize and configure the tap timer
            _tapTimer = new DispatcherTimer();
            _tapTimer.Interval = TimeSpan.FromMilliseconds(400); // Define the tap-and-hold threshold (e.g., 100ms)
            _tapTimer.Tick += OnTapTimerTick;
        }

        private Point _lastPosition;
        private bool _isSwiping = false;
        private bool _isPointerPressed = false;
        private DispatcherTimer? _tapTimer;
        private DateTime _swipeStartTime;
        private bool _isTapAndHold = false;

        private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            // Get the position where the user clicked
            _lastPosition = e.GetCurrentPoint(PhoneScreen).Position;
            _isSwiping = false;
            _isTapAndHold = false;
            _isPointerPressed = true;

            // Prevent duplicate releases
            e.Handled = true;

            // Start the tap timer
            _tapTimer?.Start();
            // Capture the start time of the swipe
            _swipeStartTime = DateTime.Now;
        }
        private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            var currentPosition = e.GetCurrentPoint(PhoneScreen).Position;
            var distanceMoved = Math.Sqrt(Math.Pow(currentPosition.X - _lastPosition.X, 2) + Math.Pow(currentPosition.Y - _lastPosition.Y, 2));
            _isSwiping = true;

            // If pointer moved a significant distance, update the swipe path
            if (distanceMoved > 50 && _isPointerPressed)
            {
                var timeSinceLastMove = (DateTime.Now - _swipeStartTime).TotalMilliseconds;

                // Send the swipe segment with duration
                SendSwipeEndEvent(_lastPosition, currentPosition, timeSinceLastMove, true);

                // Reset the swipe start time and last position for the next segment
                _swipeStartTime = DateTime.Now;
                _lastPosition = currentPosition;
            }
        }

        private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_isPointerPressed) // This ensures it's only processed once
            {
                // Get the current position of the pointer
                var currentPosition = e.GetCurrentPoint(PhoneScreen).Position;

                // Prevent out-of-range or negative values by clamping the position
                var clampedX = Math.Max(0, currentPosition.X);
                var clampedY = Math.Max(0, currentPosition.Y);

                // Update the last position with clamped values
                var clampedPosition = new Point(clampedX, clampedY);

                _isPointerPressed = false;
                _tapTimer?.Stop(); // Stop the timer on release

                // Prevent duplicate releases
                e.Handled = true;

                if (_isSwiping)
                {
                    // Calculate distance moved during the swipe
                    var distanceMoved = Math.Sqrt(Math.Pow(clampedPosition.X - _lastPosition.X, 2) + Math.Pow(clampedPosition.Y - _lastPosition.Y, 2));
                    _isTapAndHold = false;
                    var swipeDuration = (DateTime.Now - _swipeStartTime).TotalMilliseconds;


                    // If swiping, send the final swipe event with clamped positions
                    SendSwipeEndEvent(_lastPosition, clampedPosition, swipeDuration, false);
                }
                else if (_isTapAndHold)
                {
                    // Send a tap-and-hold event with clamped position
                    SendTapAndHoldEvent(_lastPosition.X, _lastPosition.Y);
                }
                else
                {
                    // Register as a single tap with clamped position
                    SendSingleTapEvent(_lastPosition.X, _lastPosition.Y);
                }
            }
        }

        private void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            // Get the scroll delta
            var scrollDelta = e.GetCurrentPoint(PhoneScreen).Properties.MouseWheelDelta;

            // Send the scroll event to Android
            SendScrollEvent(scrollDelta);
        }

        private HashSet<VirtualKey> pressedKeys = [];

        private void OnKeyDownHandler(object sender, KeyRoutedEventArgs e)
        {
            var key = e.Key;
            var originalKey = (int)e.OriginalKey;

            // Prevent multiple KeyDown events for the same key
            if (pressedKeys.Contains(key))
            {
                return; // Ignore if key is already pressed
            }

            // Add the key to the pressed set
            pressedKeys.Add(key);

            // Check if Control or Shift keys are pressed
            bool isControlPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            bool isShiftPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);

            // Map the key to text or action
            var (mappedKey, isAction) = KeyMapper.MapWindowsKeyToTextOrAction(key, originalKey, isShiftPressed, isControlPressed, false);

            if (string.IsNullOrEmpty(mappedKey))
            {
                System.Diagnostics.Debug.WriteLine(e.OriginalKey);
                return;
            }

            // Send the correct type of message depending on whether it's a text input or an action
            if (isAction)
            {
                SendKeyboardAction(mappedKey);
            }
            else
            {
                SendKeyEvent(mappedKey);
            }

            e.Handled = true;
        }

        private void OnKeyUpHandler(object sender, KeyRoutedEventArgs e)
        {
            var key = e.Key;

            // Remove the key from the pressed set
            if (pressedKeys.Contains(key))
            {
                pressedKeys.Remove(key);
            }

            e.Handled = true;
        }

        private void SendKeyEvent(string key)
        {
            var keyboardEvent = new KeyEvent
            {
                Key = key,
            };

            var message = new InteractiveControlMessage
            {
                Type = SocketMessageType.InteractiveControlMessage,
                Control = keyboardEvent
            };
            string jsonMessage = SocketMessageSerializer.Serialize(message);
            WebSocketService.Instance.SendMessage(jsonMessage);
        }

        private void SendKeyboardAction(string action)
        {
            var keyboardAction = new KeyboardAction
            {
                KeyboardActionType = action
            };

            var message = new InteractiveControlMessage
            {
                Type = SocketMessageType.InteractiveControlMessage,
                Control = keyboardAction
            };

            string jsonMessage = SocketMessageSerializer.Serialize(message);
            WebSocketService.Instance.SendMessage(jsonMessage);
        }

        private void OnTapTimerTick(object sender, object e)
        {
            _isTapAndHold = true; // If the timer ticked, it's a tap-and-hold
            _tapTimer?.Stop(); // Stop the timer after it fires
        }

        private void SendSingleTapEvent(double x, double y)
        {
            var tapEvent = new SingleTapEvent
            {
                X = x,
                Y = y,
                FrameWidth = PhoneScreen.ActualWidth,
                FrameHeight = PhoneScreen.ActualHeight,
            };

            var message = new InteractiveControlMessage
            {
                Type = SocketMessageType.InteractiveControlMessage,
                Control = tapEvent
            };
            string jsonMessage = SocketMessageSerializer.Serialize(message);
            WebSocketService.Instance.SendMessage(jsonMessage);
        }

        private void SendTapAndHoldEvent(double x, double y)
        {
            var tapAndHoldEvent = new HoldTapEvent
            {
                X = x,
                Y = y,
                FrameWidth = PhoneScreen.ActualWidth,
                FrameHeight = PhoneScreen.ActualHeight,
            };

            var message = new InteractiveControlMessage
            {
                Type = SocketMessageType.InteractiveControlMessage,
                Control = tapAndHoldEvent
            };
            string jsonMessage = SocketMessageSerializer.Serialize(message);
            WebSocketService.Instance.SendMessage(jsonMessage);
        }

        private void SendSwipeEndEvent(Point start, Point end, double duration, bool willContinue)
        {
            // Logic to send swipe path to Android
            var swipeEvent = new SwipeEvent
            {
                StartX = start.X,
                StartY = start.Y,
                EndX = end.X,
                EndY = end.Y,
                WillContinue = willContinue,
                FrameWidth = PhoneScreen.ActualWidth,
                FrameHeight = PhoneScreen.ActualHeight,
                Duration = duration
            };
            var message = new InteractiveControlMessage
            {
                Type = SocketMessageType.InteractiveControlMessage,
                Control = swipeEvent
            };
            string jsonMessage = SocketMessageSerializer.Serialize(message);
            WebSocketService.Instance.SendMessage(jsonMessage);

            if (!willContinue)
            {
                _isSwiping = false;
            }
        }

        private void SendScrollEvent(int delta)
        {
            var scrollDirection = delta > 0 ? nameof(ScrollDirection.UP) : nameof(ScrollDirection.DOWN);

            var scrollEvent = new ScrollEvent
            {
                ScrollDirection = scrollDirection 
            };

            var message = new InteractiveControlMessage
            {
                Type = SocketMessageType.InteractiveControlMessage,
                Control = scrollEvent
            };
            string jsonMessage = SocketMessageSerializer.Serialize(message);
            WebSocketService.Instance.SendMessage(jsonMessage);
        }

        private void SetWindowProperties()
        {
            Title = "Cast Window";
            SetWindowIconBasedOnTheme();

            // Set window size
            var windowId = Win32Interop.GetWindowIdFromWindow(WindowHandle);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

            if (appWindow is not null)
            {
                appWindow.Resize(new Windows.Graphics.SizeInt32(400, 760)); // Adjust size as needed
            }

            // Listen for theme changes
            if (this.Content is FrameworkElement frameworkElement)
            {
                frameworkElement.ActualThemeChanged += OnThemeChanged;
            }
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

            var windowId = Win32Interop.GetWindowIdFromWindow(WindowHandle);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            appWindow.SetIcon(iconPath);
        }

        // Variable to store the received screen time
        private long _lastScreenTimeFrame;

        // This method handles the reception of the screen time frame from the Android device
        private void OnScreenTimeFrameReceived(object? sender, ScreenData data)
        {

            // Convert the received time (in milliseconds) to DateTime
            _lastScreenTimeFrame = data.TimeStamp;
        }
        private void OnScreenDataReceived(object? sender, byte[] screenData)
        {
            // Get the current system time as Unix time in milliseconds
            long currentUnixTimeInMilliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            // Calculate the difference between the current time and the last screen time frame
            var timeDifference = currentUnixTimeInMilliseconds - _lastScreenTimeFrame;

            // Set your threshold (in milliseconds). For example, 100 ms
            const int threshold = 500;

            // Check if the time difference is within the acceptable range
            if (timeDifference > threshold)
            {
                // Skip processing if the time difference is too large
                System.Diagnostics.Debug.WriteLine($"Skipping frame due to time difference: {timeDifference} ms");
                return;
            }

            // Ensure this runs on the UI thread
            DispatcherQueue.TryEnqueue(async () =>
            {
                // Start the stopwatch to measure processing time
                //var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                // Display the image using the binary data
                var screenBitmap = await ConvertToImageSourceAsync(screenData);
                if (screenBitmap != null)
                {
                    PhoneScreen.Source = screenBitmap;
                }

                // Stop the stopwatch and log the time taken
                //stopwatch.Stop();
                //System.Diagnostics.Debug.WriteLine($"Time taken to process and display image: {stopwatch.ElapsedMilliseconds} ms");
            });
        }

        private async Task<BitmapImage?> ConvertToImageSourceAsync(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                System.Diagnostics.Debug.WriteLine("Invalid screen data: byte array is null or empty.");
                return null; // Skip this frame
            }

            using var stream = new MemoryStream(data);
            var bitmapImage = new BitmapImage();
            try
            {
                await bitmapImage.SetSourceAsync(stream.AsRandomAccessStream());
            }
            catch (COMException comEx)
            {
                // Handle the COMException specifically and skip the frame
                System.Diagnostics.Debug.WriteLine($"COMException: Failed to set image source: {comEx.Message}");
                System.Diagnostics.Debug.WriteLine($"Exception StackTrace: {comEx.StackTrace}");
                return null; // Skip this frame and continue
            }
            catch (Exception ex)
            {
                // Handle any other unexpected exceptions
                System.Diagnostics.Debug.WriteLine($"Error setting image source: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Exception StackTrace: {ex.StackTrace}");
                return null; // Skip this frame and continue
            }

            return bitmapImage;
        }

        private void OnWindowClosed(object sender, WindowEventArgs args)
        {
            // Example WebSocket message
            SendWindowClosedMessage();

            // Release any resources or cleanup
            CleanUpResources();
        }

        private void SendWindowClosedMessage()
        {
            var closeEvent = new Command
            {
                CommandType = nameof(CommandType.CLOSE_MIRROR),
                Type = SocketMessageType.CommandType
            };
            string jsonMessage = SocketMessageSerializer.Serialize(closeEvent);
            WebSocketService.Instance.SendMessage(jsonMessage);
        }

        private void CleanUpResources()
        {
            // Unsubscribe from events to prevent memory leaks
            MessageHandler.ScreenDataReceived -= OnScreenDataReceived;
            MessageHandler.ScreenTimeFrameReceived -= OnScreenTimeFrameReceived;
            PhoneScreen.PointerPressed -= OnPointerPressed;
            PhoneScreen.PointerReleased -= OnPointerReleased;
            PhoneScreen.PointerWheelChanged -= OnPointerWheelChanged;
            PhoneScreen.PointerMoved -= OnPointerMoved;
            PhoneScreen.PointerExited -= OnPointerReleased;

            // Stop any timers or background tasks
            _tapTimer?.Stop();
            _tapTimer = null;

            // Clear any other resources that may still be active
            // Example: If there are any ongoing tasks or open connections, dispose them here
        }
    }

}
