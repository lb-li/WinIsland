using NAudio.Wave;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Windows.Foundation;
using Windows.Media.Control;
using Windows.Storage.Streams;

namespace WinIsland
{
    public partial class MainWindow
    {
        // Legacy fields removed
        private WasapiLoopbackCapture? _capture;
        private float _currentVolume = 0;

        // InitializeMediaListener and EventHandlers removed as they are handled by MediaViewModel

        private void CheckCurrentSession()
        {
            if (_isNotificationActive) return;
            if (_isFileStationActive) return;

            if (_storedFiles.Count > 0)
            {
                ShowFileStationState();
                return;
            }

            // REFACTORED: Use ViewModel state
            if (_viewModel != null && _viewModel.Media != null && _viewModel.Media.IsActive)
            {
                // Ensure UI is expanded if ViewModel says so
                 Dispatcher.Invoke(() =>
                 {
                    var settings = AppSettings.Load();
                    if (!settings.ShowMediaPlayer)
                    {
                        EnterStandbyMode();
                        return;
                    }
                    
                    // Trigger UI update if needed (normally PropertyChanged handles this, but this is a re-check)
                    // We can just rely on the existing state, or force a refresh if the UI is in standby
                     _widthSpring.Target = 400;
                     _heightSpring.Target = 60;
                     
                     // Ensure panels are correct
                     HideAllPanels();
                     // Media Panels visibility is handled by bindings now
                 });
            }
            else
            {
                EnterStandbyMode();
            }
        }

        // Legacy UpdateMediaInfo, UpdatePlaybackStatus, Button Logic Removed
        // They are irrelevant now.


        private void InitializeAudioCapture()
        {
            try 
            { 
                _capture = new WasapiLoopbackCapture(); 
                _capture.DataAvailable += OnAudioDataAvailable; 
                // Do not start immediately
            } 
            catch { }
        }

        private void StartAudioCapture()
        {
            try
            {
                if (_capture != null && _capture.CaptureState == NAudio.CoreAudioApi.CaptureState.Stopped)
                {
                    _capture.StartRecording();
                }
            }
            catch { }
        }

        private void StopAudioCapture()
        {
            try
            {
                if (_capture != null && _capture.CaptureState != NAudio.CoreAudioApi.CaptureState.Stopped)
                {
                    _capture.StopRecording();
                }
            }
            catch { }
        }

        private void OnAudioDataAvailable(object? sender, WaveInEventArgs e)
        {
            if (VisualizerContainer.Visibility != Visibility.Visible) return;

            float max = 0;
            for (int i = 0; i < e.BytesRecorded; i += 8)
            {
                short sample = BitConverter.ToInt16(e.Buffer, i);
                var normalized = Math.Abs(sample / 32768f);
                if (normalized > max) max = normalized;
            }
            _currentVolume = max;
        }

        private void UpdateVisualizer()
        {
            var time = DateTime.Now.TimeOfDay.TotalSeconds;
            double baseH = 4 + (_currentVolume * 40);

            double h3 = Math.Max(4, baseH * (0.9 + 0.3 * Math.Sin(time * 20)));
            double h2 = Math.Max(4, baseH * (0.7 + 0.25 * Math.Cos(time * 18 + 1)));
            double h4 = Math.Max(4, baseH * (0.7 + 0.25 * Math.Cos(time * 16 + 2)));
            double h1 = Math.Max(4, baseH * (0.5 + 0.2 * Math.Sin(time * 14 + 3)));
            double h5 = Math.Max(4, baseH * (0.5 + 0.2 * Math.Sin(time * 12 + 4)));

            Bar1.Height = h1;
            Bar2.Height = h2;
            Bar3.Height = h3;
            Bar4.Height = h4;
            Bar5.Height = h5;
        }

        private async void LoadThumbnail(IRandomAccessStreamReference thumbnail)
        {
            try
            {
                using (var stream = await thumbnail.OpenReadAsync())
                using (var netStream = stream.AsStream())
                {
                    var b = new BitmapImage();
                    b.BeginInit();
                    b.StreamSource = netStream;
                    b.CacheOption = BitmapCacheOption.OnLoad;
                    b.EndInit();
                    b.Freeze();
                    AlbumCover.Source = b;
                }
            }
            catch { }
        }
    }
}
