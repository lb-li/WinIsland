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
        private DispatcherTimer? _todoScheduler;
        private TodoItem? _currentTodoItem;

        private void InitializeTodoFeature()
        {
            _settings = AppSettings.Load();

            if (_todoScheduler == null)
            {
                _todoScheduler = new DispatcherTimer();
                _todoScheduler.Interval = TimeSpan.FromSeconds(15);
                _todoScheduler.Tick += TodoScheduler_Tick;
            }

            if (_settings.TodoEnabled)
            {
                if (!_todoScheduler.IsEnabled) _todoScheduler.Start();
            }
            else
            {
                _todoScheduler.Stop();
            }
        }

        private void TodoScheduler_Tick(object? sender, EventArgs e)
        {
            if (_pomodoroService != null && 
               (_pomodoroService.CurrentState == PomodoroState.Running || _pomodoroService.CurrentState == PomodoroState.Paused))
            {
                return;
            }

            if (!_settings.TodoEnabled || _settings.TodoList == null) return;

            var now = DateTime.Now;

            foreach (var item in _settings.TodoList)
            {
                if (!item.IsCompleted && item.ReminderTime <= now)
                {
                    _currentTodoItem = item;
                    ShowTodoNotification(item);

                    if (_isNotificationActive && TxtTodoMessage.Text == item.Content) return;

                    break;
                }
            }
        }

        private void ShowTodoNotification(TodoItem item)
        {
            Dispatcher.Invoke(() =>
            {
                _isNotificationActive = true;
                _notificationTimer.Stop();

                HideAllPanels();

                TodoPanel.Visibility = Visibility.Visible;
                TodoPanel.Opacity = 0;
                TxtTodoMessage.Text = item.Content;

                DynamicIsland.IsHitTestVisible = true;
                SetClickThrough(false);

                DynamicIsland.BeginAnimation(UIElement.OpacityProperty, null);
                DynamicIsland.Opacity = 1.0;

                _widthSpring.Target = 320;
                _heightSpring.Target = 50;

                PlayIslandGlowEffect(Colors.Orange);
                PlayContentEntranceAnimation(TodoPanel);
                PlayTodoIconAnimation();
            });
        }

        private void PlayTodoIconAnimation()
        {
            var rotateAnim = new DoubleAnimationUsingKeyFrames { RepeatBehavior = RepeatBehavior.Forever };
            rotateAnim.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            rotateAnim.KeyFrames.Add(new EasingDoubleKeyFrame(-15, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(150))) { EasingFunction = new SineEase() });
            rotateAnim.KeyFrames.Add(new EasingDoubleKeyFrame(15, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(450))) { EasingFunction = new SineEase() });
            rotateAnim.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(600))) { EasingFunction = new SineEase() });
            rotateAnim.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(2000))));

            TodoIconRotate?.BeginAnimation(RotateTransform.AngleProperty, rotateAnim);
        }

        private void BtnTodoDone_Click(object? sender, RoutedEventArgs e)
        {
            if (_currentTodoItem != null)
            {
                _currentTodoItem.IsCompleted = true;
                _settings.Save();
                _currentTodoItem = null;
            }

            HideNotification();
        }
    }
}
