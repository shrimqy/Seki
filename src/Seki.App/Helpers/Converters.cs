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
}
