using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Seki.App.ViewModels;
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

        public static IHost ConfigureHost()
        {
            return Host.CreateDefaultBuilder()
                .ConfigureServices(services => services
                    .AddSingleton<MainPageViewModel>()
                ).Build();
        }
    }
}
