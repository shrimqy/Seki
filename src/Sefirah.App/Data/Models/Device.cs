using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sefirah.App.Data.Models
{
    public class Device
    {
        public string DeviceId { get; set; }
        public string Name { get; set; }
        public byte[] HashedKey { get; set; }
        public DateTime? LastConnected { get; set; }
    }
}
