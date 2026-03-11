using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using WinIsland.Services.FocusTimer;
using WinIsland.Services.SystemMonitor;

namespace WinIsland
{
    public partial class MainWindow : Window
    {
        private bool _isCleaningUp;

        public MainWindow()
        {
            InitializeComponent();
            
            _viewModel = new ViewModels.MainViewModel();
            DataContext = _viewModel;

            var settings = AppSettings.Load();
            ApplyFontSettings(settings);
            _viewModel.Media.PropertyChanged += OnMediaPropertyChanged;

            // 1. 物理动画（Springs），底层依赖
            InitializePhysics();

            // 2. 核心服务（Services），供 UI 逻辑使用
            _statsService = new SystemStatsService();
            _statsService.OnStatsUpdated += OnSystemStatsUpdated;

            _pomodoroService = new PomodoroService();
            _pomodoroService.OnTick += OnPomodoroTick;
            _pomodoroService.OnStateChanged += OnPomodoroStateChanged;

            // 3. 辅助窗口（Overlay）
            _focusOverlayWindow = new FocusOverlayWindow();
            _focusOverlayWindow.OnScreenClicked += OnFocusOverlayScreenClicked;

            // 4. 定时器与功能模块（Timers & Features）
            InitializeAutoHide(); 
            InitializeNotificationTimer();
            InitializeAudioCapture();
            InitializeDeviceWatcher();
            InitializeDrinkWaterFeature();
            InitializeTodoFeature();
            InitializeProgressFeature(); 

            // 5. 事件监听（Listeners），可能立即触发 UI 刷新，放在最后初始化
            InitializeNotificationListener();
            // InitializeMediaListener(); // 已重构：现在由 MainViewModel 处理

            // 6. 窗口事件与热键
            this.Loaded += MainWindow_Loaded;
            this.ContentRendered += MainWindow_ContentRendered;

            this.Closing += MainWindow_Closing;
        }

        internal ViewModels.MainViewModel _viewModel;

        private void MainWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;

