using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seki.App.Utils
{
    public class CurrentUserInformation
    {
        public async Task<(string firstName, string avatar)> GetCurrentUserInfoAsync()
        {
            try
            {
                var users = await Windows.System.User.FindAllAsync();
                var currentUser = users.FirstOrDefault();

                if (currentUser != null)
                {
                    // Get user properties
                    var properties = await currentUser.GetPropertiesAsync(new string[] { "FirstName", "DisplayName", "AccountName" });

                    string? firstName = null;

                    // Try to get FirstName directly
                    if (properties.ContainsKey("FirstName") && properties["FirstName"] is string firstNameProperty)
                    {
                        firstName = firstNameProperty;
                    }

                    // If FirstName is not available, extract it from DisplayName or AccountName
                    if (string.IsNullOrEmpty(firstName))
                    {
                        string fullName = properties["DisplayName"] as string
                            ?? properties["AccountName"] as string
                            ?? Environment.UserName;

                        firstName = fullName.Split(' ').FirstOrDefault() ?? fullName;
                    }

                    // Get user avatar
                    var picture = await currentUser.GetPictureAsync(Windows.System.UserPictureSize.Size1080x1080);
                    var stream = await picture.OpenReadAsync();
                    string? avatarBase64 = null;

                    if (stream != null)
                    {
                        using (var reader = new Windows.Storage.Streams.DataReader(stream))
                        {
                            await reader.LoadAsync((uint)stream.Size);
                            byte[] buffer = new byte[stream.Size];
                            reader.ReadBytes(buffer);
                            avatarBase64 = Convert.ToBase64String(buffer);
                        }
                    }

                    return (firstName, avatarBase64);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting user info: {ex.Message}");
            }

            // Fallback to first part of Environment.UserName if everything else fails
            return (Environment.UserName.Split('\\').Last().Split(' ').First(), null);
        }
    }
}
