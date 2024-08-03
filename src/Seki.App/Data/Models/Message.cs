﻿using System;
using System.Collections.Generic;
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
        DeviceStatus
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
    }

    public class NotificationAction
    {
        public string Label { get; set; }
        public string ActionId { get; set; }
    }

    public class DeviceInfo : SocketMessage
    {
        [JsonPropertyName("id")]
        public string DeviceId { get; set; }

        [JsonPropertyName("deviceName")]
        public string DeviceName { get; set; }

    }

    public class DeviceStatus : SocketMessage
    {
        [JsonPropertyName("batteryStatus")]
        public int BatteryStatus { get; set; }

        [JsonPropertyName("wifiStatus")]
        public Boolean WifiStatus { get; set; }

        [JsonPropertyName("bluetoothStatus")]
        public Boolean BluetoothStatus { get; set; }

    }
}