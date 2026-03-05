using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using WinIsland.Services.FocusTimer;
using WinIsland.Services.SystemMonitor;

namespace WinIsland
{
    public partial class MainWindow
    {
        private SystemStatsService _statsService = null!;
        private PomodoroService _pomodoroService = null!;
        private FocusOverlayWindow _focusOverlayWindow = null!;
        private DispatcherTimer _autoHideTimer = null!;
        private bool _shouldRunMonitorWhenVisible = false;

        private void InitializeAutoHide()
        {
            _autoHideTimer = new DispatcherTimer();
            _autoHideTimer.Interval = TimeSpan.FromMilliseconds(100);
            _autoHideTimer.Tick += OnAutoHideTick;
        }

        private void OnAutoHideTick(object? sender, EventArgs e)
        {
            // 防御性编程：如果有高优先级任务正在显示，绝对禁止执行自动隐藏逻辑
            if (_isNotificationActive || _isFileStationActive || _isProgressActive || 
                (_pomodoroService.CurrentState != PomodoroState.Idle && _pomodoroService.CurrentState != PomodoroState.Finished))
            {
                DynamicIsland.Opacity = 1.0;
                return;
            }

            var cursor = System.Windows.Forms.Cursor.Position;
            bool isTopEdge = cursor.Y <= 5;
            bool isHover = this.IsMouseOver;

            // 胶囊模式：通过物理引擎改变尺寸而不是透明度
            if (isTopEdge || isHover)
            {
                // 唤醒状态：恢复正常尺寸
                // 只有当当前尺寸过小时才触发展开（避免重复设置 Target 导致弹簧抖动）
                if (_heightSpring.Target < 30)
                {
                     DynamicIsland.Opacity = 1.0; // 确保可见

                     if (_shouldRunMonitorWhenVisible)
                     {
                         ShowSystemMonitorPanel();
                         _statsService.Start();
                     }
                     else
                     {
                         HideAllPanels();
                         _widthSpring.Target = 120;
                         _heightSpring.Target = 35;
                     }
                }
            }
            else
            {
                // 待机状态：收缩为微子胶囊
                if (_heightSpring.Target > 10)
                {
                    // 1. 隐藏内容，避免在胶囊里显示文字
                    HideAllPanels();
                    
                    // 2. 停止服务
                    _statsService.Stop();

                    // 3. 设定胶囊尺寸 (宽 50, 高 6)
                    _widthSpring.Target = 50;
                    _heightSpring.Target = 6;
                    
                    // 4. 保持不透明，但可能需要微调位置（物理引擎会自动处理居中）
                }
            }
        }

        private void StopAutoHide()
        {
            _autoHideTimer?.Stop();
            DynamicIsland.BeginAnimation(UIElement.OpacityProperty, null);
            DynamicIsland.Opacity = 1.0;
        }

        private void EnterStandbyMode()
        {
            if (_isNotificationActive) return;
            if (_isProgressActive) return;

            // 停止不需要的后台任务以节省内存
            StopAudioCapture(); 

            Dispatcher.Invoke(() =>
            {
                if (_pomodoroService.CurrentState == PomodoroState.Running || _pomodoroService.CurrentState == PomodoroState.Paused)
                {
                    StopAutoHide();
                    ShowPomodoroPanel();
                    return;
                }

                var settings = AppSettings.Load();

                if (settings.EnableAutoHide)
                {
                    // 隐身模式下强制关闭常驻监控，只响应通知/事件（打造纯净体验）
                    _shouldRunMonitorWhenVisible = false;

                    if (settings.EnableSystemMonitor)
                    {
                        ShowSystemMonitorPanel();
                        // 隐身模式下，初始默认不启动，等滑出来再启动
                        _statsService.Stop(); 
                    }
                    else
                    {
                        _statsService.Stop();
                        HideAllPanels();
                        _widthSpring.Target = 120;
                        _heightSpring.Target = 35;
                    }
                    
                    if (!_autoHideTimer.IsEnabled) _autoHideTimer.Start();
                    return;
                }

                StopAutoHide();

                if (settings.EnableSystemMonitor)
                {
                    ShowSystemMonitorPanel();
                    _statsService.Start();
                }
                else
                {
                    _statsService.Stop();
                    HideAllPanels();
                    
                    _widthSpring.Target = 120;
                    _heightSpring.Target = 35;
                    DynamicIsland.Opacity = 0.4;
                    
                    SystemMonitorPanel.Visibility = Visibility.Collapsed;
                    PomodoroPanel.Visibility = Visibility.Collapsed;
                }
            });
        }


        private void ShowSystemMonitorPanel()
        {
            HideAllPanels();
            SystemMonitorPanel.Visibility = Visibility.Visible;
            
            _widthSpring.Target = 220;
            _heightSpring.Target = 35;
            DynamicIsland.Opacity = 0.95;
        }

        private void ShowPomodoroPanel()
        {
            HideAllPanels();
            PomodoroPanel.Visibility = Visibility.Visible;

            _widthSpring.Target = 200;
            _heightSpring.Target = 35;
            DynamicIsland.Opacity = 1.0;
        }

        private void OnSystemStatsUpdated(object? sender, SystemStats stats)
        {
            Dispatcher.Invoke(() =>
            {
                if (SystemMonitorPanel.Visibility != Visibility.Visible) return;

                TxtCpuUsage.Text = stats.GetFormattedCpu();
                TxtRamUsage.Text = stats.GetFormattedRam();
                TxtUploadSpeed.Text = stats.GetFormattedUpload();
                TxtDownloadSpeed.Text = stats.GetFormattedDownload();
            });
        }

        private void OnPomodoroTick(TimeSpan remaining, double progress)
        {
            Dispatcher.Invoke(() =>
            {
                if (PomodoroPanel.Visibility != Visibility.Visible) return;
                
                TxtFocusTimer.Text = remaining.ToString(@"mm\:ss");
                
                double totalWidth = _widthSpring.Target - 24; 
                if (totalWidth < 0) totalWidth = 0;
                
                PomodoroProgressBar.Width = totalWidth * progress;
            });
        }

        private void OnPomodoroStateChanged(PomodoroState state)
        {
            Dispatcher.Invoke(() =>
            {
                if (state != PomodoroState.Finished)
                {
                    _focusOverlayWindow.StopBreathing();
                }

                if (state == PomodoroState.Running)
                {
                     TxtFocusStatus.Text = "FOCUS";
                     TxtFocusStatus.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 140, 0));
                     BadgeFocusStatus.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(44, 44, 46));
                     CheckCurrentSession(); 
                }
                else if (state == PomodoroState.Paused)
                {
                     TxtFocusStatus.Text = "PAUSED";
                     TxtFocusStatus.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(142, 142, 147));
                }
                else if (state == PomodoroState.Finished)
                {
                     TxtFocusStatus.Text = "DONE";
                     TxtFocusStatus.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(48, 209, 88));
                     
                     PlayIslandPulseAnimation(Colors.Gold);
                     CheckCurrentSession(); 
                }
                else
                {
                     StopIslandPulseAnimation();
                     CheckCurrentSession();
                }
            });
        }

    }
}
