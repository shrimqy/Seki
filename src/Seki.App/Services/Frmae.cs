using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seki.App.Services
{
    class Frmae
    {
        //private void OnScreenDataReceived(object? sender, byte[] screenData)
        //{
        //    // Ensure this runs on the UI thread
        //    DispatcherQueue.TryEnqueue(async () =>
        //    {
        //        // Start the stopwatch to measure processing time
        //        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        //        // Display the image using the binary data
        //        var screenBitmap = await ConvertToImageSourceAsync(screenData);
        //        if (screenBitmap != null)
        //        {
        //            ScreenImage.Source = screenBitmap;
        //        }
        //        // Stop the stopwatch and log the time taken
        //        stopwatch.Stop();
        //        System.Diagnostics.Debug.WriteLine($"Time taken to process and display image: {stopwatch.ElapsedMilliseconds} ms");
        //    });
        //}

        private async Task<BitmapImage?> ConvertToImageSourceAsync(byte[] data)
        {
            // Basic validation for the byte array
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
            catch (System.Runtime.InteropServices.COMException comEx)
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
    }
}
