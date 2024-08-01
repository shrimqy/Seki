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
            var jsonElement = JsonSerializer.Deserialize<JsonElement>(json, options);
            if (jsonElement.TryGetProperty("type", out var typeElement))
            {
                if (typeElement.ValueKind == JsonValueKind.String)
                {
                    string typeString = typeElement.GetString();
                    if (Enum.TryParse<SocketMessageType>(typeString, out var messageType))
                    {
                        return messageType switch
                        {
                            SocketMessageType.Notification => JsonSerializer.Deserialize<NotificationMessage>(json, options),
                            SocketMessageType.Clipboard => JsonSerializer.Deserialize<ClipboardMessage>(json, options),
                            SocketMessageType.Response => JsonSerializer.Deserialize<Response>(json, options),
                            // Add more cases for other message types
                            _ => JsonSerializer.Deserialize<SocketMessage>(json, options) // Return base message if type is unknown
                        };
                    }
                }
            }
            throw new JsonException("Invalid or missing 'type' property in the JSON message.");
        }
    }
}
