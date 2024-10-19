using Microsoft.Extensions.Options;
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

        public static event EventHandler<Device>? DeviceInfoReceived;
        public static event EventHandler<DeviceStatus>? DeviceStatusReceived;

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
                    _ = HandleDeviceInfoMessage((DeviceInfo)message, session);
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
            Debug.WriteLine($"Received response: {message.ResType}, {message.Content}");
        }

        private static async Task HandleDeviceInfoMessage(DeviceInfo message, SekiSession session)
        {
            Debug.WriteLine($"Received device info: {message.DeviceName}");

            var currentUserInfo = new CurrentUserInformation();
            var (username, avatar) = await currentUserInfo.GetCurrentUserInfoAsync();

            // Current Machine's Device Info
            var deviceInfo = new DeviceInfo
            {
                DeviceId = Environment.MachineName,
                DeviceName = username,
                UserAvatar = avatar,
            };
            _ = SocketService.Instance.SendMessage(JsonSerializer.Serialize<DeviceInfo>(deviceInfo));

            // Received Device Info
            var receivedDeviceInfo = new Device
            {
                Name = message.DeviceName,
                LastConnected = DateTime.Now,
            };

            // Load the existing devices from the file
            var localFolder = ApplicationData.Current.LocalFolder;
            var deviceListFile = await localFolder.TryGetItemAsync("deviceList.json") as StorageFile;

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };

            List<Device> deviceList = []; // Initialize an empty list

            if (deviceListFile != null)
            {
                // Read and deserialize the existing device info JSON file
                string json = await FileIO.ReadTextAsync(deviceListFile);

                if (!string.IsNullOrWhiteSpace(json))
                {
                    try
                    {
                        // Deserialize device list 
                        var receivedDevices = JsonSerializer.Deserialize<List<Device>>(json, options);

                        // Add or update the device in the list if the deserialization was successful
                        if (receivedDevices != null)
                        {
                            deviceList = receivedDevices;

                            // Check if the device already exists in the list
                            var existingDevice = deviceList.FirstOrDefault(d => d.Name == receivedDeviceInfo.Name);

                            if (existingDevice != null)
                            {
                                // Update the existing device information
                                existingDevice.LastConnected = DateTime.Now;
                                Debug.WriteLine($"Updated device info for: {existingDevice.Name}");
                            }
                            else
                            {
                                // Add the new device if it doesn't exist
                                deviceList.Add(receivedDeviceInfo);
                                Debug.WriteLine($"Added new device info for: {receivedDeviceInfo.Name}");
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        Debug.WriteLine($"Message Handler, Error deserializing device info: {ex.Message}");
                        // Fallback to empty list in case of deserialization error
                    }
                }
            }
            else
            {
                Debug.WriteLine("deviceList.json not found. Creating a new device list.");
            }

            // Add the new device if it hasn't been added already
            if (!deviceList.Any(d => d.Name == receivedDeviceInfo.Name))
            {
                deviceList.Add(receivedDeviceInfo);
            }

            // Serialize the updated list back to JSON
            string updatedJson = JsonSerializer.Serialize(deviceList, options);

            // Create the file if it doesn't exist, or overwrite it if it does
            StorageFile saveFile = deviceListFile ?? await localFolder.CreateFileAsync("deviceList.json", CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(saveFile, updatedJson);

            DeviceInfoReceived?.Invoke(null, receivedDeviceInfo);

            Debug.WriteLine($"Device info saved successfully {updatedJson}");
        }

        private static void HandleDeviceStatusMessage(DeviceStatus message, SekiSession session)
        {
            Debug.WriteLine($"Received device status: {message.BatteryStatus}");
            DeviceStatusReceived?.Invoke(null, message);
        }
    }
}
