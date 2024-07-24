using System;
using System.Collections.Generic;

namespace Seki.App.Data.Models
{

    public enum SocketMessageType
    {
        Clipboard,
        Notification,
        Response,
        Permission,
        Media,
        Link,
        Message
    }

    public class SocketMessage
    {
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
        public string Content { get; set; }

        public ClipboardMessage()
        {
            Type = SocketMessageType.Clipboard;
        }
    }

    public class NotificationMessage : SocketMessage
    {
        public string AppName { get; set; }
        public string Header { get; set; }
        public string Content { get; set; }
        public List<NotificationAction> Actions { get; set; } = new List<NotificationAction>();

        public NotificationMessage()
        {
            Type = SocketMessageType.Notification;
        }
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
