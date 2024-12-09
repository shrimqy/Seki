using Microsoft.Data.Sqlite;
using Microsoft.UI.Xaml.Media.Imaging;
using Sefirah.App.Data.Enums;
using Sefirah.App.Data.Models;
using System.IO;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Sefirah.App.Data.LocalDatabase
{
    public static class DataAccess
    {
        public const string DatabaseFileName = "AppDatabase.db";

        readonly static string DatabasePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, DatabaseFileName);

        public async static
        Task InitializeDatabase()
        {
            await ApplicationData.Current.LocalFolder
                .CreateFileAsync(DatabaseFileName, CreationCollisionOption.OpenIfExists);
            using var db = new SqliteConnection($"Filename={DatabasePath}");
            Debug.WriteLine($"Database Path: {DatabasePath}");
            db.Open();

            string notificationPreferencesTableCommand = "CREATE TABLE IF NOT EXISTS NotificationPreferences (" +
                "Id INTEGER PRIMARY KEY AUTOINCREMENT, " +
                "AppPackage NVARCHAR(2048) NOT NULL, " +
                "AppName NVARCHAR(2048), " +
                "NotificationFilter NVARCHAR(20) NOT NULL, " +
                "AppIcon BLOB)";

            string deviceDetailsTableCommand = "CREATE TABLE IF NOT EXISTS DeviceDetails (" +
                "DeviceId NVARCHAR(128) PRIMARY KEY, " +
                "DeviceName NVARCHAR(2048) NOT NULL, " +
                "HashedKey BLOB, " +
                "LastConnected DATETIME)";

            var createNotificationPreferencesTable = new SqliteCommand(notificationPreferencesTableCommand, db);
            createNotificationPreferencesTable.ExecuteNonQuery();

            var createDeviceDetailsTable = new SqliteCommand(deviceDetailsTableCommand, db);
            createDeviceDetailsTable.ExecuteNonQuery();
        }

        public static async Task<Device> AddOrUpdateDeviceDetail(string deviceId, string deviceName, byte[] hashedKey, DateTime lastConnected)
        {
            using var db = new SqliteConnection($"Filename={DatabasePath}");
            await db.OpenAsync();

            var checkCommand = new SqliteCommand
            {
                Connection = db,
                CommandText = "SELECT COUNT(*) FROM DeviceDetails WHERE DeviceId = @DeviceId;"
            };
            checkCommand.Parameters.AddWithValue("@DeviceId", deviceId);

            var exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;

            if (exists)
            {
                // Update the existing record
                var updateCommand = new SqliteCommand
                {
                    Connection = db,
                    CommandText = "UPDATE DeviceDetails SET DeviceName = @DeviceName, HashedKey = @HashedKey, LastConnected = @LastConnected WHERE DeviceId = @DeviceId;"
                };
                updateCommand.Parameters.AddWithValue("@DeviceName", deviceName);
                updateCommand.Parameters.AddWithValue("@HashedKey", hashedKey);
                updateCommand.Parameters.AddWithValue("@LastConnected", lastConnected);
                updateCommand.Parameters.AddWithValue("@DeviceId", deviceId);

                await updateCommand.ExecuteNonQueryAsync();
            }
            else
            {
                // Insert new record
                var insertCommand = new SqliteCommand
                {
                    Connection = db,
                    CommandText = "INSERT INTO DeviceDetails (DeviceId, DeviceName, HashedKey, LastConnected) VALUES (@DeviceId, @DeviceName, @HashedKey, @LastConnected);"
                };
                insertCommand.Parameters.AddWithValue("@DeviceId", deviceId);
                insertCommand.Parameters.AddWithValue("@DeviceName", deviceName);
                insertCommand.Parameters.AddWithValue("@HashedKey", hashedKey);
                insertCommand.Parameters.AddWithValue("@LastConnected", lastConnected);

                await insertCommand.ExecuteNonQueryAsync();
            }

            // Return the device object
            return new Device
            {
                DeviceId = deviceId,
                Name = deviceName,
                HashedKey = hashedKey,
                LastConnected = lastConnected
            };
        }

        public static List<Device> GetDeviceDetails()
        {
            var devices = new List<Device>();

            using var db = new SqliteConnection($"Filename={DatabasePath}");
            db.Open();

            var selectCommand = new SqliteCommand("SELECT DeviceId, DeviceName, LastConnected FROM DeviceDetails;", db);

            using var reader = selectCommand.ExecuteReader();
            while (reader.Read())
            {
                var device = new Device
                {
                    DeviceId = reader.GetString(0),
                    Name = reader.GetString(1),
                    LastConnected = reader.IsDBNull(2) ? null : reader.GetDateTime(2)
                };
                devices.Add(device);
            }

            return devices;
        }


        public static NotificationFilter AddAppPreference(string appPackage, string appName, string? appIconBase64)
        {
            if (string.IsNullOrEmpty(appPackage))
            {
                Debug.WriteLine("Error: AppPackage is required.");
                throw new InvalidOperationException("AppPackage cannot be null or empty.");
            }

            using var db = new SqliteConnection($"Filename={DatabasePath}");
            db.Open();

            // Check for existing preference
            var checkCommand = new SqliteCommand
            {
                Connection = db,
                CommandText = "SELECT COUNT(*) FROM NotificationPreferences WHERE AppPackage = @AppPackage;"
            };
            checkCommand.Parameters.AddWithValue("@AppPackage", appPackage);

            var exists = Convert.ToInt32(checkCommand.ExecuteScalar()) > 0;

            if (exists)
            {
                Debug.WriteLine($"Preference for {appPackage} already exists.");
                return NotificationFilter.TOASTEDFEED; // Return default or existing filter if duplicate found
            }

            // Insert if no duplicate
            byte[]? appIconBytes = string.IsNullOrEmpty(appIconBase64) ? null : Convert.FromBase64String(appIconBase64);
            var defaultFilter = NotificationFilter.TOASTEDFEED;

            var insertCommand = new SqliteCommand
            {
                Connection = db,
                CommandText = "INSERT INTO NotificationPreferences (AppPackage, AppName, NotificationFilter, AppIcon) " +
                              "VALUES (@AppPackage, @AppName, @NotificationFilter, @AppIcon);"
            };

            insertCommand.Parameters.AddWithValue("@AppPackage", appPackage);
            insertCommand.Parameters.Add("@AppName", (SqliteType)System.Data.DbType.String).Value = (object?)appName ?? DBNull.Value;
            insertCommand.Parameters.Add("@NotificationFilter", (SqliteType)System.Data.DbType.String).Value = defaultFilter.ToString();
            insertCommand.Parameters.Add("@AppIcon", (SqliteType)System.Data.DbType.Binary).Value = (object?)appIconBytes ?? DBNull.Value;

            insertCommand.ExecuteNonQuery();

            return defaultFilter;
        }


        public static void AddNotificationPreference(string appPackage, NotificationFilter notificationFilter)
        {
            using var db = new SqliteConnection($"Filename={DatabasePath}");
            db.Open();

            var insertCommand = new SqliteCommand
            {
                Connection = db,

                // Parameterized query for inserting notification preferences
                CommandText = "INSERT INTO NotificationPreferences (AppPackage, NotificationFilter) VALUES (@AppPackage, @NotificationFilter);"
            };
            insertCommand.Parameters.AddWithValue("@AppPackage", appPackage);
            insertCommand.Parameters.AddWithValue("@NotificationFilter", notificationFilter.ToString());

            insertCommand.ExecuteNonQuery();  // Use ExecuteNonQuery for inserts
        }

        // Update notification preference for a specific app package
        public static void UpdateNotificationPreference(string appPackage, NotificationFilter notificationFilter)
        {
            try
            {
                using var db = new SqliteConnection($"Filename={DatabasePath}");
                db.Open();

                var updateCommand = new SqliteCommand
                {
                    Connection = db,

                    CommandText = "UPDATE NotificationPreferences SET NotificationFilter = @NotificationFilter WHERE AppPackage = @AppPackage;"
                };
                updateCommand.Parameters.AddWithValue("@AppPackage", appPackage);
                updateCommand.Parameters.AddWithValue("@NotificationFilter", notificationFilter.ToString());

                updateCommand.ExecuteNonQuery();

                Debug.WriteLine($"Updated NotificationFilter: {notificationFilter}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Database update failed: " + ex.Message);
            }

        }

        // Retrieve all preferences
        public static async Task<List<NotificationPreferences>> GetNotificationPreferences()
        {
            var preferences = new List<NotificationPreferences>();
            using var db = new SqliteConnection($"Filename={DatabasePath}");
            db.Open();

            // Add ORDER BY clause to sort by AppName
            var selectCommand = new SqliteCommand("SELECT AppPackage, AppName, NotificationFilter, AppIcon FROM NotificationPreferences ORDER BY AppName", db);
            SqliteDataReader query = selectCommand.ExecuteReader();

            while (query.Read())
            {
                var appPackage = query.GetString(0);
                var appName = query.GetString(1);
                var notificationFilterStr = query.GetString(2); // NotificationFilter stored as string

                // Retrieve AppIcon as a byte array
                byte[]? appIconBytes = query.IsDBNull(3) ? null : (byte[])query[3]; // Get byte array

                // Convert string to enum
                _ = Enum.TryParse(notificationFilterStr, out NotificationFilter notificationFilter);

                // Convert the byte array to a BitmapImage
                BitmapImage? appIconImage = await ConvertByteArrayToBitmapImageAsync(appIconBytes);

                preferences.Add(new NotificationPreferences
                {
                    AppPackage = appPackage,
                    AppName = appName,
                    NotificationFilter = notificationFilter,
                    AppIcon = appIconImage
                });
            }

            return preferences;
        }


        // Convert byte array to BitmapImage
        private static async Task<BitmapImage?> ConvertByteArrayToBitmapImageAsync(byte[]? byteArray)
        {
            if (byteArray == null) return null;

            using var stream = new InMemoryRandomAccessStream();
            using (var writer = new DataWriter(stream.GetOutputStreamAt(0)))
            {
                writer.WriteBytes(byteArray);
                await writer.StoreAsync();
                await writer.FlushAsync();
            }

            var bitmapImage = new BitmapImage();
            await bitmapImage.SetSourceAsync(stream); // Set the source directly
            return bitmapImage;
        }


        // Check the notification filter for a specific app, and add the default filter if it doesn't exist
        public static NotificationFilter? GetNotificationFilter(string appPackage)
        {
            using var db = new SqliteConnection($"Filename={DatabasePath}");
            db.Open();

            var selectCommand = new SqliteCommand("SELECT NotificationFilter FROM NotificationPreferences WHERE AppPackage = @AppPackage", db);
            selectCommand.Parameters.AddWithValue("@AppPackage", appPackage);

            var result = selectCommand.ExecuteScalar()?.ToString();

            if (Enum.TryParse(result, out NotificationFilter filter))
            {
                return filter;
            }
            return null;
        }

        public static async Task<Device?> GetDeviceById(string deviceId)
        {
            using var db = new SqliteConnection($"Filename={DatabasePath}");
            await db.OpenAsync();

            var selectCommand = new SqliteCommand(
                "SELECT DeviceId, DeviceName, HashedKey, LastConnected FROM DeviceDetails WHERE DeviceId = @DeviceId;",
                db);
            selectCommand.Parameters.AddWithValue("@DeviceId", deviceId);

            SqliteDataReader query = selectCommand.ExecuteReader();

            if (await query.ReadAsync())
            {
                return new Device
                {
                    DeviceId = query.GetString(0),
                    Name = query.GetString(1),
                    HashedKey = (byte[])query[2],
                    LastConnected = query.IsDBNull(3) ? null : query.GetDateTime(3)
                };
            }
            return null;
        }
    }
}