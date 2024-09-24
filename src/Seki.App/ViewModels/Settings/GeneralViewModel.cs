using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seki.App.ViewModels.Settings
{
    public class GeneralViewModel : ObservableObject
    {
        private bool _isStartupEnabled;
        public bool IsStartupEnabled
        {
            get => _isStartupEnabled;
            set => SetProperty(ref _isStartupEnabled, value);
        }

        public GeneralViewModel()
        {
            // Load saved preferences from configuration (if applicable)
        }

        public void SetStartupBehavior(bool isEnabled)
        {
            // Set the startup behavior of the app.
            // You may need to handle system tasks for setting the app to start at startup.
        }
    }
}
