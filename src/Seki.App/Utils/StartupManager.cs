using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seki.App.Utils
{
    public static class StartupManager
    {
        public static void SetStartup(bool enable)
        {
            string appName = "MyApp";
            string exePath = Process.GetCurrentProcess().MainModule.FileName;

            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            if (enable)
            {
                registryKey.SetValue(appName, exePath);
            }
            else
            {
                registryKey.DeleteValue(appName, false);
            }
        }

        public static bool IsStartupEnabled()
        {
            string appName = "MyApp";
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            return registryKey.GetValue(appName) != null;
        }
    }
}
