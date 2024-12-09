using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Imaging;
using Sefirah.App.Data.Models;
using System.IO;
using System.Runtime.InteropServices;

namespace Sefirah.App.ViewModels
{
    public sealed class CastWindowViewModel : ObservableObject
    {
        private readonly DispatcherQueue _dispatcher;
        private BitmapImage? _phoneScreenImage;

        public CastWindowViewModel()
        {
            _dispatcher = DispatcherQueue.GetForCurrentThread();
        }

        // Property to hold the screen bitmap
        public BitmapImage? PhoneScreenImage
        {
            get => _phoneScreenImage;
            private set => SetProperty(ref _phoneScreenImage, value); // Notify the UI when the image changes
        }

        // Variable to store the received screen time
        private long _lastScreenTimeFrame;

        private void ScreenTimeFrameReceived(object? sender, ScreenData data)
        {
            _lastScreenTimeFrame = data.TimeStamp;
        }

        private void ScreenDataReceived(object? sender, byte[] screenData)
        {
            long currentUnixTimeInMilliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var timeDifference = currentUnixTimeInMilliseconds - _lastScreenTimeFrame;

            const int threshold = 500;

            if (timeDifference > threshold)
            {
                Debug.WriteLine($"Skipping frame due to time difference: {timeDifference} ms");
                return;
            }

            _dispatcher.TryEnqueue(async () =>
            {
                Debug.WriteLine($"time difference: {timeDifference} ms");
                var screenBitmap = await ConvertToImageSourceAsync(screenData);
                if (screenBitmap != null)
                {
                    PhoneScreenImage = screenBitmap; // Update the bound property
                }
            });
        }

        private async Task<BitmapImage?> ConvertToImageSourceAsync(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                Debug.WriteLine("Invalid screen data: byte array is null or empty.");
                return null;
            }

            try
            {
                using var stream = new MemoryStream(data);
                var bitmapImage = new BitmapImage();
                await bitmapImage.SetSourceAsync(stream.AsRandomAccessStream());
                return bitmapImage;
            }
            catch (COMException comEx)
            {
                Debug.WriteLine($"COMException: Failed to set image source: {comEx.Message}");
                Debug.WriteLine($"Exception StackTrace: {comEx.StackTrace}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting image source: {ex.Message}");
                Debug.WriteLine($"Exception StackTrace: {ex.StackTrace}");
            }

            return null;
        }
    }
}
