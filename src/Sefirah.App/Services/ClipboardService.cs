using CommunityToolkit.WinUI;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Sefirah.App.Data.Contracts;
using Sefirah.App.Data.Enums;
using Sefirah.App.Data.Models;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace Sefirah.App.Services;

public class ClipboardService : IClipboardService
{
    private readonly ISessionManager _sessionManager;
    private readonly ILogger<ClipboardService> _logger;
    private readonly DispatcherQueue _dispatcher;
    public ClipboardService(ILogger<ClipboardService> logger,
        ISessionManager sessionManager)
    {
        _logger = logger;
        _sessionManager = sessionManager;
        _dispatcher = DispatcherQueue.GetForCurrentThread();
        // Subscribe to Clipboard Content Changed Event
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
                if (text != null && _sessionManager != null)
                {
                    var clipboardMessage = new ClipboardMessage
                    {
                        Type = SocketMessageType.Clipboard,
                        Content = text
                    };
                    _logger.LogInformation($"clipboard: {clipboardMessage.Content}");
                    string jsonMessage = SocketMessageSerializer.Serialize(clipboardMessage);
                    _sessionManager.SendMessage(jsonMessage);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting clipboard content: {ex}");
        }
    }
    public async Task SetContentAsync(object content)
    {
        await _dispatcher.EnqueueAsync(async () =>
        {
            try
            {
                var dataPackage = new DataPackage();

                switch (content)
                {
                    case string textContent:
                        await HandleTextContent(dataPackage, textContent);
                        break;
                    case StorageFile fileContent:
                        var files = new List<IStorageFile> { fileContent };
                        dataPackage.SetStorageItems(files);
                        break;
                    default:
                        throw new ArgumentException($"Unsupported content type: {content.GetType()}");
                }

                Clipboard.SetContent(dataPackage);
                _logger.LogInformation($"Clipboard content set: {content}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting clipboard content");
                throw;
            }
        });
    }

    private async Task HandleTextContent(DataPackage dataPackage, string content)
    {
        dataPackage.SetText(content);

        if (Uri.TryCreate(content, UriKind.Absolute, out Uri? uri))
        {
            // Add URI to clipboard data
            dataPackage.SetWebLink(uri);

            // Check if auto-open links is enabled and handle accordingly
            //    var settings = await _userSettingsService.GetSettingsAsync();
            //    if (settings.OpenLinksInBrowser)
            //    {
            //        try
            //        {
            //            // Using Windows.System.Launcher to open the URL
            //            await Windows.System.Launcher.LaunchUriAsync(uri);
            //            _logger.LogInformation($"Opened URL in browser: {uri}");
            //        }
            //        catch (Exception ex)
            //        {
            //            _logger.LogError(ex, "Failed to open URL in browser");
            //        }
            //    }
        }
    }
}