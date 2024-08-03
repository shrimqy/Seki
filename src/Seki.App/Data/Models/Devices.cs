using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seki.App.Data.Models
{
    public class Devices
    {
        public required string Id { get; set; }
        public string? Name { get; set; }
        public DateTime? LastConnected { get; set; }
    }
}
