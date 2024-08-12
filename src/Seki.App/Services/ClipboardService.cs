using Microsoft.UI.Xaml.Controls;
using System;
using Windows.ApplicationModel.DataTransfer;

namespace Seki.App.Services
{
    public class ClipboardService
    {
        public event EventHandler<string>? ClipboardContentChanged;

        public ClipboardService()
        {
            Clipboard.ContentChanged += OnClipboardContentChanged;
        }

        private async void OnClipboardContentChanged(object? sender, object? e)
        {
            var dataPackageView = Clipboard.GetContent();
            if (dataPackageView.Contains(StandardDataFormats.Text))
            {
                string text = await dataPackageView.GetTextAsync();
                ClipboardContentChanged?.Invoke(this, text);
            }
        }
    }
}
