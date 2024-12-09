using Sefirah.App.Data.Models;

namespace Sefirah.App.ViewModels
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
        }

        private void OnStorageInfoReceived(object? sender, StorageInfo data)
        {
            _dispatcher.TryEnqueue(() => StorageInfo = data);
        }
    }
}
