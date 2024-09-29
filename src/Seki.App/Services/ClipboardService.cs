using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Windows.ApplicationModel.DataTransfer;

namespace Seki.App.Services
{
    public class ClipboardService
    {
        private static ClipboardService? _instance;
        public static ClipboardService Instance => _instance ??= new ClipboardService();

        private readonly DispatcherQueue _dispatcherQueue;
        public event EventHandler<string>? ClipboardContentChanged;

        public ClipboardService()
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread() ?? throw new InvalidOperationException("ClipboardService must be initialized on a thread with a DispatcherQueue.");
            Clipboard.ContentChanged += OnClipboardContentChanged;
        }

        private async void OnClipboardContentChanged(object? sender, object? e)
        {
            try
            {
                var dataPackageView = Clipboard.GetContent();
                if (dataPackageView.Contains(StandardDataFormats.Text))
                {
                    string text = await dataPackageView.GetTextAsync();
                    ClipboardContentChanged?.Invoke(this, text);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting clipboard content: {ex}");
            }
        }

        public Task SetContentAsync(string content)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    var dataPackage = new DataPackage();
                    dataPackage.SetText(content);
                    Clipboard.SetContent(dataPackage);
                    System.Diagnostics.Debug.WriteLine($"Clipboard content set: {content}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error setting clipboard content: {ex}");
                    throw; // Rethrow the exception to let the caller handle it
                }
            });
            return Task.CompletedTask;
        }
    }
}
                

