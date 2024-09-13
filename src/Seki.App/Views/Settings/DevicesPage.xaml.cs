using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Seki.App.ViewModels;
using Seki.App.ViewModels.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;


namespace Seki.App.Views.Settings
{
    public sealed partial class DevicesPage : Page
    {

        public DevicesViewModel ViewModel { get; }
        public DevicesPage()
        {
            this.InitializeComponent();
            ViewModel = Ioc.Default.GetRequiredService<DevicesViewModel>();
        }
    }
}
