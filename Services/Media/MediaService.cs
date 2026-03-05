using System;
using System.Threading.Tasks;
using Windows.Media.Control;

namespace WinIsland.Services.Media
{
    public class MediaModel
    {
        public string Title { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public Windows.Storage.Streams.IRandomAccessStreamReference? Thumbnail { get; set; }
        public bool IsPlaying { get; set; }
    }

    public interface IMediaService : IDisposable
    {
        event EventHandler<MediaModel?> MediaUpdated;
        event EventHandler PlaybackStateChanged;
        Task InitializeAsync();
        Task TogglePlayPauseAsync();
        Task SkipNextAsync();
        Task SkipPreviousAsync();
        MediaModel? CurrentMedia { get; }
    }

    public class MediaService : IMediaService
    {
        private GlobalSystemMediaTransportControlsSessionManager? _mediaManager;
        private GlobalSystemMediaTransportControlsSession? _currentSession;
        private MediaModel? _currentMedia;
        private bool _disposed;

        public event EventHandler<MediaModel?>? MediaUpdated;
        public event EventHandler? PlaybackStateChanged;

        public MediaModel? CurrentMedia => _currentMedia;

        public async Task InitializeAsync()
        {
            if (_disposed) return;

            try
            {
                if (_mediaManager == null)
                {
                    _mediaManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
                    _mediaManager.CurrentSessionChanged += OnCurrentSessionChanged;
                    UpdateCurrentSession();
                }
            }
            catch (Exception)
            {
                // Log error
            }
        }

        private void OnCurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
        {
            UpdateCurrentSession();
        }

        private void UpdateCurrentSession()
        {
            if (_disposed) return;

            var session = _mediaManager?.GetCurrentSession();

            if (_currentSession != null)
            {
                _currentSession.MediaPropertiesChanged -= OnMediaPropertiesChanged;
                _currentSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
            }

            _currentSession = session;

            if (_currentSession != null)
            {
                _currentSession.MediaPropertiesChanged += OnMediaPropertiesChanged;
                _currentSession.PlaybackInfoChanged += OnPlaybackInfoChanged;
                
                // Trigger an immediate update
                _ = UpdateMediaInfoAsync();
            }
            else
            {
                _currentMedia = null;
                MediaUpdated?.Invoke(this, null);
            }
        }

        private void OnPlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
        {
             _ = UpdateMediaInfoAsync();
             PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
        }

        private async void OnMediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
        {
            await UpdateMediaInfoAsync();
        }

        private async Task UpdateMediaInfoAsync()
        {
            if (_disposed) return;
            var session = _currentSession;
            if (session == null) return;

            try
            {
                var info = await session.TryGetMediaPropertiesAsync();
                var playbackInfo = session.GetPlaybackInfo();
                
                if (info == null || playbackInfo == null) return;

                bool isPlaying = playbackInfo.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;

                _currentMedia = new MediaModel
                {
                    Title = info.Title,
                    Artist = info.Artist,
                    Thumbnail = info.Thumbnail,
                    IsPlaying = isPlaying
                };

                MediaUpdated?.Invoke(this, _currentMedia);
            }
            catch 
            {
               // Handle errors
            }
        }

        public async Task TogglePlayPauseAsync()
        {
            var session = _currentSession;
            if (session != null) await session.TryTogglePlayPauseAsync();
        }

        public async Task SkipNextAsync()
        {
            var session = _currentSession;
            if (session != null) await session.TrySkipNextAsync();
        }

        public async Task SkipPreviousAsync()
        {
            var session = _currentSession;
            if (session != null) await session.TrySkipPreviousAsync();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_currentSession != null)
            {
                _currentSession.MediaPropertiesChanged -= OnMediaPropertiesChanged;
                _currentSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
                _currentSession = null;
            }

            if (_mediaManager != null)
            {
                _mediaManager.CurrentSessionChanged -= OnCurrentSessionChanged;
                _mediaManager = null;
            }
        }
    }
}
