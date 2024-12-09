using Microsoft.UI.Xaml.Media.Imaging;
using Sefirah.App.Data.Enums;

namespace Sefirah.App.Data.Models
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
        public required string Content { get; set; }

        public ClipboardMessage()
        {
            Type = SocketMessageType.Clipboard;
        }
    }

    public class NotificationMessage : SocketMessage
    {
        [JsonPropertyName("notificationKey")]
        public required string NotificationKey { get; set; }

        [JsonPropertyName("timestamp")]
        public string? TimeStamp { get; set; }


        [JsonIgnore]
        public bool HasTimestamp => !string.IsNullOrEmpty(TimeStamp);


        [JsonPropertyName("notificationType")]
        public required string NotificationType { get; set; }

        [JsonPropertyName("appName")]
        public string? AppName { get; set; }

        [JsonPropertyName("appPackage")]
        public string? AppPackage { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("messages")]
        public List<Message>? Messages { get; set; }

        [JsonIgnore]
        public List<GroupedMessage>? GroupedMessages
        {
            get
            {
                var result = new List<GroupedMessage>();
                var currentGroup = new GroupedMessage();
                if (Messages != null)
                {
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
                                Messages = []
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
                return null;
            }
        }

        [JsonIgnore]
        public bool HasGroupedMessages => GroupedMessages?.Count > 0;

        [JsonIgnore]
        public bool ShouldShowTitle
        {
            get
            {
                // Check if GroupedMessages is not empty and compare Title with the first Sender
                if (GroupedMessages != null && GroupedMessages.Any())
                {
                    return !string.Equals(Title, GroupedMessages.First().Sender, StringComparison.OrdinalIgnoreCase);
                }

                else if (string.Equals(AppName, Title, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                // If no GroupedMessages or if it's empty, return true
                return true;
            }
        }

        [JsonIgnore]
        public string? FlyoutFilterString
        {
            get
            {
                return $"Turn off Notifications from {AppName}";
            }
        }

        [JsonPropertyName("tag")]
        public string? Tag { get; set; }

        [JsonPropertyName("groupKey")]
        public string? GroupKey { get; set; }

        [JsonPropertyName("actions")]
        public List<NotificationAction?> Actions { get; set; } = [];

        [JsonPropertyName("replyResultKey")]
        public string? ReplyResultKey { get; set; }

        [JsonPropertyName("appIcon")]
        public string? AppIcon { get; set; }

        [JsonPropertyName("bigPicture")]
        public string? BigPicture { get; set; }

        [JsonPropertyName("largeIcon")]
        public string? LargeIcon { get; set; }

        [JsonIgnore]
        public BitmapImage? Icon
        {
            get
            {
                if (!string.IsNullOrEmpty(LargeIcon))
                {
                    return BitmapHelper.Base64ToBitmapImage(LargeIcon);
                }
                else if (!string.IsNullOrEmpty(AppIcon))
                {
                    return BitmapHelper.Base64ToBitmapImage(AppIcon);
                }
                else
                {
                    return null;
                }
            }
        }

        public NotificationMessage()
        {
            Type = SocketMessageType.Notification;
        }
    }

    public class ReplyAction : SocketMessage
    {
        [JsonPropertyName("notificationKey")]
        public required string NotificationKey { get; set; }

        [JsonPropertyName("replyResultKey")]
        public required string ReplyResultKey { get; set; }

        [JsonPropertyName("replyText")]
        public required string ReplyText { get; set; }

        public ReplyAction()
        {
            Type = SocketMessageType.ReplyAction;
        }
    }

    public class NotificationAction : SocketMessage
    {
        [JsonPropertyName("notificationKey")]
        public required string NotificationKey { get; set; }

        [JsonPropertyName("label")]
        public required string Label { get; set; }

        [JsonPropertyName("actionIndex")]
        public required int ActionIndex { get; set; }

        [JsonPropertyName("isReplyAction")]
        public bool IsReplyAction { get; set; }

        public NotificationAction()
        {
            Type = SocketMessageType.NotificationAction;
        }
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

    public class DeviceInfo : SocketMessage
    {
        [JsonPropertyName("deviceId")]
        public string DeviceId { get; set; }

        [JsonPropertyName("deviceName")]
        public string DeviceName { get; set; }

        [JsonPropertyName("userAvatar")]
        public string? UserAvatar { get; set; }

        [JsonPropertyName("hashedSecret")]
        public string? HashedSecret { get; set; }

        [JsonPropertyName("publicKey")]
        public string PublicKey { get; set; }

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
        public bool ChargingStatus { get; set; }

        [JsonPropertyName("wifiStatus")]
        public bool WifiStatus { get; set; }

        [JsonPropertyName("bluetoothStatus")]
        public bool BluetoothStatus { get; set; }

    }

    public class PlaybackData : SocketMessage
    {
        [JsonPropertyName("appName")]
        public string? AppName { get; set; }

        [JsonPropertyName("trackTitle")]
        public string? TrackTitle { get; set; }

        [JsonPropertyName("artist")]
        public string? Artist { get; set; }

        [JsonPropertyName("isPlaying")]
        public bool? IsPlaying { get; set; }

        [JsonPropertyName("position")]
        public long? Position { get; set; }

        [JsonPropertyName("maxSeekTime")]
        public long? MaxSeekTime { get; set; }

        [JsonPropertyName("minSeekTime")]
        public long? MinSeekTime { get; set; }

        [JsonPropertyName("mediaAction")]
        public string? MediaAction { get; set; }

        [JsonPropertyName("volume")]
        public float Volume { get; set; }

        [JsonPropertyName("thumbnail")]
        public string? Thumbnail { get; set; }

        [JsonPropertyName("appIcon")]
        public string? AppIcon { get; set; }

        public PlaybackData()
        {
            Type = SocketMessageType.PlaybackData;
        }
    }

    public class Command : SocketMessage
    {
        [JsonPropertyName("commandType")]
        public required string CommandType { get; set; }

        public Command()
        {
            Type = SocketMessageType.CommandType;
        }
    }


    public class FileTransfer : SocketMessage
    {
        [JsonPropertyName("transferType")]
        public required string TransferType { get; set; }

        [JsonPropertyName("dataTransferType")]
        public required string DataTransferType { get; set; }

        [JsonPropertyName("metadata")]
        public FileMetadata? Metadata { get; set; }

        [JsonPropertyName("chunkData")]
        public string? ChunkData { get; set; }

        public FileTransfer()
        {
            Type = SocketMessageType.FileTransferType;
        }
    }

    public class FileMetadata
    {
        [JsonPropertyName("fileName")]
        public required string FileName { get; set; }

        [JsonPropertyName("mimeType")]
        public required string MimeType { get; set; }

        [JsonPropertyName("fileSize")]
        public required long FileSize { get; set; }

        [JsonPropertyName("uri")]
        public required string Uri { get; set; }
    }

    public class StorageInfo : SocketMessage
    {
        [JsonPropertyName("totalSpace")]
        public long TotalSpace { get; set; }

        [JsonPropertyName("freeSpace")]
        public long FreeSpace { get; set; }

        [JsonPropertyName("usedSpace")]
        public long UsedSpace { get; set; }

        public StorageInfo()
        {
            Type = SocketMessageType.StorageInfo;
        }

    }


    public class ScreenData : SocketMessage
    {
        [JsonPropertyName("timestamp")]
        public long TimeStamp { get; set; }

    }

    public class InteractiveControlMessage : SocketMessage
    {
        [JsonPropertyName("control")]
        public InteractiveControl Control { get; set; }

        public InteractiveControlMessage()
        {
            Type = SocketMessageType.InteractiveControlMessage;
        }
    }


    public class ApplicationInfo : SocketMessage
    {
        [JsonPropertyName("packageName")]
        public required string PackageName { get; set; }

        [JsonPropertyName("appName")]
        public required string AppName { get; set; }

        [JsonPropertyName("appIcon")]
        public string? AppIcon { get; set; }

    }

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(SingleTapEvent), typeDiscriminator: "SINGLE")]
    [JsonDerivedType(typeof(HoldTapEvent), typeDiscriminator: "HOLD")]
    [JsonDerivedType(typeof(KeyboardAction), typeDiscriminator: "KEYBOARD")]
    [JsonDerivedType(typeof(KeyEvent), typeDiscriminator: "KEY")]
    [JsonDerivedType(typeof(ScrollEvent), typeDiscriminator: "SCROLL")]
    [JsonDerivedType(typeof(SwipeEvent), typeDiscriminator: "SWIPE")]
    public abstract class InteractiveControl
    {
    }

    public class SingleTapEvent : InteractiveControl
    {
        [JsonPropertyName("x")]
        public double X { get; set; }

        [JsonPropertyName("y")]
        public double Y { get; set; }

        [JsonPropertyName("frameWidth")]
        public double FrameWidth { get; set; }

        [JsonPropertyName("frameHeight")]
        public double FrameHeight { get; set; }
    }

    public class HoldTapEvent : InteractiveControl
    {
        [JsonPropertyName("x")]
        public double X { get; set; }

        [JsonPropertyName("y")]
        public double Y { get; set; }

        [JsonPropertyName("frameWidth")]
        public double FrameWidth { get; set; }

        [JsonPropertyName("frameHeight")]
        public double FrameHeight { get; set; }
    }

    public class KeyboardAction : InteractiveControl
    {
        [JsonPropertyName("action")]
        public required string KeyboardActionType { get; set; }
    }

    public class KeyEvent : InteractiveControl
    {
        [JsonPropertyName("key")]
        public required string Key { get; set; }
    }

    public class SwipeEvent : InteractiveControl
    {
        [JsonPropertyName("startX")]
        public double StartX { get; set; }

        [JsonPropertyName("startY")]
        public double StartY { get; set; }

        [JsonPropertyName("willContinue")]
        public bool WillContinue { get; set; }

        [JsonPropertyName("endX")]
        public double EndX { get; set; }

        [JsonPropertyName("endY")]
        public double EndY { get; set; }

        [JsonPropertyName("frameWidth")]
        public double FrameWidth { get; set; }

        [JsonPropertyName("frameHeight")]
        public double FrameHeight { get; set; }

        [JsonPropertyName("duration")]
        public double Duration { get; set; }
    }

    public class ScrollEvent : InteractiveControl
    {
        [JsonPropertyName("direction")]
        public required string ScrollDirection { get; set; }
    }

    public class SftpServerInfo : SocketMessage
    {
        [JsonPropertyName("username")]
        public required string Username { get; set; }

        [JsonPropertyName("password")]
        public required string Password { get; set; }

        [JsonPropertyName("ipAddress")]
        public required string IpAddress { get; set; }

        [JsonPropertyName("port")]
        public int Port { get; set; }
    }
}
