using CommunityToolkit.WinUI;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Seki.App.Data.Models;
using Seki.App.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer.ShareTarget;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using System.Net.Http;
using Seki.App.Helpers;

namespace Seki.App.Services
{
    public class FileTransferService
    {
        private static FileTransferService? _instance;
        public static FileTransferService Instance => _instance ??= new FileTransferService();

        private FileMetadata? currentFileMetadata;
        private FileStream? currentFileStream;
        private readonly string downloadFolder;

        public FileTransferService()
        {
            // Load the saved location from local settings or use the default Downloads folder
            downloadFolder = LoadPreferredDownloadFolder();
        }


        private string LoadPreferredDownloadFolder()
        {
            // Access the application's local settings
            var localSettings = ApplicationData.Current.LocalSettings;

            // Try to load the saved path from local settings
            if (localSettings.Values.TryGetValue("SaveLocation", out object savedPath) && savedPath is string folderPath && !string.IsNullOrEmpty(folderPath))
            {
                return folderPath; // Use the user-saved folder
            }
            else
            {
                // Default to the Downloads folder if no location is saved
                var defaultDownloadsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

                // Optionally, you can also save the Downloads folder as the default
                localSettings.Values["SaveLocation"] = defaultDownloadsFolder;

                return defaultDownloadsFolder;
            }
        }

        public async Task HandleFileTransfer(FileTransfer message)
        {
            switch (message.TransferType)
            {
                case nameof(FileTransferType.WEBSOCKET):
                    HandleWebSocketTransfer(message);
                    break;
                case nameof(FileTransferType.HTTP):
                    // await HandleHttpTransfer(message);
                    break;
                case nameof(FileTransferType.P2P):
                    throw new NotImplementedException("P2P file transfer is not implemented yet.");
                default:
                    throw new ArgumentException($"Unknown transfer type: {message.TransferType}");
            }
        }

        public void HandleWebSocketTransfer(FileTransfer message)
        {
            if (message.Metadata != null)
            {
                // Start a new file transfer
                currentFileMetadata = message.Metadata;
                string filePath = Path.Combine(downloadFolder, currentFileMetadata.FileName);
                currentFileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Received file transfer message without metadata");
            }
        }

        public void ReceiveFileData(byte[] data, int offset, int count)
        {
            if (currentFileStream == null || currentFileMetadata == null)
            {
                System.Diagnostics.Debug.WriteLine("Received file data without metadata or file stream is not initialized");
                return;
            }

            currentFileStream.Write(data, offset, count);

            if (currentFileStream.Length >= currentFileMetadata.FileSize)
            {
                // File transfer complete
                SaveFile();
            }
        }

        private void SaveFile()
        {
            if (currentFileStream == null || currentFileMetadata == null)
            {
                return;
            }

            // Close and dispose of the file stream
            currentFileStream.Close();
            currentFileStream.Dispose();
            currentFileStream = null;

            System.Diagnostics.Debug.WriteLine($"File saved to {Path.Combine(downloadFolder, currentFileMetadata.FileName)}");

            var appNotification = new AppNotificationBuilder()
                .AddText("New File Received", new AppNotificationTextProperties().SetMaxLines(1))
                .AddText($"File saved to {Path.Combine(downloadFolder, currentFileMetadata.FileName)}")
                .BuildNotification();
            appNotification.ExpiresOnReboot = true;
            AppNotificationManager.Default.Show(appNotification);

            // Clean up metadata
            currentFileMetadata = null;
        }

        public void AbortFileTransfer()
        {
            if (currentFileStream != null)
            {
                currentFileStream.Close();
                currentFileStream = null;
            }

            if (currentFileMetadata != null)
            {
                string filePath = Path.Combine(downloadFolder, currentFileMetadata.FileName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                currentFileMetadata = null;
            }

            System.Diagnostics.Debug.WriteLine("File transfer aborted and resources cleaned up.");
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

        public async Task SendFileViaWebSocket(StorageFile file)
        {
            try
            {
                const int ChunkSize = 512 * 1024; // 512 * 1024 KB chunks
                var buffer = new Windows.Storage.Streams.Buffer((uint)ChunkSize);
                using (var stream = await file.OpenReadAsync())
                {
                    // Prepare file metadata
                    var fileMetadata = new FileMetadata
                    {
                        FileName = file.Name,
                        FileType = file.FileType,
                        FileSize = ((long)stream.Size),
                        Uri = file.Path // Assuming Uri is the file path, adjust if needed
                    };

                    // Prepare FileTransfer object for metadata
                    var metadataTransfer = new FileTransfer
                    {
                        TransferType = "Metadata",
                        Metadata = fileMetadata
                    };

                    // Send metadata
                    string metadataJson = JsonSerializer.Serialize(metadataTransfer);
                    await MainWindow.Instance.DispatcherQueue.EnqueueAsync(() =>
                        WebSocketService.Instance.SendMessage(metadataJson));

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

                        // Prepare FileTransfer object for chunk data
                        var chunkTransfer = new FileTransfer
                        {
                            TransferType = "Chunk",
                            chunkData = Convert.ToBase64String(chunk) // Base64 encoding here
                        };

                        string jsonMessage = SocketMessageSerializer.Serialize(chunkTransfer);

                        // Send chunk data
                        await MainWindow.Instance.DispatcherQueue.EnqueueAsync(() =>
                            WebSocketService.Instance.SendMessage(jsonMessage));

                        totalBytesRead += readBuffer.Length;

                        // Report progress (optional)
                        double progress = (double)totalBytesRead / stream.Size;
                        await MainWindow.Instance.DispatcherQueue.EnqueueAsync(() => ReportProgress(progress));
                    }
                }

                // Send completion message
                var completionTransfer = new FileTransfer
                {
                    TransferType = "Completed"
                };
                await MainWindow.Instance.DispatcherQueue.EnqueueAsync(() =>
                    WebSocketService.Instance.SendMessage(JsonSerializer.Serialize(completionTransfer)));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in SendFileViaWebSocket: {ex}");
            }
        }

        public void ReportProgress(double progress)
        {
            // Implement progress reporting logic here
            // For example, update a progress bar in your UI
            System.Diagnostics.Debug.WriteLine($"File transfer progress: {progress:P}");
        }
    }
}

