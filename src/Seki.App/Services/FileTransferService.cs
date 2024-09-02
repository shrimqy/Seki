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
    public class FileTransferService
    {
        private static FileTransferService? _instance;
        public static FileTransferService Instance => _instance ??= new FileTransferService();

        private readonly string downloadFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        private FileMetadata? currentFileMetadata;
        private FileStream? currentFileStream;

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

            currentFileStream.Close();
            System.Diagnostics.Debug.WriteLine($"File saved to {Path.Combine(downloadFolder, currentFileMetadata.FileName)}");

            // Clean up
            currentFileStream = null;
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
    }
}

