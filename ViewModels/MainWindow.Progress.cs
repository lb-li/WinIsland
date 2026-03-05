using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace WinIsland
{
    public partial class MainWindow
    {
        private UniversalProgressManager? _progressManager;
        private bool _isProgressActive = false;

        private void InitializeProgressFeature()
        {
            if (_progressManager == null)
            {
                _progressManager = new UniversalProgressManager();
                _progressManager.OnProgressChanged += ProgressManager_OnProgressChanged;
            }
        }

        private void ProgressManager_OnProgressChanged(object? sender, UniversalProgressDetails e)
        {
            Dispatcher.Invoke(() =>
            {
                if (_isNotificationActive)
                {
                    return;
                }

                if (e.IsActive)
                {
                    ShowProgressState(e.AppName, e.ProgressValue);
                }
                else
                {
                    if (_isProgressActive)
                    {
                        HideProgressState();
                    }
                }
            });
        }

        private void ShowProgressState(string appName, double progress)
        {
            if (!_isProgressActive)
            {
                _isProgressActive = true;

                HideAllPanels();

                ProgressPanel.Visibility = Visibility.Visible;
                ProgressFill.Visibility = Visibility.Visible;

                DynamicIsland.BeginAnimation(UIElement.OpacityProperty, null);
                DynamicIsland.Opacity = 1.0;
                SetClickThrough(false);

                _widthSpring.Target = 200;
                _heightSpring.Target = 35;
            }

            string displayName = appName.Length > 8 ? appName.Substring(0, 8) + "..." : appName;
            TxtProgressTitle.Text = displayName;
            TxtProgressValue.Text = $"{Math.Round(progress)}%";

            double maxBarWidth = 176.0;
            double targetFillWidth = (progress / 100.0) * maxBarWidth;

            var widthAnim = new DoubleAnimation(targetFillWidth, TimeSpan.FromMilliseconds(400));
            widthAnim.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            ProgressFill.BeginAnimation(WidthProperty, widthAnim);
        }

        private void HideProgressState()
        {
            _isProgressActive = false;
            ProgressPanel.Visibility = Visibility.Collapsed;
            ProgressFill.Visibility = Visibility.Collapsed;

            CheckCurrentSession();
        }
    }
}
