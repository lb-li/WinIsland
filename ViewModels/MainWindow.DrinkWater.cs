using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using WinIsland.Services.FocusTimer;

namespace WinIsland
{
    public partial class MainWindow
    {
        private DispatcherTimer? _drinkWaterScheduler;
        private DateTime _nextDrinkTime;
        private AppSettings _settings = new AppSettings();
        private string _lastTriggeredCustomTime = "";

        private void InitializeDrinkWaterFeature()
        {
            _settings = AppSettings.Load();

            if (_drinkWaterScheduler == null)
            {
                _drinkWaterScheduler = new DispatcherTimer();
                _drinkWaterScheduler.Interval = TimeSpan.FromSeconds(30);
                _drinkWaterScheduler.Tick += DrinkWaterScheduler_Tick;
            }

            if (_settings.DrinkWaterEnabled)
            {
                if (!_drinkWaterScheduler.IsEnabled)
                {
                    ResetNextDrinkTime();
                    _drinkWaterScheduler.Start();
                }
            }
            else
            {
                _drinkWaterScheduler.Stop();
            }
        }

        private void ResetNextDrinkTime()
        {
            _nextDrinkTime = DateTime.Now.AddMinutes(_settings.DrinkWaterIntervalMinutes);
            LogDebug($"Next drink time set to: {_nextDrinkTime}");
        }

        private void DrinkWaterScheduler_Tick(object? sender, EventArgs e)
        {
            if (!_settings.DrinkWaterEnabled) return;
            
            if (_pomodoroService != null && 
               (_pomodoroService.CurrentState == PomodoroState.Running || _pomodoroService.CurrentState == PomodoroState.Paused))
            {
                return;
            }

            if (_settings.DrinkWaterMode == DrinkWaterMode.Interval)
            {
                if (!IsWithinActiveHours()) return;

                if (DateTime.Now >= _nextDrinkTime)
                {
                    ShowDrinkWaterNotification();
                    ResetNextDrinkTime();
                }
            }
            else
            {
                var nowStr = DateTime.Now.ToString("HH:mm");
                if (_settings.CustomDrinkWaterTimes != null && _settings.CustomDrinkWaterTimes.Contains(nowStr))
                {
                    if (_lastTriggeredCustomTime != nowStr)
                    {
                        ShowDrinkWaterNotification();
                        _lastTriggeredCustomTime = nowStr;
                    }
                }
            }
        }

        private bool IsWithinActiveHours()
        {
            try
            {
                if (TimeSpan.TryParse(_settings.DrinkWaterStartTime, out TimeSpan start) &&
                    TimeSpan.TryParse(_settings.DrinkWaterEndTime, out TimeSpan end))
                {
                    var now = DateTime.Now.TimeOfDay;
                    if (start <= end)
                    {
                        return now >= start && now <= end;
                    }
                    else
                    {
                        return now >= start || now <= end;
                    }
                }
            }
            catch { }
            return true; 
        }

        private void ShowDrinkWaterNotification()
        {
            Dispatcher.Invoke(() =>
            {
                if (_pomodoroService != null && 
                   (_pomodoroService.CurrentState == PomodoroState.Running || _pomodoroService.CurrentState == PomodoroState.Paused))
                {
                    return;
                }

                _isNotificationActive = true;
                _notificationTimer.Stop();

                HideAllPanels();

                DrinkWaterPanel.Visibility = Visibility.Visible;
                DrinkWaterPanel.Opacity = 0;

                DynamicIsland.IsHitTestVisible = true;
                SetClickThrough(false);

                DynamicIsland.BeginAnimation(UIElement.OpacityProperty, null);
                DynamicIsland.Opacity = 1.0;

                _widthSpring.Target = 280;
                _heightSpring.Target = 50;

                PlayIslandGlowEffect(Colors.DeepSkyBlue);
                PlayContentEntranceAnimation(DrinkWaterPanel);
                PlayWaterDropAnimation();
            });
        }

        private void PlayWaterDropAnimation()
        {
            var floatAnim = new DoubleAnimationUsingKeyFrames { RepeatBehavior = RepeatBehavior.Forever };
            floatAnim.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            floatAnim.KeyFrames.Add(new EasingDoubleKeyFrame(-3, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1000)))
            { EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } });
            floatAnim.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(2000)))
            { EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } });

            WaterIconTranslate?.BeginAnimation(TranslateTransform.YProperty, floatAnim);
        }

        private void BtnDrank_Click(object? sender, RoutedEventArgs e)
        {
            HideNotification(); 
            ResetNextDrinkTime();
        }
    }
}
