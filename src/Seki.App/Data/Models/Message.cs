﻿using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Seki.App.Services;
using System;
using System.Collections.Generic;
using System.Linq;
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
        [EnumMember(Value = "8")]
        StorageInfo,
        [EnumMember(Value = "9")]
        DirectoryInfo,
        [EnumMember(Value = "10")]
        ScreenData,
        [EnumMember(Value ="11")]
        InteractiveControlMessage
    }

    public enum InteractiveControlType
    {
        [EnumMember(Value ="SINGLE")]
        SingleTapEvent,
        [EnumMember(Value = "HOLD")]
        HoldTapEvent,
        [EnumMember(Value = "SWIPE")]
        SwipeEvent,
        [EnumMember(Value = "KEYBOARD")]
        KeyboardEvent,
        [EnumMember(Value = "SCROLL")]
        ScrollEvent,
        [EnumMember(Value = "KEY")]
        KeyEvent,
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
        MIRROR,
        CLOSE_MIRROR,
        CLEAR_NOTIFICATIONS
    }

    public enum FileTransferType
    {
        HTTP,
        WEBSOCKET,
        P2P,
    }
    public enum DataTransferType
    {
        CHUNK,
        METADATA,
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
        [JsonPropertyName("notificationKey")]
        public string NotificationKey { get; set; }

        [JsonPropertyName("timestamp")]
        public string TimeStamp { get; set; }


        [JsonIgnore]
        public bool HasTimestamp => !string.IsNullOrEmpty(TimeStamp);


        [JsonPropertyName("notificationType")]
        public string NotificationType { get; set; }

        [JsonPropertyName("appName")]
        public string? AppName { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("messages")]
        public List<Message> Messages { get; set; }

        [JsonIgnore]
        public List<GroupedMessage> GroupedMessages
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

                // If no GroupedMessages or if it's empty, return true
                return true;
            }
        }

        [JsonPropertyName("tag")]
        public string? Tag { get; set; }

        [JsonPropertyName("groupKey")]
        public string? GroupKey { get; set; }

        public List<NotificationAction> Actions { get; set; } = new List<NotificationAction>();

        [JsonPropertyName("appIcon")]
        public string? AppIcon { get; set; }

        [JsonPropertyName("bigPicture")]
        public string? BigPicture { get; set; }

        [JsonPropertyName("largeIcon")]
        public string? LargeIcon { get; set; }

        [JsonIgnore]
        public string? IconBase64 { get; set; }

        [JsonIgnore]
        public BitmapImage? Icon { get; set; }
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
        public string? AppName { get; set; }

        [JsonPropertyName("trackTitle")]
        public string? TrackTitle { get; set; }

        [JsonPropertyName("artist")]
        public string? Artist { get; set; }

        [JsonPropertyName("volume")]
        public float Volume { get; set; }

        [JsonPropertyName("isPlaying")]
        public bool? IsPlaying { get; set; }

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

        public Command()
        {
            Type = SocketMessageType.CommandType;
        }
    }


    public class FileTransfer : SocketMessage
    {
        [JsonPropertyName("transferType")]
        public required String TransferType { get; set; }

        [JsonPropertyName("dataTransferType")]
        public required String DataTransferType { get; set; }

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
        public required String FileName {  get; set; }

        [JsonPropertyName("mimeType")]
        public required String MimeType { get; set; }

        [JsonPropertyName("fileSize")]
        public required long FileSize { get; set; }

        [JsonPropertyName("uri")]
        public required String Uri { get; set; }
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

    public enum ScrollDirection
    {
        UP, DOWN
    }

    public enum KeyboardActionType
    {
        Tab, Backspace, Enter, Escape, CtrlC, CtrlV, CtrlX, CtrlA, CtrlZ, CtrlY, Shift
    }
}
