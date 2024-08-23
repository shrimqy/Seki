using HttpMultipartParser;
using NetCoreServer;
using Org.BouncyCastle.Utilities;
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

namespace Seki.App.Services
{
    public class FileTransferService()
    {

        private static FileTransferService? _instance;
        public static FileTransferService Instance => _instance ??= new FileTransferService();
        readonly string downloadFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        private const int HttpServerPort = 8686;
        readonly string ipAddress = NetworkHelper.GetLocalIPAddress();

        private FileMetadata metadata;

        public async Task HandleFileTransfer(FileTransfer message)
        {
            switch (message.TransferType)
            {
                case nameof(FileTransferType.WEBSOCKET):
                    HandleWebSocketTransfer(message);
                    break;
                case nameof(FileTransferType.HTTP):
                    //await HandleHttpTransfer(message);
                    break;
                case nameof(FileTransferType.P2P):
                    throw new NotImplementedException("P2P file transfer is not implemented yet.");
                default:
                    throw new ArgumentException($"Unknown transfer type: {message.TransferType}");
            }
        }

        //private async Task HandleWebSocketTransfer(FileTransfer message)
        //{
        //    if (string.IsNullOrEmpty(message.Data) || message.Metadata == null)
        //    {
        //        throw new ArgumentException("Invalid WebSocket transfer: missing data or metadata.");
        //    }
        //    byte[] fileData = Convert.FromBase64String(message.Data);
        //    string filePath = Path.Combine(downloadFolder, message.Metadata.FileName);
        //    await File.WriteAllBytesAsync(filePath, fileData);
        //    System.Diagnostics.Debug.WriteLine($"File received and saved: {filePath}");
        //}


        private FileMetadata? currentFileMetadata;
        private MemoryStream? currentFileStream;

        public void HandleWebSocketTransfer(FileTransfer message)
        {
            if (message.Metadata != null)
            {
                // Start a new file transfer
                currentFileMetadata = message.Metadata;
                currentFileStream = new MemoryStream();
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
                System.Diagnostics.Debug.WriteLine("Received file data without metadata");
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

            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), currentFileMetadata.FileName);

            using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                currentFileStream.Seek(0, SeekOrigin.Begin);
                currentFileStream.CopyTo(fileStream);
            }

            System.Diagnostics.Debug.WriteLine($"File saved: {filePath}");

            // Clean up
            currentFileStream.Dispose();
            currentFileStream = null;
            currentFileMetadata = null;
        }
    }
}

