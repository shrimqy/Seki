using Sefirah.App.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sefirah.App.Helpers;
public class JsonHelper
{
    public static SocketMessage JsonToSocketMessage(string jsonMessage)
    {
        try
        {
            return SocketMessageSerializer.DeserializeMessage(jsonMessage);
        }
        catch (JsonException ex)
        {
            Debug.WriteLine($"JSON deserialization error: {ex.Message}");
            throw;
        }
    }

    public static bool IsValidJson(string jsonString)
    {
        jsonString = jsonString.Trim();
        if (jsonString.StartsWith("{") && jsonString.EndsWith("}") || // object
            jsonString.StartsWith("[") && jsonString.EndsWith("]")) // array
        {
            try
            {
                JsonDocument.Parse(jsonString);
                return true;
            }
            catch (JsonException)
            {
                // Invalid JSON
                return false;
            }
        }
        return false;
    }
}
