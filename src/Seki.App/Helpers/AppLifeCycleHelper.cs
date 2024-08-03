using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seki.App.Helpers
{
    internal class AppLifeCycleHelper
    { 
        internal static void CloseApp()
        {
            MainWindow.Instance.Close();
        }
    }
}
