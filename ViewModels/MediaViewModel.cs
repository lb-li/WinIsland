using System;
using System.IO;
using System.Windows.Media.Imaging;
using WinIsland.Core;
using WinIsland.Services.Media;

namespace WinIsland.ViewModels
{
    public class MediaViewModel : ObservableObject, IDisposable
    {
        private readonly IMediaService _mediaService;
        private bool _disposed;
        private string _title = string.Empty;
        private string _artist = string.Empty;
        private BitmapImage? _albumCover;
        private bool _isPlaying;
        private bool _isActive;

        public MediaViewModel(IMediaService mediaService)
        {
            _mediaService = mediaService;
            _mediaService.MediaUpdated += OnMediaUpdated;
            _mediaService.PlaybackStateChanged += OnPlaybackStateChanged;

            TogglePlayPauseCommand = new RelayCommand(async _ => await _mediaService.TogglePlayPauseAsync());
            NextCommand = new RelayCommand(async _ => await _mediaService.SkipNextAsync());
            PreviousCommand = new RelayCommand(async _ => await _mediaService.SkipPreviousAsync());
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string Artist
        {
            get => _artist;
            set => SetProperty(ref _artist, value);
        }

        public BitmapImage? AlbumCover
        {
            get => _albumCover;
            set => SetProperty(ref _albumCover, value);
        }

        public bool IsPlaying
        {
            get => _isPlaying;
            set => SetProperty(ref _isPlaying, value);
        }

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public RelayCommand TogglePlayPauseCommand { get; }
        public RelayCommand NextCommand { get; }
        public RelayCommand PreviousCommand { get; }

        public async void Initialize()
        {
            if (_disposed) return;
            await _mediaService.InitializeAsync();
        }

        private void OnPlaybackStateChanged(object? sender, EventArgs e)
        {
            // State handled in MediaUpdated usually, but if needed we can refresh
        }

        private async void OnMediaUpdated(object? sender, MediaModel? model)
        {
            if (_disposed) return;

            if (model == null || string.IsNullOrWhiteSpace(model.Title))
            {
                IsActive = false;
                return;
            }

            IsActive = true;
            Title = model.Title;
            Artist = model.Artist;
            IsPlaying = model.IsPlaying;

            if (model.Thumbnail != null)
            {
                try
                {
                    using (var stream = await model.Thumbnail.OpenReadAsync())
                    using (var netStream = stream.AsStream())
                    {
                        var b = new BitmapImage();
                        b.BeginInit();
                        b.StreamSource = netStream;
                        b.CacheOption = BitmapCacheOption.OnLoad;
                        b.EndInit();
                        b.Freeze();
                        AlbumCover = b;
                    }
                }
                catch
                {
                    AlbumCover = null;
                }
            }
            else
            {
                AlbumCover = null;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _mediaService.MediaUpdated -= OnMediaUpdated;
            _mediaService.PlaybackStateChanged -= OnPlaybackStateChanged;
            _mediaService.Dispose();
        }
    }
}
