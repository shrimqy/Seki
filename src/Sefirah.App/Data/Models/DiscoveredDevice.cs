using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sefirah.App.Data.Models
{
    public class DiscoveredDevice
    {
        public string ServiceName { get; set; }
        public string PublicKey { get; set; }
        public string DeviceName { get; set; }
        public byte[]? HashedKey { get; set; }

        // Add a property to return the formatted key
        public string? FormattedKey
        {
            get
            {
                if (HashedKey == null)
                {
                    return "000000";
                }
                var derivedKeyInt = BitConverter.ToInt32(HashedKey, 0);
                derivedKeyInt = Math.Abs(derivedKeyInt) % 1_000_000;
                return derivedKeyInt.ToString().PadLeft(6, '0');
            }
        }
    }
}
