namespace Seki.App.Data.Models
{
    public class SocketMessage
    {
        public string Type { get; set; }
        public string Content { get; set; }
    }

    public class SocketMessageType
    {
        public const string Link = "link";
        public const string Clipboard = "clipboard";
        public const string Response = "response";
        public const string Notification = "notification";
        public const string Permission = "permissions";
        public const string Media = "media";
        public const string Message = "message";
    }

    public class Response : SocketMessage
    {
        public Response()
        {
            Type = SocketMessageType.Response;
        }
    }

    public class Link : SocketMessage
    {
        public Link()
        {
            Type = SocketMessageType.Link;
        }
    }

    public class ClipboardMessage : SocketMessage
    {


        public ClipboardMessage()
        {
            Type = SocketMessageType.Clipboard;
        }
    }

    public class Notification : SocketMessage
    {
        public string AppName { get; set; }
        public string NotificationContent { get; set; }
        public string[] Actions { get; set; }
        public Notification()
        {
            Type = SocketMessageType.Notification;
        }
    }

    public class RequestAccessMessage : SocketMessage
    {
        public string Feature { get; set; }
        public RequestAccessMessage()
        {
            Type = SocketMessageType.Permission;
        }
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

    public class MediaControlMessage : SocketMessage
    {
        public string ControlAction { get; set; }
        public string MediaInfo { get; set; }
        public MediaControlMessage()
        {
            Type = SocketMessageType.Media;
        }
    }
}
