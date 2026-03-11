using System.IO;
using System.Text.Json;
using System.Windows.Media.Effects;

namespace WinIsland
{
    public class AppSettings
    {
        public bool DrinkWaterEnabled { get; set; } = false;
        public int DrinkWaterIntervalMinutes { get; set; } = 30;
        public bool TodoEnabled { get; set; } = false;
        public string DrinkWaterStartTime { get; set; } = "09:00";
        public string DrinkWaterEndTime { get; set; } = "22:00";
        public DrinkWaterMode DrinkWaterMode { get; set; } = DrinkWaterMode.Interval;
        public List<string> CustomDrinkWaterTimes { get; set; } = new List<string>();
        public List<TodoItem> TodoList { get; set; } = new List<TodoItem>();
        public bool CleanSystemNotification { get; set; } = false;

        // 新增配置项
        public bool EnableBluetoothNotification { get; set; } = true;
        public bool EnableUsbNotification { get; set; } = true;
        public bool EnableMessageNotification { get; set; } = true;
        public bool ShowMediaPlayer { get; set; } = true;
        public bool ShowVisualizer { get; set; } = true;

        // 新增 极客与专注 配置
        public bool EnableSystemMonitor { get; set; } = false; // 系统监控 (替换待机黑条)
        public bool EnableFocusMode { get; set; } = true;      // 专注模式 (番茄钟)
        public int PomodoroDurationMinutes { get; set; } = 25; // 专注时长 (分钟)
        public bool EnableAutoHide { get; set; } = false;      // 智能隐身

        // 字体配置
        public string FontFamily { get; set; } = "Segoe UI";
        public double FontSize { get; set; } = 14;
        public string DefaultFontFamily { get; set; } = "Segoe UI";
        public double DefaultFontSize { get; set; } = 12;


        private static string ConfigPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch { }
            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, json);
            }
            catch { }
        }
    }

    public class TodoItem
    {
        public DateTime ReminderTime { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
    }
}
