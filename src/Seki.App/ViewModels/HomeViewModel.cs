using CommunityToolkit.Mvvm.ComponentModel;
using Seki.App.Data.Models;
using Seki.App.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seki.App.ViewModels
{
    
    public class HomeViewModel : ObservableObject
    {
        private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;

        private StorageInfo? _storageInfo = new();

        public StorageInfo? StorageInfo
        {
            get => _storageInfo;
            set => SetProperty(ref _storageInfo, value);
        }

        public HomeViewModel()
        {
            _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            MessageHandler.StorageInfoReceived += OnStorageInfoReceived;
        }

        private void OnStorageInfoReceived(object? sender, StorageInfo data)
        {
            _dispatcher.TryEnqueue(() => StorageInfo = data);
        }
    }
}
