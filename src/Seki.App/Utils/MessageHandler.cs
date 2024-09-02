using Microsoft.Extensions.Options;
﻿using Seki.App.Data.Models;
using Seki.App.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace Seki.App.Utils
{
    public static class MessageHandler
    {
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
                default:
                    System.Diagnostics.Debug.WriteLine($"Unknown message type: {message.Type}");
                    session.SendMessage(new Response { Content = "Unknown message type" });
                    break;
            }
        }

        private static async void HandleFileTransfer(FileTransfer message, SekiSession session)
        {
            session._fileTransferService.HandleFileTransfer(message);
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
            ((SekiServer)session.Server).OnDeviceInfoReceived(message);
            var currentUserInfo = new CurrentUserInformation();
            var (username, avatar) = await currentUserInfo.GetCurrentUserInfoAsync();

            // Create and send DeviceInfo
            var deviceInfo = new DeviceInfo
            {
                DeviceName = username,
                UserAvatar = avatar,
            };
            WebSocketService.Instance.SendMessage(JsonSerializer.Serialize<DeviceInfo>(deviceInfo));
        }

        private static void HandleDeviceStatusMessage(DeviceStatus message, SekiSession session)
        {
            System.Diagnostics.Debug.WriteLine($"Received device status: {message.BatteryStatus}");
            ((SekiServer)session.Server).OnDeviceStatusReceived(message);
        }
    }
}
