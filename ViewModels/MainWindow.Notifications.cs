using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Windows.UI.Notifications; // UserNotification
using Windows.UI.Notifications.Management; // UserNotificationListener

namespace WinIsland
{
    public partial class MainWindow
    {
        private DispatcherTimer _notificationTimer = null!;
        private bool _isNotificationActive = false;
        private UserNotificationListener _listener = null!;
        
        private DispatcherTimer? _notificationPoller;
        private HashSet<uint> _knownNotificationIds = new HashSet<uint>();
        private int _pollCount = 0;

        private void InitializeNotificationTimer()
        {
            _notificationTimer = new DispatcherTimer();
            _notificationTimer.Interval = TimeSpan.FromSeconds(5);
            _notificationTimer.Tick += NotificationTimer_Tick;
        }

        private void NotificationTimer_Tick(object? sender, EventArgs e)
        {
            HideNotification();
        }

        private async void InitializeNotificationListener()
        {
            LogDebug("Initializing Notification Listener (Hybrid Mode)...");
            try
            {
                if (!Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.UI.Notifications.Management.UserNotificationListener"))
                {
                    LogDebug("Error: UserNotificationListener API not present on this system.");
                    return;
                }

                _listener = UserNotificationListener.Current;
                var accessStatus = await _listener.RequestAccessAsync();
                LogDebug($"Notification Access Status: {accessStatus}");

                if (accessStatus == UserNotificationListenerAccessStatus.Allowed)
                {
                    LogDebug("Notification access granted. Initializing hybrid listener...");

                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(2000); 
                        try
                        {
                            Dispatcher.Invoke(() =>
                            {
                                _listener.NotificationChanged += Listener_NotificationChanged;
                                LogDebug("鉁?Event listener registered successfully (delayed)");
                            });
                        }
                        catch (Exception ex)
                        {
                            LogDebug($"鉁?Event registration failed: {ex.Message}. Relying on polling.");
                        }
                    });

                    _notificationPoller = new DispatcherTimer();
                    _notificationPoller.Interval = TimeSpan.FromSeconds(2);
                    _notificationPoller.Tick += NotificationPoller_Tick;
                    _notificationPoller.Start();
                    LogDebug("鉁?Polling backup started (300ms interval)");
                }
                else
                {
                    LogDebug("Notification access denied.");
                    System.Windows.MessageBox.Show(
                        "WinIsland 需要通知访问权限。请在弹出的系统设置中允许通知访问。",
                        "权限提示");
                    await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-notifications"));
                }
            }
            catch (Exception ex)
            {
                LogDebug($"Notification Listener Error: {ex.Message}");
            }
        }


        private void Listener_NotificationChanged(UserNotificationListener sender, UserNotificationChangedEventArgs args)
        {
            try
            {
                if (args.ChangeKind != UserNotificationChangedKind.Added) return;

                var notif = _listener.GetNotification(args.UserNotificationId);
                if (notif == null) return;

                lock (_knownNotificationIds)
                {
                    if (_knownNotificationIds.Contains(notif.Id)) return;
                    _knownNotificationIds.Add(notif.Id);
                }

                LogDebug($"[EVENT] New notification ID={notif.Id}");
                ProcessNotification(notif);
            }
            catch (Exception ex)
            {
                LogDebug($"Event handler error: {ex.Message}");
            }
        }

        private async void NotificationPoller_Tick(object? sender, EventArgs e)
        {
            try
            {
                _pollCount++;
                var notifications = await _listener.GetNotificationsAsync(NotificationKinds.Toast);

                if (_pollCount % 30 == 0)
                {
                    LogDebug($"[HEARTBEAT] Poll #{_pollCount}, Notifications: {notifications.Count}");
                }

                foreach (var notif in notifications)
                {
                    bool isNew = false;
                    lock (_knownNotificationIds)
                    {
                        if (!_knownNotificationIds.Contains(notif.Id))
                        {
                            _knownNotificationIds.Add(notif.Id);
                            isNew = true;
                        }
                    }

                    if (isNew)
                    {
                        LogDebug($"[POLL] New notification ID={notif.Id}");
                        ProcessNotification(notif);
                    }
                }

                var currentIds = new HashSet<uint>(notifications.Select(n => n.Id));
                lock (_knownNotificationIds)
                {
                    _knownNotificationIds.RemoveWhere(id => !currentIds.Contains(id));
                }
            }
            catch (Exception ex)
            {
                LogDebug($"[POLL] Error: {ex.Message}");
            }
        }

