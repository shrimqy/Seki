﻿using Microsoft.Extensions.Options;
using Seki.App.Data.Models;
using Seki.App.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace Seki.App.Utils
{
    public static class MessageHandler
    {
        public static event EventHandler<StorageInfo>? StorageInfoReceived;

        public static event EventHandler<byte[]>? ScreenDataReceived;

        public static event EventHandler<ScreenData>? ScreenTimeFrameReceived;
        public static void HandleMessage(SocketMessage message, SekiSession session)
        {
            switch (message.Type)
            {
                case SocketMessageType.Clipboard:
                    HandleClipboardMessage((ClipboardMessage)message, session);
                    break;
                case SocketMessageType.Notification:
                    HandleNotificationMessage((NotificationMessage)message);
                    break;
                case SocketMessageType.Response:
                    HandleResponseMessage((Response)message);
                    break;
                case SocketMessageType.DeviceInfo:
                    HandleDeviceInfoMessage((DeviceInfo)message, session);
                    break;
                case SocketMessageType.DeviceStatus:
                    HandleDeviceStatusMessage((DeviceStatus)message, session);
                    break;
                case SocketMessageType.PlaybackData:
                    HandlePlaybackDataMessage((PlaybackData)message);
                    break;
                case SocketMessageType.CommandType:
                    HandleCommandMessage((Command)message);
                    break;
                case SocketMessageType.FileTransferType:
                    HandleFileTransfer((FileTransfer)message, session);
                    break;
                case SocketMessageType.StorageInfo:
                    HandleStorageInfo((StorageInfo)message, session);
                    break;
                case SocketMessageType.ScreenData:
                    HandleScreenData((ScreenData)message);
                    break;
                default:
                    System.Diagnostics.Debug.WriteLine($"Unknown message type: {message.Type}");
                    session.SendMessage(new Response { Content = "Unknown message type" });
                    break;
            }
        }


        public static void HandleBinaryData(byte[] data)
        {
            ScreenDataReceived?.Invoke(null, data);
        }

        private static void HandleScreenData(ScreenData message)
        {
            ScreenTimeFrameReceived?.Invoke(null, message);
        }

        private static void HandleStorageInfo(StorageInfo message, SekiSession session)
        {
            StorageInfoReceived?.Invoke(null, message);
        }

        private static async void HandleFileTransfer(FileTransfer message, SekiSession session)
        {
            await FileTransferService.Instance.HandleFileTransfer(message);
        }

        private static async void HandleCommandMessage(Command message)
        {
            if (message.CommandType != null)
            {
                await CommandService.Instance.HandleCommandMessageAsync(message);
            }
        }

        private static async void HandlePlaybackDataMessage(PlaybackData message)
        {
            if (message.MediaAction != null)
            {
                await PlaybackService.Instance.HandleMediaActionAsync(message);
            }
        }

        private static async void HandleClipboardMessage(ClipboardMessage message, SekiSession session)
        {
            if (message.Content != null)
            {
                await ClipboardService.Instance.SetContentAsync(message.Content);
            }
        }

        private static void HandleNotificationMessage(NotificationMessage message)
        {
            NotificationService.ShowDesktopNotification(message);
        }

        private static void HandleResponseMessage(Response message)
        {
            System.Diagnostics.Debug.WriteLine($"Received response: {message.ResType}, {message.Content}");
        }

        private static async Task HandleDeviceInfoMessage(DeviceInfo message, SekiSession session)
        {
            System.Diagnostics.Debug.WriteLine($"Received device info: {message.DeviceName}");

            var currentUserInfo = new CurrentUserInformation();
            var (username, avatar) = await currentUserInfo.GetCurrentUserInfoAsync();

            // Current Machine's Device Info
            var deviceInfo = new DeviceInfo
            {
                DeviceName = username,
                UserAvatar = avatar,
            };
            WebSocketService.Instance.SendMessage(JsonSerializer.Serialize<DeviceInfo>(deviceInfo));

            // Received Device Info
            var windowsDeviceInfo = new Device
            {
                Name = message.DeviceName,
                LastConnected = DateTime.Now,
            };

            ((SekiServer)session.Server).OnDeviceInfoReceived(windowsDeviceInfo);

            // Load the existing devices from the file
            var localFolder = ApplicationData.Current.LocalFolder;
            var deviceInfoFile = await localFolder.TryGetItemAsync("deviceInfo.json") as StorageFile;

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };

            List<Device> deviceList = new List<Device>();

            if (deviceInfoFile != null)
            {
                // Read and deserialize the existing device info JSON file
                string json = await FileIO.ReadTextAsync(deviceInfoFile);

                // If the JSON is not empty, deserialize it to a List<Device>
                if (!string.IsNullOrWhiteSpace(json))
                {
                    try
                    {
                        deviceList = JsonSerializer.Deserialize<List<Device>>(json, options) ?? new List<Device>();
                    }
                    catch (JsonException ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Message Handler, Error deserializing device info: {ex.Message}");
                        // Handle the deserialization error (e.g., fallback to an empty list)
                    }
                }
            }

            // Check if a device with the same name already exists in the list
            var existingDevice = deviceList.FirstOrDefault(d => d.Name == message.DeviceName);

            if (existingDevice != null)
            {
                // Update the existing device information
                existingDevice.LastConnected = DateTime.Now;
                System.Diagnostics.Debug.WriteLine($"Updated device info for: {existingDevice.Name}");
            }
            else
            {
                // Add the new device information if it doesn't exist
                deviceList.Add(windowsDeviceInfo);
                System.Diagnostics.Debug.WriteLine($"Added new device info for: {windowsDeviceInfo.Name}");
            }

            // Serialize the updated list back to JSON
            string updatedJson = JsonSerializer.Serialize(deviceList, options);

            // Save the updated JSON to the file
            StorageFile saveFile = deviceInfoFile ?? await localFolder.CreateFileAsync("deviceInfo.json", CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(saveFile, updatedJson);

            System.Diagnostics.Debug.WriteLine($"Device info saved successfully.");
        }

        private static void HandleDeviceStatusMessage(DeviceStatus message, SekiSession session)
        {
            System.Diagnostics.Debug.WriteLine($"Received device status: {message.BatteryStatus}");
            ((SekiServer)session.Server).OnDeviceStatusReceived(message);
        }
    }
}
