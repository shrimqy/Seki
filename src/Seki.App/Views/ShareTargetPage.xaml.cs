using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer.ShareTarget;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Seki.App.Services;
using CommunityToolkit.WinUI;

namespace Seki.App.Views
{

    public sealed partial class ShareTargetPage : Page
    {
        public ShareTargetPage()
        {
            this.InitializeComponent();
        }

        public async Task ProcessShareAsync(ShareOperation shareOperation)
        {
            try
            {
                if (shareOperation.Data.Contains(StandardDataFormats.StorageItems))
                {
                    var items = await shareOperation.Data.GetStorageItemsAsync();
                    foreach (var item in items)
                    {
                        if (item is StorageFile file)
                        {
                            await SendFileViaWebSocket(file);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ProcessShareAsync: {ex}");
            }
            finally
            {
                shareOperation.ReportCompleted();
            }
        }

        private async Task SendFileViaWebSocket(StorageFile file)
        {
            try
            {
                const int ChunkSize = 1024 * 1024; // 1MB chunks
                var buffer = new Windows.Storage.Streams.Buffer((uint)ChunkSize);

                using (var stream = await file.OpenReadAsync())
                {
                    // Send file metadata
                    var metadata = new
                    {
                        Type = "FileTransfer",
                        FileName = file.Name,
                        FileSize = stream.Size,
                        ChunkSize = ChunkSize
                    };
                    string metadataJson = JsonSerializer.Serialize(metadata);
                    await MainWindow.Instance.DispatcherQueue.EnqueueAsync(() => WebSocketService.Instance.SendMessage(metadataJson));

                    // Send file contents in chunks
                    Windows.Storage.Streams.IBuffer readBuffer;
                    long totalBytesRead = 0;
                    while ((readBuffer = await stream.ReadAsync(buffer, (uint)ChunkSize, Windows.Storage.Streams.InputStreamOptions.None)).Length > 0)
                    {
                        // Convert IBuffer to byte array
                        byte[] chunk = new byte[readBuffer.Length];
                        using (var dataReader = Windows.Storage.Streams.DataReader.FromBuffer(readBuffer))
                        {
                            dataReader.ReadBytes(chunk);
                        }

                        // Convert byte array to Base64 string for sending
                        string base64Chunk = Convert.ToBase64String(chunk);
                        await MainWindow.Instance.DispatcherQueue.EnqueueAsync(() => WebSocketService.Instance.SendMessage(base64Chunk));

                        totalBytesRead += readBuffer.Length;

                        // Report progress (optional)
                        double progress = (double)totalBytesRead / stream.Size;
                        await MainWindow.Instance.DispatcherQueue.EnqueueAsync(() => ReportProgress(progress));
                    }
                }

                // Send completion message
                await MainWindow.Instance.DispatcherQueue.EnqueueAsync(() =>
                    WebSocketService.Instance.SendMessage(JsonSerializer.Serialize(new { Type = "FileTransfer", Status = "Completed" })));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in SendFileViaWebSocket: {ex}");
            }
        }

        private void ReportProgress(double progress)
        {
            // Implement progress reporting logic here
            // For example, update a progress bar in your UI
            System.Diagnostics.Debug.WriteLine($"File transfer progress: {progress:P}");
        }
    }
}
