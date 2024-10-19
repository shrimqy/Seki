using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seki.App.Data.Models
{
    public enum NotificationFilter
    {
        DISABLED,
        FEED,
        TOASTEDFEED
    }

    public class NotificationPreferences
    {
        public string? AppName { get; set; }
        public string? NotificationFilter { get; set; }
        
    }
}
