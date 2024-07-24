using Seki.App.Data.Models;
using System;
using System.Text.Json;
namespace Seki.App.Helpers
{
    public static class SocketMessageSerializer
    {
        private static JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
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
            var baseMessage = JsonSerializer.Deserialize<SocketMessage>(json, options);
            return baseMessage.Type switch
            {
                SocketMessageType.Notification => JsonSerializer.Deserialize<NotificationMessage>(json, options),
                SocketMessageType.Clipboard => JsonSerializer.Deserialize<ClipboardMessage>(json, options),
                // Add more cases for other message types
                _ => baseMessage // Return base message if type is unknown
            };
        }
    }
}
