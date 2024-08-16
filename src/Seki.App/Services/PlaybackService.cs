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


        private GlobalSystemMediaTransportControlsSession? _session;

        public event EventHandler<PlaybackData>? PlaybackDataChanged;
        public SystemMediaTransportControlsDisplayUpdater? DisplayUpdater { get; }
        // Property to access the current media player
        private static MediaPlayer MediaPlayer => BackgroundMediaPlayer.Current;

        private PlaybackService() { }

        public async Task InitializeAsync()
        {
            var manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            _session = manager.GetCurrentSession();

            if (_session != null)
            {
                _session.MediaPropertiesChanged += Session_MediaPropertiesChanged;
                _session.PlaybackInfoChanged += Session_PlaybackInfoChanged;
                System.Diagnostics.Debug.WriteLine("Session initialized successfully");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Failed to get current session");
            }
        }


        private async void Session_MediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine("Media properties changed");
            await UpdatePlaybackDataAsync();
        }

        private async void Session_PlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine("Playback info changed");
            await UpdatePlaybackDataAsync();
        }

        private async Task UpdatePlaybackDataAsync()
        {
            var playbackData = await GetCurrentPlaybackDataAsync();
            if (playbackData != null)
            {
                System.Diagnostics.Debug.WriteLine($"Playback data updated: {playbackData.TrackTitle} by {playbackData.Artist}");
                PlaybackDataChanged?.Invoke(this, playbackData);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Failed to get current playback data");
            }
        }

        public async Task<PlaybackData?> GetCurrentPlaybackDataAsync()
        {

            if (_session == null)
            {
                return null;
            }

            var mediaProperties = await _session.TryGetMediaPropertiesAsync();
            var playbackInfo = _session.GetPlaybackInfo();

            var playbackData = new PlaybackData
            {
                AppName = _session.SourceAppUserModelId,
                TrackTitle = mediaProperties.Title,
                Artist = mediaProperties.Artist,
                IsPlaying = playbackInfo.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing,
                Volume = MediaPlayer?.Volume ?? 0
            };

            System.Diagnostics.Debug.WriteLine("appName: " + playbackData.AppName + "track Title: " + playbackData.TrackTitle);
            // Get thumbnail
            if (mediaProperties.Thumbnail != null)
            {
                using IRandomAccessStreamWithContentType stream = await mediaProperties.Thumbnail.OpenReadAsync();
                // Convert stream to byte array or base64 string
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
    }
}
