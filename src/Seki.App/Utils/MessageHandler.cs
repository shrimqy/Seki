using Seki.App.Data.Models;
using Seki.App.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                default:
                    System.Diagnostics.Debug.WriteLine($"Unknown message type: {message.Type}");
                    session.SendMessage(new Response { Content = "Unknown message type" });
                    break;
            }
        }

        private static void HandlePlaybackDataMessage(PlaybackData message)
        {
            // TODO
        }

        private static void HandleClipboardMessage(ClipboardMessage message, SekiSession session)
        {
            var clipboardData = new DataPackage();
            clipboardData.SetText(message.Content);
            Clipboard.SetContent(clipboardData);
        }

        private static void HandleNotificationMessage(NotificationMessage message)
        {
            NotificationService.ShowDesktopNotification(message);
        }

        private static void HandleResponseMessage(Response message)
        {
            System.Diagnostics.Debug.WriteLine($"Received response: {message.ResType}, {message.Content}");
        }

        private static void HandleDeviceInfoMessage(DeviceInfo message, SekiSession session)
        {
            System.Diagnostics.Debug.WriteLine($"Received device info: {message.DeviceName}");
            ((SekiServer)session.Server).OnDeviceInfoReceived(message);
        }

        private static void HandleDeviceStatusMessage(DeviceStatus message, SekiSession session)
        {
            System.Diagnostics.Debug.WriteLine($"Received device status: {message.BatteryStatus}");
            ((SekiServer)session.Server).OnDeviceStatusReceived(message);
        }
    }
}
