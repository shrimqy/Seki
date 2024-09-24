using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Seki.App.Data.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seki.App.Helpers
{
    public class DateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string timestampStr && DateTime.TryParseExact(timestampStr, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime timestamp))
            {
                if (timestamp.Date == DateTime.Today)
                {
                    // Return only the time if the date is the same as today
                    return timestamp.ToString("t"); // Short time pattern
                }
                else
                {
                    // Return the short date and time pattern otherwise
                    return timestamp.ToString("g"); // Short date and time pattern
                }
            }

            return string.Empty; // Return an empty string if the timestamp is null or invalid
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class DateTimeDevicesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DateTime timestamp)
            {
                // Check if the date is today
                if (timestamp.Date == DateTime.Today)
                {
                    // Return only the time if the date is the same as today
                    return timestamp.ToString("t", CultureInfo.CurrentCulture); // Short time pattern
                }
                else
                {
                    // Return the short date and time pattern otherwise
                    return timestamp.ToString("g", CultureInfo.CurrentCulture); // Short date and time pattern
                }
            }

            return string.Empty; // Return an empty string if the value is not a DateTime
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class BatteryStatusToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DeviceStatus deviceStatus)
            {
                // Based on battery level and charging state, choose the appropriate icon
                if (deviceStatus.ChargingStatus)
                {
                    return deviceStatus.BatteryStatus switch
                    {
                        >= 100 => "\uEA93",
                        >= 90 => "\uE83E",
                        >= 80 => "\uE862",  
                        >= 70 => "\uE861",
                        >= 60 => "\uE860",
                        >= 50 => "\uE85F",
                        >= 40 => "\uE85E",
                        >= 30 => "\uE85D", 
                        >= 20 => "\uE85C",
                        >= 10 => "\uE85B",
                        _ => "\uE85A"       
                    };
                }
                else
                {
                    return deviceStatus.BatteryStatus switch
                    {
                        >= 100 => "\uE83F",
                        >= 90 => "\uE859",  
                        >= 80 => "\uE858",
                        >= 70 => "\uE857", 
                        >= 60 => "\uE856",
                        >= 50 => "\uE855",
                        >= 40 => "\uE854",
                        >= 30 => "\uE853",
                        >= 20 => "\uE852",
                        >= 10 => "\uE851",
                        _ => "\uE850"
                    };
                }
            }

            return "\uE83F"; // Default icon
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value == null ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class NullBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value == null ? false : true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }


    public class StoragePercentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            // Check if value is not null and is of type StorageInfo, and TotalSpace is greater than zero
            if (value is StorageInfo storageInfo && storageInfo.TotalSpace > 0)
            {
                // Safely calculate the percentage of used space and return a valid double
                double percentage = ((double)storageInfo.UsedSpace / storageInfo.TotalSpace) * 100;
                return percentage;
            }

            // If the value is null or invalid, return 0 to prevent crashes
            return 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
    public class StorageInfoTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            // Check if the value is not null and is of type StorageInfo
            if (value is StorageInfo storageInfo)
            {
                // Convert the long values to GB for display purposes
                double freeSpaceGB = storageInfo.FreeSpace / 1_073_741_824.0;
                double totalSpaceGB = storageInfo.TotalSpace / 1_073_741_824.0;

                // Format and return the storage info text
                return $"{freeSpaceGB:F2} GB free of {totalSpaceGB:F2} GB";
            }

            // Return a fallback message if the value is null or invalid
            return "Storage information not available";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
