using Sefirah.App.Data.Enums;
using Sefirah.App.Data.Models;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;

namespace Sefirah.App.Helpers
{
    public static class SocketMessageSerializer
    {
        private static readonly JsonSerializerOptions options = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        public static string Serialize(object message)
        {
            return JsonSerializer.Serialize(message, options);
        }

        public static T Deserialize<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, options);
        }

        public static SocketMessage DeserializeMessage(string json)
        {
            var jsonElement = JsonSerializer.Deserialize<JsonElement>(json, options);
            if (jsonElement.TryGetProperty("type", out var typeElement) && typeElement.ValueKind == JsonValueKind.String)
            {
                string typeString = typeElement.GetString();
                if (Enum.TryParse<SocketMessageType>(typeString, out var messageType))
                {
                    switch (messageType)
                    {
                        case SocketMessageType.Notification:
                            return JsonSerializer.Deserialize<NotificationMessage>(json, options);
                        case SocketMessageType.Clipboard:
                            return JsonSerializer.Deserialize<ClipboardMessage>(json, options);
                        case SocketMessageType.Response:
                            return JsonSerializer.Deserialize<Response>(json, options);
                        case SocketMessageType.DeviceInfo:
                            return JsonSerializer.Deserialize<DeviceInfo>(json, options);
                        case SocketMessageType.DeviceStatus:
                            return JsonSerializer.Deserialize<DeviceStatus>(json, options);
                        case SocketMessageType.PlaybackData:
                            return JsonSerializer.Deserialize<PlaybackData>(json, options);
                        case SocketMessageType.CommandType:
                            return JsonSerializer.Deserialize<Command>(json, options);
                        case SocketMessageType.FileTransferType:
                            return JsonSerializer.Deserialize<FileTransfer>(json, options);
                        case SocketMessageType.StorageInfo:
                            return JsonSerializer.Deserialize<StorageInfo>(json, options);
                        case SocketMessageType.ScreenData:
                            return JsonSerializer.Deserialize<ScreenData>(json, options);
                        case SocketMessageType.ApplicationInfo:
                            return JsonSerializer.Deserialize<ApplicationInfo>(json, options);
                        case SocketMessageType.SftpServerInfo:
                            return JsonSerializer.Deserialize<SftpServerInfo>(json, options);
                        default:
                            return JsonSerializer.Deserialize<SocketMessage>(json, options);
                    }
                }
            }
            throw new JsonException("Invalid or missing 'type' property in the JSON message.");
        }
    }
}

