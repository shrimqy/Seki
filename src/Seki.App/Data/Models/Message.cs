using System;
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
        Permission,
        [EnumMember(Value = "4")]
        Media,
        [EnumMember(Value = "5")]
        Link,
        [EnumMember(Value = "6")]
        Message
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

    public class Link : SocketMessage
    {
        public string Url { get; set; }
        public Link()
        {
            Type = SocketMessageType.Link;
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
        [JsonPropertyName("appName")]
        public string AppName { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }

        public List<NotificationAction> Actions { get; set; } = new List<NotificationAction>();
    }

    public class NotificationAction
    {
        public string Label { get; set; }
        public string ActionId { get; set; }
    }

    public class RequestAccessMessage : SocketMessage
    {
        public string Feature { get; set; }
    }

    public class Message : SocketMessage
    {
        public string Sender { get; set; }
        public string MessageContent { get; set; }
        public bool ReadStatus { get; set; }

        public Message()
        {
            Type = SocketMessageType.Message;
        }
    }

    public class Media : SocketMessage
    {
        public string ControlAction { get; set; }
        public string MediaInfo { get; set; }

        public Media()
        {
            Type = SocketMessageType.Media;
        }
    }
}
