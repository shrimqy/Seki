using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Media.Control;
using Seki.App.Data.Models;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage.Streams;

namespace Seki.App.Services
{
    public class PlaybackService
    {
        private static PlaybackService? _instance;
        public static PlaybackService Instance => _instance ??= new PlaybackService();

        private GlobalSystemMediaTransportControlsSessionManager? _manager;
        private Dictionary<string, GlobalSystemMediaTransportControlsSession> _activeSessions = new Dictionary<string, GlobalSystemMediaTransportControlsSession>();

        public event EventHandler<PlaybackData>? PlaybackDataChanged;

        private PlaybackService() { }

        public async Task InitializeAsync()
        {
            _manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            _manager.SessionsChanged += Manager_SessionsChanged;

            UpdateActiveSessions();
        }

        private void Manager_SessionsChanged(GlobalSystemMediaTransportControlsSessionManager sender, SessionsChangedEventArgs args)
        {
            UpdateActiveSessions();
        }

        private void UpdateActiveSessions()
        {
            if (_manager == null) return;

            var currentSessions = _manager.GetSessions();

            // Remove old sessions
            foreach (var sessionId in _activeSessions.Keys.ToList())
            {
                if (!currentSessions.Any(s => s.SourceAppUserModelId == sessionId))
                {
                    var removedSession = _activeSessions[sessionId];
                    UnsubscribeFromSessionEvents(removedSession);
                    _activeSessions.Remove(sessionId);
                    System.Diagnostics.Debug.WriteLine($"Removed session: {sessionId}");
                }
            }

            // Add new sessions
            foreach (var session in currentSessions)
            {
                if (!_activeSessions.ContainsKey(session.SourceAppUserModelId))
                {
                    _activeSessions[session.SourceAppUserModelId] = session;
                    SubscribeToSessionEvents(session);
                    System.Diagnostics.Debug.WriteLine($"Added new session: {session.SourceAppUserModelId}");
                }
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
            System.Diagnostics.Debug.WriteLine($"Media properties changed for {sender.SourceAppUserModelId}");
            await UpdatePlaybackDataAsync(sender);
        }

        private async void Session_PlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine($"Playback info changed for {sender.SourceAppUserModelId}");
            await UpdatePlaybackDataAsync(sender);
        }

        private async Task UpdatePlaybackDataAsync(GlobalSystemMediaTransportControlsSession session)
        {
            var playbackData = await GetPlaybackDataAsync(session);
            if (playbackData != null)
            {
                System.Diagnostics.Debug.WriteLine($"Playback data updated for {session.SourceAppUserModelId}: {playbackData.TrackTitle} by {playbackData.Artist}");
                PlaybackDataChanged?.Invoke(this, playbackData);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Failed to get playback data for {session.SourceAppUserModelId}");
            }
        }

        public async Task<PlaybackData?> GetPlaybackDataAsync(GlobalSystemMediaTransportControlsSession session)
        {
            var mediaProperties = await session.TryGetMediaPropertiesAsync();
            var playbackInfo = session.GetPlaybackInfo();

            var playbackData = new PlaybackData
            {
                AppName = session.SourceAppUserModelId,
                TrackTitle = mediaProperties.Title,
                Artist = mediaProperties.Artist,
                IsPlaying = playbackInfo.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing,
                // Note: Volume is not available from the session, you might need to handle this differently
            };

            if (mediaProperties.Thumbnail != null)
            {
                using IRandomAccessStreamWithContentType stream = await mediaProperties.Thumbnail.OpenReadAsync();
                playbackData.Thumbnail = await ConvertStreamToBase64(stream);
            }

            return playbackData;
        }

        private static async Task<string> ConvertStreamToBase64(IRandomAccessStreamWithContentType stream)
        {
            var reader = new DataReader(stream.GetInputStreamAt(0));
            var bytes = new byte[stream.Size];
            await reader.LoadAsync((uint)stream.Size);
            reader.ReadBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        public async Task HandleMediaActionAsync(PlaybackData message)
        {
            if (!_activeSessions.TryGetValue(message.AppName, out var session))
            {
                System.Diagnostics.Debug.WriteLine($"No active media session found for {message.AppName}");
                return;
            }

            if (!Enum.TryParse(message.MediaAction, true, out MediaAction action))
            {
                System.Diagnostics.Debug.WriteLine($"Unknown action: {message.MediaAction}");
                return;
            }

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
                default:
                    System.Diagnostics.Debug.WriteLine($"Unknown action: {action}");
                    break;
            }
        }
    }
}