using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Media.Control;
using Seki.App.Data.Models;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage.Streams;
using CommunityToolkit.WinUI.Helpers;
using Windows.Storage;
using System.IO;
using Windows.Media.Devices;
using Seki.App.Utils;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Seki.App.Services
{
    public class PlaybackService : ObservableObject
    {
        private static PlaybackService? _instance;
        public static PlaybackService Instance => _instance ??= new PlaybackService();


        private GlobalSystemMediaTransportControlsSessionManager? _manager;
        private Dictionary<string, GlobalSystemMediaTransportControlsSession> _activeSessions = new Dictionary<string, GlobalSystemMediaTransportControlsSession>();

        public event EventHandler<PlaybackData>? PlaybackDataChanged;
        private bool _connectionStatus = false;
        public bool ConnectionStatus
        {
            get => _connectionStatus;
            set
            {
                if (SetProperty(ref _connectionStatus, value))
                {
                    OnConnectionStatusChange(_connectionStatus);
                }
            }
        }


        private PlaybackService() 
        {
            WebSocketService.Instance.ConnectionStatusChange += OnConnectionStatusChange;
        }

        private void OnConnectionStatusChange(bool connectionStatus)
        {
            ConnectionStatus = connectionStatus;
            TriggerPlaybackDataUpdate();
        }


        private async void TriggerPlaybackDataUpdate()
        {
            if (_activeSessions.Any())
            {
                foreach (var session in _activeSessions.Values)
                {
                    var playbackData = await GetPlaybackDataAsync(session);
                    if (playbackData != null)
                    {
                        PlaybackDataChanged?.Invoke(this, playbackData);
                    }
                }
            }
            else
            {
                // No active sessions, but you can still trigger a volume update or send default data.
                var volumeData = new PlaybackData
                {
                    Volume = VolumeControl.GetMasterVolume() * 100,
                    // Other default or null fields...
                };
                PlaybackDataChanged?.Invoke(this, volumeData);
            }
        }

        public async Task InitializeAsync()
        {
            try
            {
                _manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
                if (_manager == null)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to get GlobalSystemMediaTransportControlsSessionManager.");
                    return;
                }

                _manager.SessionsChanged += Manager_SessionsChanged;

                UpdateActiveSessions();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception during initialization: {ex.Message}");
            }
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
                    if (_activeSessions.TryGetValue(sessionId, out var removedSession))
                    {
                        UnsubscribeFromSessionEvents(removedSession);
                        _activeSessions.Remove(sessionId);
                        System.Diagnostics.Debug.WriteLine($"Removed session: {sessionId}");
                    }
                }
            }

            // Add new sessions
            foreach (var session in currentSessions)
            {
                if (session == null)
                {
                    System.Diagnostics.Debug.WriteLine("Encountered a null session in current sessions.");
                    continue;
                }

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
            try
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
            catch (COMException ex)
            {
                System.Diagnostics.Debug.WriteLine($"COM Exception occurred while updating playback data for {session.SourceAppUserModelId}: {ex.Message}");
                // Consider removing this session from _activeSessions if it's no longer valid
                _activeSessions.Remove(session.SourceAppUserModelId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception occurred while updating playback data for {session.SourceAppUserModelId}: {ex.Message}");
            }
        }

        public async Task<PlaybackData?> GetPlaybackDataAsync(GlobalSystemMediaTransportControlsSession session)
        {
            try
            {
                var mediaProperties = await session.TryGetMediaPropertiesAsync();
                if (mediaProperties == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Media properties are null for {session.SourceAppUserModelId}");
                    return null;
                }

                var playbackInfo = session.GetPlaybackInfo();
                if (playbackInfo == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Playback info is null for {session.SourceAppUserModelId}");
                    return null;
                }

                var volume = VolumeControl.GetMasterVolume() * 100;

                var playbackData = new PlaybackData
                {
                    AppName = session.SourceAppUserModelId,
                    TrackTitle = mediaProperties.Title ?? "Unknown Title",
                    Artist = mediaProperties.Artist ?? "Unknown Artist",
                    IsPlaying = playbackInfo.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing,
                    Volume = volume
                };

                if (mediaProperties.Thumbnail != null)
                {
                    using IRandomAccessStreamWithContentType stream = await mediaProperties.Thumbnail.OpenReadAsync();
                    playbackData.Thumbnail = await ConvertStreamToBase64(stream);
                }

                return playbackData;
            }
            catch (COMException ex)
            {
                System.Diagnostics.Debug.WriteLine($"COM Exception occurred while getting playback data for {session.SourceAppUserModelId}: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception occurred while getting playback data for {session.SourceAppUserModelId}: {ex.Message}");
                return null;
            }
        }

        private static async Task<string> ConvertStreamToBase64(IRandomAccessStream stream)
        {
            var reader = new DataReader(stream.GetInputStreamAt(0));
            var bytes = new byte[stream.Size];
            await reader.LoadAsync((uint)stream.Size);
            reader.ReadBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        private async Task<IRandomAccessStream?> GetAppIconAsync(string appUserModelId)
        {
            try
            {
                var packageManager = new Windows.Management.Deployment.PackageManager();
                var packages = packageManager.FindPackagesForUser(string.Empty, appUserModelId);
                System.Diagnostics.Debug.WriteLine("packages" + packages);
                // Get the package (first one should be the correct one)
                var package = packages.FirstOrDefault();
                System.Diagnostics.Debug.WriteLine("package" + package);
                if (package == null) return null;

                // Get the app's logo
                var logo = package.Logo;

                var file = await StorageFile.GetFileFromApplicationUriAsync(logo);
                System.Diagnostics.Debug.WriteLine(logo);
                System.Diagnostics.Debug.WriteLine(file);
                if (file != null)
                {
                    var stream = await file.OpenAsync(FileAccessMode.Read);
                    return stream;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to get app icon for {appUserModelId}: {ex.Message}");
            }
            //Computer\HKEY_CLASSES_ROOT\Local Settings\Software\Microsoft\Windows\Shell\MuiCache

            return null;
        }

        public async Task HandleMediaActionAsync(PlaybackData message)
        {

            if (!Enum.TryParse(message.MediaAction, true, out MediaAction action))
            {
                System.Diagnostics.Debug.WriteLine($"Unknown action: {message.MediaAction}");
                return;
            }

            // Handle volume change directly
            if (action.Equals(MediaAction.VOLUME))
            {
                VolumeControl.ChangeVolume(message.Volume);
                return;
            }

            if (!_activeSessions.TryGetValue(message.AppName, out var session))
            {
                System.Diagnostics.Debug.WriteLine($"No active media session found for {message.AppName}");
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