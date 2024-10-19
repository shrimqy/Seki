using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using Windows.Storage;
using System.IO;
using Seki.App.Data.Models;

namespace Seki.App.Data
{
    public static class DataAccess
    {
        public const string DatabaseFileName = "AppDatabase.db"; 

        public static string DatabasePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, DatabaseFileName);

        public async static void InitializeDatabase()
        {
            await ApplicationData.Current.LocalFolder
                .CreateFileAsync(DatabaseFileName, CreationCollisionOption.OpenIfExists);
            using var db = new SqliteConnection($"Filename={DatabasePath}");
            db.Open();

            // Modify table creation for NotificationPreferences
            string tableCommand = "CREATE TABLE IF NOT EXISTS NotificationPreferences (" +
                "Id INTEGER PRIMARY KEY AUTOINCREMENT, " +
                "AppName NVARCHAR(2048) NOT NULL, " +
                "NotificationFilter NVARCHAR(20) NOT NULL)"; 

            var createTable = new SqliteCommand(tableCommand, db);
            createTable.ExecuteNonQuery();  // Use ExecuteNonQuery for statements not returning results
        }

        public static void AddNotificationPreference(string appName, NotificationFilter notificationFilter)
        {
            using var db = new SqliteConnection($"Filename={DatabasePath}");
            db.Open();

            var insertCommand = new SqliteCommand
            {
                Connection = db,

                // Parameterized query for inserting notification preferences
                CommandText = "INSERT INTO NotificationPreferences (AppName, NotificationFilter) VALUES (@AppName, @NotificationFilter);"
            };
            insertCommand.Parameters.AddWithValue("@AppName", appName);
            insertCommand.Parameters.AddWithValue("@NotificationFilter", notificationFilter.ToString());

            insertCommand.ExecuteNonQuery();  // Use ExecuteNonQuery for inserts
        }

        // Update notification preference for a specific app package
        public static void UpdateNotificationPreference(string appName, NotificationFilter notificationFilter)
        {
            using var db = new SqliteConnection($"Filename={DatabasePath}");
            db.Open();

            var updateCommand = new SqliteCommand
            {
                Connection = db,

                CommandText = "UPDATE NotificationPreferences SET NotificationFilter = @NotificationFilter WHERE AppName = @AppName;"
            };
            updateCommand.Parameters.AddWithValue("@AppName", appName);
            updateCommand.Parameters.AddWithValue("@NotificationFilter", notificationFilter.ToString());

            updateCommand.ExecuteNonQuery();
        }

        // Retrieve all preferences
        public static List<NotificationPreferences> GetNotificationPreferences()
        {
            var preferences = new List<NotificationPreferences>();
            using var db = new SqliteConnection($"Filename={DatabasePath}");
            db.Open();

            var selectCommand = new SqliteCommand("SELECT AppName, NotificationFilter FROM NotificationPreferences", db);
            SqliteDataReader query = selectCommand.ExecuteReader();

            while (query.Read())
            {
                preferences.Add(new NotificationPreferences
                {
                    AppName = query.GetString(0),
                    NotificationFilter = query.GetString(1)
                });
            }

            return preferences;
        }

        // Check the notification filter for a specific app, and add the default filter if it doesn't exist
        public static NotificationFilter GetNotificationFilter(string appName)
        {
            using var db = new SqliteConnection($"Filename={DatabasePath}");
            db.Open();

            // Step 1: Try to retrieve the existing filter
            var selectCommand = new SqliteCommand("SELECT NotificationFilter FROM NotificationPreferences WHERE AppName = @AppName", db);
            selectCommand.Parameters.AddWithValue("@AppName", appName);

            var result = selectCommand.ExecuteScalar()?.ToString();

            // Step 2: If the filter exists, return it
            if (Enum.TryParse(result, out NotificationFilter filter))
            {
                return filter;
            }

            // Step 3: If no filter exists, insert a new preference with the default filter (DISABLED)
            var insertCommand = new SqliteCommand("INSERT INTO NotificationPreferences (AppName, NotificationFilter) VALUES (@AppName, @DefaultFilter)", db);
            insertCommand.Parameters.AddWithValue("@AppName", appName);
            insertCommand.Parameters.AddWithValue("@DefaultFilter", NotificationFilter.TOASTEDFEED.ToString());

            // Execute the insert command
            insertCommand.ExecuteNonQuery();

            // Return the default filter since it's just been added
            return NotificationFilter.TOASTEDFEED;
        }
    }
}