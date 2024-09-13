using Microsoft.UI.Xaml.Media.Imaging;
using Seki.App.Services;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seki.App.Data.Models
{

    public class SocketMessageTypeConverter : JsonConverter<SocketMessageType>
    {
        public override SocketMessageType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string value = reader.GetString();
            return Enum.TryParse<SocketMessageType>(value, out var result) ? result : throw new JsonException($"Invalid SocketMessageType value: {value}");
        }

        public override void Write(Utf8JsonWriter writer, SocketMessageType value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(((int)value).ToString());
        }
    }

    public enum SocketMessageType
    {
        [EnumMember(Value = "0")]
        Response,
        [EnumMember(Value = "1")]
        Clipboard,
        [EnumMember(Value = "2")]
        Notification,
        [EnumMember(Value = "3")]
        DeviceInfo,
        [EnumMember(Value = "4")]
        DeviceStatus,
        [EnumMember(Value = "5")]
        PlaybackData,
        [EnumMember(Value = "6")]
        CommandType,
        [EnumMember(Value = "7")]
        FileTransferType,
    }

    public enum MediaAction
    {
        RESUME,
        PAUSE,
        NEXT_QUEUE,
        PREV_QUEUE,
        VOLUME
    }

    public enum CommandType
    {
        LOCK,
        SHUTDOWN,
        SLEEP,
        HIBERNATE,
    }

    public enum FileTransferType
    {
        HTTP,
        WEBSOCKET,
        P2P,
    }

    public class SocketMessage
    {
        [JsonPropertyName("type")]
        [JsonConverter(typeof(SocketMessageTypeConverter))]
        public SocketMessageType Type { get; set; }
    }

    public class Response : SocketMessage
    {
        public string ResType { get; set; }
        public string Content { get; set; }

        public Response()
        {
            Type = SocketMessageType.Response;
        }
    }


    public class ClipboardMessage : SocketMessage
    {
        [JsonPropertyName("content")]
        public string Content { get; set; }

        public ClipboardMessage()
        {
            Type = SocketMessageType.Clipboard;
        }
    }

    public class NotificationMessage : SocketMessage
    {
        [JsonPropertyName("notificationType")]
        public string NotificationType { get; set; }

        [JsonPropertyName("appName")]
        public string AppName { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("messages")]
        public List<Message> Messages { get; set; }

        [JsonIgnore]
        public List<GroupedMessage> GroupedMessages
        {
            get
            {
                var result = new List<GroupedMessage>();
                var currentGroup = new GroupedMessage();

                foreach (var message in Messages)
                {
                    if (currentGroup.Sender != message.Sender)
                    {
                        if (currentGroup.Sender != null)
                        {
                            result.Add(currentGroup);
                        }
                        currentGroup = new GroupedMessage
                        {
                            Sender = message.Sender,
                            Messages = new List<string>()
                        };
                    }
                    currentGroup.Messages.Add(message.Text);
                }

                if (currentGroup.Sender != null)
                {
                    result.Add(currentGroup);
                }

                return result;
            }
        }

        [JsonIgnore]
        public bool HasGroupedMessages => GroupedMessages?.Count > 0;

        [JsonPropertyName("tag")]
        public string Tag { get; set; }

        [JsonPropertyName("groupKey")]
        public string GroupKey { get; set; }

        public List<NotificationAction> Actions { get; set; } = new List<NotificationAction>();

        [JsonPropertyName("appIcon")]
        public string AppIcon { get; set; }

        [JsonPropertyName("bigPicture")]
        public string BigPicture { get; set; }

        [JsonPropertyName("largeIcon")]
        public string LargeIcon { get; set; }

        [JsonIgnore]
        public string IconBase64 { get; set; }

        [JsonIgnore]
        public BitmapImage Icon { get; set; }
    }

    public class GroupedMessage
    {
        public string Sender { get; set; }
        public List<string> Messages { get; set; }
    }

    public class Message
    {
        [JsonPropertyName("sender")]
        public string Sender { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }
    }

    public class NotificationAction
    {
        public string Label { get; set; }
        public string ActionId { get; set; }
    }

    public class DeviceInfo : SocketMessage
    {
        [JsonPropertyName("deviceName")]
        public string DeviceName { get; set; }

        [JsonPropertyName("userAvatar")]
        public string? UserAvatar { get; set; }

        public DeviceInfo()
        {
            Type = SocketMessageType.DeviceInfo;
        }
    }

    public class DeviceStatus : SocketMessage
    {
        [JsonPropertyName("batteryStatus")]
        public int BatteryStatus { get; set; }

        [JsonPropertyName("chargingStatus")]
        public Boolean ChargingStatus { get; set; }

        [JsonPropertyName("wifiStatus")]
        public Boolean WifiStatus { get; set; }

        [JsonPropertyName("bluetoothStatus")]
        public Boolean BluetoothStatus { get; set; }

    }

    public class PlaybackData : SocketMessage
    {
        [JsonPropertyName("appName")]
        public string AppName { get; set; }

        [JsonPropertyName("trackTitle")]
        public string TrackTitle { get; set; }

        [JsonPropertyName("artist")]
        public string? Artist { get; set; }

        [JsonPropertyName("volume")]
        public float Volume { get; set; }

        [JsonPropertyName("isPlaying")]
        public bool IsPlaying { get; set; }

        [JsonPropertyName("mediaAction")]
        public string? MediaAction { get; set; }

        [JsonPropertyName("thumbnail")]
        public string? Thumbnail { get; set; }

        [JsonPropertyName("appIcon")]
        public string? AppIcon { get; set; }
    }

    public class Command : SocketMessage
    {
        [JsonPropertyName("commandType")]
        public required String CommandType { get; set; }
    }


    public class FileTransfer : SocketMessage
    {
        [JsonPropertyName("transferType")]
        public required String TransferType { get; set; }

        [JsonPropertyName("metadata")]
        public FileMetadata? Metadata { get; set; }

        [JsonPropertyName("data")]
        public ByteArrayContent? Data { get; set; }

        public FileTransfer()
        {
            Type = SocketMessageType.FileTransferType;
        }
    }

    public class FileMetadata
    {
        [JsonPropertyName("fileName")]
        public required String FileName {  get; set; }

        [JsonPropertyName("fileType")]
        public required String FileType { get; set; }

        [JsonPropertyName("fileSize")]
        public required long FileSize { get; set; }

        [JsonPropertyName("uri")]
        public required String Uri { get; set; }
    }

}
