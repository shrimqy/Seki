using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Sefirah.App.Data.Enums;
using Sefirah.App.Data.Models;
using System.IO;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.ShareTarget;
using Windows.Storage;

namespace Sefirah.App.Services
{
    public class FileTransferService
    {
        private static FileTransferService? _instance;
        public static FileTransferService Instance => _instance ??= new FileTransferService();

        private FileMetadata? currentFileMetadata;
        private FileStream? currentFileStream;
        private readonly string downloadFolder;

        private string? currentFilePath;

        private string? FilePath;

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

        public void PrepareFileTransfer(FileTransfer fileTransfer)
        {
            currentFileMetadata = fileTransfer.Metadata;
            if (currentFileMetadata != null)
            {
                currentFilePath = Path.Combine(downloadFolder, currentFileMetadata.FileName);
                // Ensure the file name is unique
                int counter = 1;
                while (File.Exists(currentFilePath))
                {
                    string fileNameWithoutExt = Path.GetFileNameWithoutExtension(currentFileMetadata.FileName);
                    string extension = Path.GetExtension(currentFileMetadata.FileName);
                    currentFilePath = Path.Combine(downloadFolder, $"{fileNameWithoutExt}({counter}){extension}");
                    counter++;
                }
            }
        }

        public FileStream CreateFileStream()
        {
            if (currentFilePath == null)
            {
                throw new InvalidOperationException("File transfer not prepared. Call PrepareFileTransfer first.");
            }

            currentFileStream = new FileStream(currentFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
            return currentFileStream;
        }

        public void FinalizeFileTransfer()
        {
            currentFileStream?.Dispose();
            currentFileStream = null;
            currentFileMetadata = null;
            currentFilePath = null;
        }

        public async Task HandleFileTransfer(FileTransfer message)
        {
            switch (message.TransferType)
            {
                case nameof(FileTransferType.WEBSOCKET):
                    await HandleWebSocketTransfer(message);
                    break;
                case nameof(FileTransferType.HTTP):
                    // await HandleHttpTransfer(message);
                    break;
                default:
                    throw new ArgumentException($"Unknown transfer type: {message.TransferType}");
            }
        }

        public async Task HandleWebSocketTransfer(FileTransfer message)
        {
            switch (message.DataTransferType)
            {
                case nameof(DataTransferType.METADATA):
                    await handleMetadata(message.Metadata);
                    break;
                case nameof(DataTransferType.CHUNK):
                    await ReceiveFileData(message.ChunkData);
                    break;
            }
        }
        long totalBytesRead = 0;
        public async Task handleMetadata(FileMetadata metadata)
        {
            if (metadata != null)
            {
                currentFileMetadata = metadata;
                Debug.WriteLine("Metadata received: " + metadata.FileName + " Size: " + metadata.FileSize);

                FilePath = Path.Combine(downloadFolder, currentFileMetadata.FileName);

                try
                {
                    totalBytesRead = 0;
                    currentFileStream = new FileStream(FilePath, FileMode.Create, FileAccess.Write, FileShare.None);
                    Debug.WriteLine($"File stream created for {currentFileMetadata.FileName} at {FilePath}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error creating file stream: {ex.Message}");
                }
            }
            else
            {
                Debug.WriteLine("Metadata is null");
            }
        }

        public async Task ReceiveFileData(string base64Data)
        {
            if (currentFileStream == null || currentFileMetadata == null)
            {
                Debug.WriteLine("Received file data without metadata or file stream is not initialized");
                return;
            }

            try
            {
                Debug.WriteLine("Chunk Processing");

                // Decode the Base64 string to a byte array
                byte[] fileData = Convert.FromBase64String(base64Data);

                // Write the byte array to the file stream
                await currentFileStream.WriteAsync(fileData, 0, fileData.Length);

                // Report progress (optional)

                totalBytesRead += fileData.Length;
                double progress = (double)totalBytesRead / currentFileMetadata.FileSize;
                Debug.WriteLine($"File transfer progress: {progress:P}");

                // Check if the file transfer is complete
                if (currentFileStream.Length >= currentFileMetadata.FileSize)
                {
                    // File transfer complete
                    await SaveFile();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error receiving file data: {ex.Message}");
            }
        }

        private async Task SaveFile()
        {
            if (currentFileStream == null || currentFileMetadata == null)
            {
                return;
            }

            // Close and dispose of the file stream
            currentFileStream.Close();
            currentFileStream.Dispose();
            currentFileStream = null;
            totalBytesRead = 0;

            Debug.WriteLine($"File saved to {Path.Combine(downloadFolder, currentFileMetadata.FileName)}");

            var appNotification = new AppNotificationBuilder()
                .AddText("New File Received", new AppNotificationTextProperties().SetMaxLines(1))
                .AddText($"File saved to {Path.Combine(downloadFolder, currentFileMetadata.FileName)}")
                .BuildNotification();
            appNotification.ExpiresOnReboot = true;
            AppNotificationManager.Default.Show(appNotification);


            var isClipboardFileEnabled = (bool?)ApplicationData.Current.LocalSettings.Values["ClipboardFiles"] ?? false;
            if (isClipboardFileEnabled)
            {
                // Add the saved file to the clipboard
                var dataPackage = new DataPackage();
                var file = await StorageFile.GetFileFromPathAsync(FilePath);
                dataPackage.SetStorageItems(new List<StorageFile> { file });
                Clipboard.SetContent(dataPackage);

                Debug.WriteLine($"File {currentFileMetadata.FileName} added to clipboard.");
            }

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

            Debug.WriteLine("File transfer aborted and resources cleaned up.");
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
                            //await SendFileViaWebSocket(file);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ProcessShareAsync: {ex}");
            }
            finally
            {
                shareOperation.ReportCompleted();
            }
        }

        public async Task SendFile(StorageFile file)
        {
            try
            {

                const int ChunkSize = 1024 * 1024;
                var buffer = new Windows.Storage.Streams.Buffer(ChunkSize);
                using var stream = await file.OpenReadAsync();
                // Prepare file metadata
                var fileMetadata = new FileMetadata
                {
                    FileName = file.Name,
                    MimeType = file.ContentType,
                    FileSize = (long)stream.Size,
                    Uri = file.Path // Assuming Uri is the file path, adjust if needed
                };

                // Prepare FileTransfer object for metadata
                var metadataTransfer = new FileTransfer
                {
                    TransferType = nameof(FileTransferType.TCP),
                    DataTransferType = nameof(DataTransferType.METADATA),
                    Metadata = fileMetadata
                };

                // Send metadata
                //string metadataJson = JsonSerializer.Serialize(metadataTransfer);
                //await MainWindow.Instance.DispatcherQueue.EnqueueAsync(() =>
                //    SocketService.Instance.SendMessage(metadataJson));

                // Send file contents in chunks
                Windows.Storage.Streams.IBuffer readBuffer;
                long totalBytesRead = 0;
                while ((readBuffer = await stream.ReadAsync(buffer, ChunkSize, Windows.Storage.Streams.InputStreamOptions.None)).Length > 0)
                {
                    // Convert IBuffer to byte array
                    byte[] chunk = new byte[readBuffer.Length];
                    using (var dataReader = Windows.Storage.Streams.DataReader.FromBuffer(readBuffer))
                    {
                        dataReader.ReadBytes(chunk);
                    }

                    //await MainWindow.Instance.DispatcherQueue.EnqueueAsync(() =>
                    //    SocketService.Instance.SendMessage("F \n"));


                    //await MainWindow.Instance.DispatcherQueue.EnqueueAsync(() =>
                    //    SocketService.Instance.SendFile(chunk));

                    totalBytesRead += readBuffer.Length;

                    // Report progress (optional)
                    double progress = (double)totalBytesRead / stream.Size;
                    Debug.WriteLine($"File transfer progress: {progress:P}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in SendFileViaWebSocket: {ex}");
            }
        }
    }
}

