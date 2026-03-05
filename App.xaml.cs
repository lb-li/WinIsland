using Hardcodet.Wpf.TaskbarNotification;
using System.Windows;
using System.Windows.Controls;

namespace WinIsland
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private TaskbarIcon? _trayIcon;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 创建托盘图标
            _trayIcon = new TaskbarIcon
            {
                Icon = LoadIconFromResource("ico.ico"),
                ToolTipText = "WinIsland - 灵动岛",
                ContextMenu = (ContextMenu)FindResource("TrayMenu")
            };
            _trayIcon.TrayLeftMouseDown += (s, args) => MainWindow?.Activate();
        }

        private System.Drawing.Icon LoadIconFromResource(string resourceName)
        {
            try
            {
                Uri uri = new Uri($"pack://application:,,,/{resourceName}");
                var streamInfo = GetResourceStream(uri);
                if (streamInfo != null)
                {
                    return new System.Drawing.Icon(streamInfo.Stream);
                }
            }
            catch { }

            // 如果加载失败，使用系统默认图标
            return System.Drawing.SystemIcons.Application;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _trayIcon?.Dispose();
            base.OnExit(e);
        }

        private void Settings_Click(object? sender, RoutedEventArgs e)
        {
            // 打开设置窗口
            var settingsWindow = new SettingsWindow();
            settingsWindow.Show();
        }

        private void Exit_Click(object? sender, RoutedEventArgs e)
        {
            Shutdown();
        }
    }

}