        private void ProcessNotification(UserNotification notif)
        {
            try
            {
                var appName = notif.AppInfo.DisplayInfo.DisplayName;
                LogDebug($"[PROCESS] AppName='{appName}', ID={notif.Id}");

                var settings = AppSettings.Load();
                if (!settings.EnableMessageNotification)
                {
                    LogDebug($"[PROCESS] Message notification disabled, skipping");
                    return;
                }

                string title = "";
                string body = "";

                try
                {
                    var binding = notif.Notification.Visual.GetBinding(KnownNotificationBindings.ToastGeneric);
                    if (binding != null)
                    {
                        var texts = binding.GetTextElements();
                        if (texts.Count > 0) title = texts[0].Text ?? "";
                        if (texts.Count > 1) body = texts[1].Text ?? "";
                    }
                }
                catch (Exception ex)
                {
                    LogDebug($"[PARSE] Error: {ex.Message}");
                }

                const int MAX_LENGTH = 200;
                if (body.Length > MAX_LENGTH)
                    body = body.Substring(0, MAX_LENGTH) + "...";

                LogDebug($"[DISPLAY] App: {appName}, Title: {title}, Body: {body}");
                Dispatcher.Invoke(() => ShowAppNotification(appName, title, body));

                if (settings.CleanSystemNotification)
                {
                    try
                    {
                        _listener.RemoveNotification(notif.Id);
                        LogDebug($"[CLEAN] Removed system notification: {notif.Id}");
                    }
                    catch (Exception removeEx)
                    {
                        LogDebug($"[CLEAN] Failed to remove: {removeEx.Message}");
                    }
                }
            }
            catch (Exception e)
            {
                LogDebug($"[PROCESS] Fatal error: {e.Message}");
            }
        }

        private void ShowAppNotification(string appName, string title, string body)
        {
            bool isLongText = (title.Length + body.Length) > 25;
            double targetHeight = isLongText ? 85 : 50;
            double targetWidth = isLongText ? 360 : 320;

            ActivateNotification(targetHeight, targetWidth);

            NotificationTitle.Text = appName.ToUpper();
            NotificationBody.Inlines.Clear();

            if (!string.IsNullOrEmpty(title))
            {
                NotificationBody.Inlines.Add(new Run(title)
                {
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Colors.White),
                    FontSize = 13
                });
            }

            if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(body) && title != body)
            {
                NotificationBody.Inlines.Add(new Run("\n"));
            }

            if (!string.IsNullOrEmpty(body) && title != body)
            {
                NotificationBody.Inlines.Add(new Run(body)
                {
                    Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(200, 200, 200)),
                    FontSize = 12
                });
            }

            IconConnect.Visibility = Visibility.Collapsed;
            IconDisconnect.Visibility = Visibility.Collapsed;
            IconMessage.Visibility = Visibility.Visible;

            PlayFlipAnimation();
        }

        private void ActivateNotification(double targetHeight = 50, double targetWidth = 320)
        {
            _isNotificationActive = true;
            _notificationTimer.Stop();
            _notificationTimer.Start();

            HideAllPanels();

            NotificationPanel.Visibility = Visibility.Visible;

            DynamicIsland.IsHitTestVisible = true;
            SetClickThrough(false);

            DynamicIsland.BeginAnimation(UIElement.OpacityProperty, null);
            DynamicIsland.Opacity = 1.0;

            _widthSpring.Target = targetWidth;
            _heightSpring.Target = targetHeight;

            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(400),
                BeginTime = TimeSpan.FromMilliseconds(200),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            NotificationPanel.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        }

        private void HideNotification()
        {
            _isNotificationActive = false;
            _notificationTimer.Stop();

            var fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            fadeOut.Completed += (s, e) =>
            {
                NotificationPanel.Visibility = Visibility.Collapsed;
                DrinkWaterPanel.Visibility = Visibility.Collapsed;
                TodoPanel.Visibility = Visibility.Collapsed;

                CheckCurrentSession();
            };

            if (NotificationPanel.Visibility == Visibility.Visible)
                NotificationPanel.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            else if (DrinkWaterPanel.Visibility == Visibility.Visible)
                DrinkWaterPanel.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            else if (TodoPanel.Visibility == Visibility.Visible)
                TodoPanel.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            else
            {
                CheckCurrentSession();
            }
        }
    }
}
