using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Sefirah.App.Data.Contracts;
using Sefirah.App.Data.Enums;
using Sefirah.App.Data.Models;
using Sefirah.App.Utils;
using System.Runtime.InteropServices;
using Windows.Media.Control;
using Windows.Storage.Streams;

namespace Sefirah.App.Services;

public class PlaybackService(
    ILogger logger,
    ISessionManager sessionManager) : IPlaybackService, IDisposable
{
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ISessionManager _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
    private readonly DispatcherQueue _dispatcher = DispatcherQueue.GetForCurrentThread();
    private readonly Dictionary<string, GlobalSystemMediaTransportControlsSession> _activeSessions = [];
    private GlobalSystemMediaTransportControlsSessionManager? _manager;
    private bool _disposed;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        try
        {
            _manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            if (_manager == null)
            {
                _logger.Error("Failed to initialize GlobalSystemMediaTransportControlsSessionManager");
                return;
            }

            _manager.SessionsChanged += Manager_SessionsChanged;
            UpdateActiveSessions();

            _logger.Info("PlaybackService initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to initialize PlaybackService", ex);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task HandleLocalMediaActionAsync(PlaybackData request)
    {
        try
        {
            if (!Enum.TryParse(request.MediaAction, true, out MediaAction action)) return;

            await ExecuteMediaActionAsync(request, action);
        }
        catch (Exception ex)
        {
            _logger.Error("Error handling media action", ex);
            throw;
        }
    }

    private async Task ExecuteMediaActionAsync(PlaybackData request, MediaAction action)
    {
        var session = _activeSessions.Values.FirstOrDefault();
        if (session == null)
        {
            _logger.Warn("No active media sessions found");
            return;
        }

        await ExecuteSessionActionAsync(session, action, request);
    }

    private async Task ExecuteSessionActionAsync(GlobalSystemMediaTransportControlsSession session, MediaAction action, PlaybackData playbackData)
    {
        await _dispatcher.EnqueueAsync(async () =>
        {
            try
            {
                _logger.Info("Executing {0} for session {1}", action, session.SourceAppUserModelId);

                switch (action)
                {
                    case MediaAction.RESUME:
                        await session.TryPlayAsync();
                        break;
                    case MediaAction.PAUSE:
                        await session.TryPauseAsync();
                        break;
                    case MediaAction.NEXT_QUEUE:
                        await session.TrySkipNextAsync();
                        break;
                    case MediaAction.PREV_QUEUE:
                        await session.TrySkipPreviousAsync();
                        break;
                    case MediaAction.VOLUME:
                        VolumeControlAsync(playbackData.Volume);
                        break;
                    case MediaAction.SEEK:
                        break;
                    default:
                        _logger.Warn("Unhandled media action: {0}", action);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error executing media action {0} for session {1}",
                    new object[] { action, session.SourceAppUserModelId }, ex);
                throw;
            }
        });
    }

    private void Manager_SessionsChanged(GlobalSystemMediaTransportControlsSessionManager sender, SessionsChangedEventArgs args)
    {
        _logger.Info("Media sessions changed");
        UpdateActiveSessions();
    }

    private void UpdateActiveSessions()
    {
        if (_manager == null) return;

        try
        {
            var currentSessions = _manager.GetSessions();
            UpdateSessionsList(currentSessions);
            TriggerPlaybackDataUpdate();
        }
        catch (Exception ex)
        {
            _logger.Error("Error updating active sessions", ex);
        }
    }

    private void UpdateSessionsList(IReadOnlyList<GlobalSystemMediaTransportControlsSession> currentSessions)
    {
        // Remove old sessions
        foreach (var sessionId in _activeSessions.Keys.ToList())
        {
            if (!currentSessions.Any(s => s.SourceAppUserModelId == sessionId))
            {
                RemoveSession(sessionId);
            }
        }

        // Add new sessions
        foreach (var session in currentSessions.Where(s => s != null))
        {
            AddSession(session);
        }
    }

    private void RemoveSession(string sessionId)
    {
        if (_activeSessions.TryGetValue(sessionId, out var session))
        {
            UnsubscribeFromSessionEvents(session);
            _activeSessions.Remove(sessionId);
            _logger.Info("Removed media session: {0}", sessionId);
        }
    }

    private void AddSession(GlobalSystemMediaTransportControlsSession session)
    {
        if (!_activeSessions.ContainsKey(session.SourceAppUserModelId))
        {
            _activeSessions[session.SourceAppUserModelId] = session;
            SubscribeToSessionEvents(session);
            _logger.Info("Added new media session: {0}", session.SourceAppUserModelId);
        }
    }

    private void SubscribeToSessionEvents(GlobalSystemMediaTransportControlsSession session)
    {
        session.MediaPropertiesChanged += Session_MediaPropertiesChanged;
        session.PlaybackInfoChanged += Session_PlaybackInfoChanged;
    }

    private void UnsubscribeFromSessionEvents(GlobalSystemMediaTransportControlsSession session)
    {
        session.MediaPropertiesChanged -= Session_MediaPropertiesChanged;
        session.PlaybackInfoChanged -= Session_PlaybackInfoChanged;
    }

    private async void Session_MediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
    {
        _logger.Debug("Media properties changed for {0}", sender.SourceAppUserModelId);
        await UpdatePlaybackDataAsync(sender);
    }

    private async void Session_PlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
    {
        _logger.Debug("Playback info changed for {0}", sender.SourceAppUserModelId);
        await UpdatePlaybackDataAsync(sender);
    }

    private async void TriggerPlaybackDataUpdate()
    {
        if (_activeSessions.Count == 0)
        {
            return;
        }

        foreach (var session in _activeSessions.Values)
        {
            await UpdatePlaybackDataAsync(session);
        }
    }

    private async Task UpdatePlaybackDataAsync(GlobalSystemMediaTransportControlsSession session)
    {
        await _dispatcher.EnqueueAsync(async () =>
        {
            try
            {
                var playbackData = await GetPlaybackDataAsync(session);
                if (playbackData != null)
                {
                    _logger.Debug("Updated playback data for {0}", session.SourceAppUserModelId);
                    SendPlaybackData(playbackData);
                }
            }
            catch (COMException ex)
            {
                _logger.Error("COM Exception updating playback data for {0}", session.SourceAppUserModelId, ex);
                _activeSessions.Remove(session.SourceAppUserModelId);
            }
            catch (Exception ex)
            {
                _logger.Error("Error updating playback data for {0}", session.SourceAppUserModelId, ex);
            }
        });
    }

    private async Task<PlaybackData?> GetPlaybackDataAsync(GlobalSystemMediaTransportControlsSession session)
    {
        try
        {
            var mediaProperties = await session.TryGetMediaPropertiesAsync();
            var timelineProperties = session.GetTimelineProperties();
            var playbackInfo = session.GetPlaybackInfo();

            if (mediaProperties == null || playbackInfo == null)
            {
                _logger.Warn("Failed to get media properties or playback info for {SessionId}",
                    session.SourceAppUserModelId);
                return null;
            }

            var playbackData = new PlaybackData
            {
                AppName = session.SourceAppUserModelId,
                TrackTitle = mediaProperties.Title ?? "Unknown Title",
                Artist = mediaProperties.Artist ?? "Unknown Artist",
                IsPlaying = playbackInfo.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing,
                Position = timelineProperties.Position.Ticks,
                MinSeekTime = timelineProperties.MinSeekTime.Ticks,
                MaxSeekTime = timelineProperties.MaxSeekTime.Ticks,
                Volume = VolumeControl.GetMasterVolume() * 100
            };
            // TODO : Shuffle data, App Icon

            if (mediaProperties.Thumbnail != null)
            {
                playbackData.Thumbnail = await GetThumbnailBase64Async(mediaProperties.Thumbnail);
            }

            return playbackData;
        }
        catch (Exception ex)
        {
            _logger.Error("Error getting playback data for {0}", session.SourceAppUserModelId, ex);
            return null;
        }
    }

    private static async Task<string> GetThumbnailBase64Async(IRandomAccessStreamReference thumbnail)
    {
        using var stream = await thumbnail.OpenReadAsync();
        var reader = new DataReader(stream.GetInputStreamAt(0));
        var bytes = new byte[stream.Size];
        await reader.LoadAsync((uint)stream.Size);
        reader.ReadBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    private void SendPlaybackData(PlaybackData playbackData)
    {
        try
        {
            string jsonMessage = SocketMessageSerializer.Serialize(playbackData);
            _sessionManager.SendMessage(jsonMessage);
        }
        catch (Exception ex)
        {
            _logger.Error("Error sending playback data", ex);
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            foreach (var session in _activeSessions.Values)
            {
                UnsubscribeFromSessionEvents(session);
            }
            _activeSessions.Clear();

            if (_manager != null)
            {
                _manager.SessionsChanged -= Manager_SessionsChanged;
            }
        }
        _disposed = true;
    }

    public void VolumeControlAsync(double volume)
    {
        try
        {
            VolumeControl.ChangeVolume(volume);
            _logger.Info("Volume changed to {0}", volume);
        }
        catch (Exception ex)
        {
            _logger.Error("Error changing volume to {0}", volume, ex);
            throw;
        }
    }

    public Task HandleRemotePlaybackMessageAsync(PlaybackData data)
    {
        throw new NotImplementedException();
    }
}