            // Hide from Alt+Tab
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_TOOLWINDOW);
        }

        private void MainWindow_ContentRendered(object? sender, EventArgs e)
        {
            CenterWindowAtTop();
            CheckCurrentSession();
        }

        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            CleanupResources();
        }

        private void OnFocusOverlayScreenClicked()
        {
            _pomodoroService.Stop();
            CheckCurrentSession();
        }

        private void CleanupResources()
        {
            if (_isCleaningUp) return;
            _isCleaningUp = true;

            try { this.Loaded -= MainWindow_Loaded; } catch { }
            try { this.ContentRendered -= MainWindow_ContentRendered; } catch { }
            try { this.Closing -= MainWindow_Closing; } catch { }

            try { _viewModel.Media.PropertyChanged -= OnMediaPropertyChanged; } catch { }
            try { _viewModel.Dispose(); } catch { }

            try { _statsService.OnStatsUpdated -= OnSystemStatsUpdated; _statsService.Stop(); } catch { }
            try
            {
                _pomodoroService.OnTick -= OnPomodoroTick;
                _pomodoroService.OnStateChanged -= OnPomodoroStateChanged;
                _pomodoroService.Stop();
            }
            catch { }

            try
            {
                _focusOverlayWindow.OnScreenClicked -= OnFocusOverlayScreenClicked;
                _focusOverlayWindow.StopBreathing();
                _focusOverlayWindow.Close();
            }
            catch { }

            try
            {
                if (_progressManager != null)
                {
                    _progressManager.OnProgressChanged -= ProgressManager_OnProgressChanged;
                    _progressManager.Dispose();
                    _progressManager = null;
                }
            }
            catch { }

            try { if (_autoHideTimer != null) { _autoHideTimer.Stop(); _autoHideTimer.Tick -= OnAutoHideTick; } } catch { }
            try { if (_drinkWaterScheduler != null) { _drinkWaterScheduler.Stop(); _drinkWaterScheduler.Tick -= DrinkWaterScheduler_Tick; } } catch { }
            try { if (_todoScheduler != null) { _todoScheduler.Stop(); _todoScheduler.Tick -= TodoScheduler_Tick; } } catch { }
            try { if (_notificationTimer != null) { _notificationTimer.Stop(); _notificationTimer.Tick -= NotificationTimer_Tick; } } catch { }
            try
            {
                if (_notificationPoller != null)
                {
                    _notificationPoller.Stop();
                    _notificationPoller.Tick -= NotificationPoller_Tick;
                    _notificationPoller = null;
                }
            }
            catch { }
            try { _listener.NotificationChanged -= Listener_NotificationChanged; } catch { }

            try
            {
                if (_bluetoothWatcher != null)
                {
                    _bluetoothWatcher.Added -= BluetoothWatcher_Added;
                    _bluetoothWatcher.Removed -= BluetoothWatcher_Removed;
                    _bluetoothWatcher.Updated -= BluetoothWatcher_Updated;
                    _bluetoothWatcher.Stop();
                    _bluetoothWatcher = null;
                }
            }
            catch { }

            try
            {
                if (_usbWatcher != null)
                {
                    _usbWatcher.Added -= UsbWatcher_Added;
                    _usbWatcher.Removed -= UsbWatcher_Removed;
                    _usbWatcher.Stop();
                    _usbWatcher = null;
                }
            }
            catch { }

            try { CompositionTarget.Rendering -= OnRendering; } catch { }

            try
            {
                if (_capture != null)
                {
                    _capture.DataAvailable -= OnAudioDataAvailable;
                    _capture.StopRecording();
                    _capture.Dispose();
                    _capture = null;
                }
            }
            catch { }
        }

        private void OnMediaPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModels.MediaViewModel.IsActive))
            {
                Dispatcher.Invoke(() =>
                {
                    if (_viewModel.Media.IsActive)
                    {
                        HideAllPanels(); // 隐藏非媒体面板
                        _widthSpring.Target = 400;
                        _heightSpring.Target = 60;

                        // 音频可视化逻辑
                        try 
                        {
                            var settings = AppSettings.Load();
                            if (settings.ShowVisualizer && !settings.EnableAutoHide)
                            {
                                VisualizerContainer.Visibility = Visibility.Visible;
                                StartAudioCapture();
                            }
                            else
                            {
                                StopAudioCapture();
                            }
                        }
                        catch {}
                    }
                    else
                    {
                        StopAudioCapture();
                        // 如果未被其他功能接管，则收缩
                        _widthSpring.Target = 120;
                        _heightSpring.Target = 35;
                    }
                });
            }
        }

        public void ReloadSettings()
        {
            var settings = AppSettings.Load();
            ApplyFontSettings(settings);

            InitializeDrinkWaterFeature();
            InitializeTodoFeature();
            InitializeProgressFeature();

            CheckCurrentSession();
        }

        private void ApplyFontSettings(AppSettings settings)
        {
            try
            {
                var fontFamily = new System.Windows.Media.FontFamily(settings.FontFamily);
                
                this.FontFamily = fontFamily;
                this.FontSize = settings.FontSize;

                foreach (var child in GetAllChildren(Content as System.Windows.DependencyObject))
                {
                    if (child is System.Windows.Controls.TextBlock textBlock)
                    {
                        textBlock.FontFamily = fontFamily;
                        textBlock.FontSize = settings.FontSize;
                    }
                    else if (child is System.Windows.Controls.Control control)
                    {
                        control.FontFamily = fontFamily;
                        control.FontSize = settings.FontSize;
                    }
                }
            }
            catch { }
        }

        private IEnumerable<System.Windows.DependencyObject> GetAllChildren(System.Windows.DependencyObject? parent)
        {
            if (parent == null) yield break;
            
            int childCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                yield return child;
                foreach (var descendant in GetAllChildren(child))
                {
                    yield return descendant;
                }
            }
        }

        private void CenterWindowAtTop()
        {
            try
            {
                var screenWidth = SystemParameters.PrimaryScreenWidth;
                var screenHeight = SystemParameters.PrimaryScreenHeight;

                this.Left = (screenWidth - this.Width) / 2;
                this.Top = 0; 
            }
            catch { }
        }

        private void HideAllPanels()
        {
            // 歌曲标题、专辑封面、控制面板现在由 MVVM 绑定处理
            // SongTitle.Visibility = Visibility.Collapsed;
            // AlbumCover.Visibility = Visibility.Collapsed;
            // ControlPanel.Visibility = Visibility.Collapsed;
            
            VisualizerContainer.Visibility = Visibility.Collapsed;
            
            NotificationPanel.Visibility = Visibility.Collapsed;
            DrinkWaterPanel.Visibility = Visibility.Collapsed;
            TodoPanel.Visibility = Visibility.Collapsed;
            FileStationPanel.Visibility = Visibility.Collapsed;
            
            SystemMonitorPanel.Visibility = Visibility.Collapsed;
            PomodoroPanel.Visibility = Visibility.Collapsed;
            ProgressPanel.Visibility = Visibility.Collapsed;
        }

        // Win32 API
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        // private const int WS_EX_LAYERED = 0x00080000;

        private void SetClickThrough(bool enable)
        {
            try
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                if (enable)
                {
                    SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_TRANSPARENT);
                }
                else
                {
                    SetWindowLong(hwnd, GWL_EXSTYLE, exStyle & ~WS_EX_TRANSPARENT);
                }
            }
            catch { }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private void LogDebug(string message)
        {
            // Debug logging disabled by default
        }

        private System.Windows.Point _dragStartPoint;
        private bool _isPotentialDrag;

        private void Window_MouseDown(object? sender, MouseButtonEventArgs e)
        {
            // 双击左键：切换专注模式
            if (e.ClickCount == 2 && e.ChangedButton == MouseButton.Left)
            {
                var settings = AppSettings.Load();
                if (settings.EnableFocusMode)
                {
                    if (_pomodoroService.CurrentState == PomodoroState.Idle || _pomodoroService.CurrentState == PomodoroState.Finished)
                    {
                        _pomodoroService.StartFocus(settings.PomodoroDurationMinutes);
                    }
                    else
                    {
                        _pomodoroService.Stop();
                    }
                    CheckCurrentSession(); 
                }
                return;
            }

            // Ctrl+左键：开始拖拽
            if (e.ChangedButton == MouseButton.Left && Keyboard.Modifiers == ModifierKeys.Control)
            {
                _dragStartPoint = e.GetPosition(this);
                _isPotentialDrag = true;
                DynamicIsland.CaptureMouse();
            }
        }

        private void Window_MouseMove(object? sender, System.Windows.Input.MouseEventArgs e)
        {
            // 仅在 Ctrl+左键拖拽模式下移动窗口
            if (_isPotentialDrag && e.LeftButton == MouseButtonState.Pressed && Keyboard.Modifiers == ModifierKeys.Control)
            {
                var currentPoint = e.GetPosition(this);
                if (Math.Abs(currentPoint.X - _dragStartPoint.X) > 5 || 
                    Math.Abs(currentPoint.Y - _dragStartPoint.Y) > 5)
                {
                    _isPotentialDrag = false;
                    DynamicIsland.ReleaseMouseCapture();
                    try { DragMove(); } catch { }
                }
            }
            else if (_isPotentialDrag && Keyboard.Modifiers != ModifierKeys.Control)
            {
                // 如果松开 Ctrl 键，取消拖拽
                _isPotentialDrag = false;
                DynamicIsland.ReleaseMouseCapture();
            }
        }

        private void Window_MouseUp(object? sender, MouseButtonEventArgs e)
        {
            if (_isPotentialDrag)
            {
                _isPotentialDrag = false;
                DynamicIsland.ReleaseMouseCapture();
            }
        }

        private void DynamicIsland_MouseDown(object? sender, MouseButtonEventArgs e)
        {
            // Ctrl+左键：拖拽窗口
            if (e.LeftButton == MouseButtonState.Pressed &&
                Keyboard.Modifiers == ModifierKeys.Control)
            {
                DragMove();
            }
        }

        private void DynamicIsland_MouseEnter(object? sender, System.Windows.Input.MouseEventArgs e)
        {
        }

        private void DynamicIsland_MouseLeave(object? sender, System.Windows.Input.MouseEventArgs e)
        {
        }
    }
}


