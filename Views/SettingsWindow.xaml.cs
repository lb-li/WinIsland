using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WinIsland
{
    public partial class SettingsWindow : Window
    {
        private enum TimeTarget
        {
            DrinkStart,
            DrinkEnd,
            CustomDrink,
            TodoTime
        }

        private TimeTarget _activeTimeTarget;
        private readonly Dictionary<int, System.Windows.Controls.Button> _hourButtons = new();
        private int _dialHour;
        private int _dialMinute;

        private string _drinkStartTime = "09:00";
        private string _drinkEndTime = "22:00";
        private string? _selectedCustomDrinkTime;

        private DateTime _todoDate = DateTime.Today;
        private TimeSpan _todoTime = new TimeSpan(9, 0, 0);
        private DateTime _calendarMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

        public SettingsWindow()
        {
            InitializeComponent();
            InitializeTimeDial();
            LoadSettings();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void InitializeTimeDial()
        {
            BuildDialHours();
            SetDialTime(9, 0);
        }

        private void BuildDialHours()
        {
            TimeDialCanvas.Children.Clear();
            _hourButtons.Clear();

            const double center = 150;
            const double ringR = 104;

            for (int hour = 0; hour < 24; hour++)
            {
                double angle = (hour / 24.0) * 2 * Math.PI - Math.PI / 2;

                double x = center + ringR * Math.Cos(angle) - 12;
                double y = center + ringR * Math.Sin(angle) - 10;

                var btn = new System.Windows.Controls.Button
                {
                    Content = hour.ToString("00"),
                    Width = 24,
                    Height = 20,
                    Tag = hour,
                    Style = (Style)FindResource("DialHourButton"),
                    FontSize = 11,
                    Padding = new Thickness(2, 0, 2, 0)
                };

                btn.Click += DialHour_Click;
                Canvas.SetLeft(btn, x);
                Canvas.SetTop(btn, y);
                TimeDialCanvas.Children.Add(btn);
                _hourButtons[hour] = btn;
            }
        }

        private void DialHour_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is int hour)
            {
                _dialHour = hour;
                UpdateDialVisuals();
            }
        }

        private void SetDialTime(int hour, int minute)
        {
            _dialHour = Math.Clamp(hour, 0, 23);
            _dialMinute = Math.Clamp(minute, 0, 59);
            UpdateDialVisuals();
        }

        private void UpdateDialVisuals()
        {
            TxtDialValue.Text = $"{_dialHour:00}:{_dialMinute:00}";

            foreach (var kv in _hourButtons)
            {
                kv.Value.Background = kv.Key == _dialHour ? System.Windows.Media.Brushes.Black : System.Windows.Media.Brushes.White;
                kv.Value.Foreground = kv.Key == _dialHour ? System.Windows.Media.Brushes.White : System.Windows.Media.Brushes.Black;
            }
            TxtMinuteValue.Text = _dialMinute.ToString("00");
        }

        private void OpenTimeDial(TimeTarget target, string currentValue, string title)
        {
            _activeTimeTarget = target;
            TxtDialTitle.Text = title;

            int hour = 9, minute = 0;
            if (TimeSpan.TryParse(currentValue, out var ts))
            {
                hour = ts.Hours;
                minute = ts.Minutes;
            }
            SetDialTime(hour, minute);

            PopupTodoDate.IsOpen = false;
            PopupTimeDial.IsOpen = true;
        }

        private void BtnDrinkStartTime_Click(object sender, RoutedEventArgs e) => OpenTimeDial(TimeTarget.DrinkStart, _drinkStartTime, "选择开始时间");
        private void BtnDrinkEndTime_Click(object sender, RoutedEventArgs e) => OpenTimeDial(TimeTarget.DrinkEnd, _drinkEndTime, "选择结束时间");
        private void BtnCustomDrinkTime_Click(object sender, RoutedEventArgs e)
        {
            var current = _selectedCustomDrinkTime ?? "09:00";
            OpenTimeDial(TimeTarget.CustomDrink, current, "选择自定义时间");
        }
        private void BtnTodoTime_Click(object sender, RoutedEventArgs e) => OpenTimeDial(TimeTarget.TodoTime, BtnTodoTime.Content?.ToString() ?? "09:00", "选择待办时间");

        private void BtnMinuteDown_Click(object sender, RoutedEventArgs e)
        {
            _dialMinute = (_dialMinute + 59) % 60;
            UpdateDialVisuals();
        }

        private void BtnMinuteUp_Click(object sender, RoutedEventArgs e)
        {
            _dialMinute = (_dialMinute + 1) % 60;
            UpdateDialVisuals();
        }

        private void TxtMinuteValue_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = new System.Text.RegularExpressions.Regex("[^0-9]+").IsMatch(e.Text);
        }

        private void TxtMinuteValue_LostFocus(object sender, RoutedEventArgs e)
        {
            ApplyMinuteFromInput();
        }

        private void TxtMinuteValue_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ApplyMinuteFromInput();
            }
        }

        private void ApplyMinuteFromInput()
        {
            int minute = _dialMinute;
            if (!int.TryParse(TxtMinuteValue.Text, out minute))
            {
                minute = _dialMinute;
            }

            _dialMinute = Math.Clamp(minute, 0, 59);
            UpdateDialVisuals();
        }

        private void BtnDialCancel_Click(object sender, RoutedEventArgs e)
        {
            PopupTimeDial.IsOpen = false;
        }

        private void BtnDialOk_Click(object sender, RoutedEventArgs e)
        {
            string value = $"{_dialHour:00}:{_dialMinute:00}";
            switch (_activeTimeTarget)
            {
                case TimeTarget.DrinkStart:
                    _drinkStartTime = value;
                    BtnDrinkStartTime.Content = value;
                    break;
                case TimeTarget.DrinkEnd:
                    _drinkEndTime = value;
                    BtnDrinkEndTime.Content = value;
                    break;
                case TimeTarget.CustomDrink:
                    _selectedCustomDrinkTime = value;
                    BtnCustomDrinkTime.Content = value;
                    break;
                case TimeTarget.TodoTime:
                    BtnTodoTime.Content = value;
                    _todoTime = TimeSpan.Parse(value);
                    break;
            }

            PopupTimeDial.IsOpen = false;
        }

        private void BtnTodoDate_Click(object sender, RoutedEventArgs e)
        {
            PopupTimeDial.IsOpen = false;
            _calendarMonth = new DateTime(_todoDate.Year, _todoDate.Month, 1);
            RenderCalendar();
            PopupTodoDate.IsOpen = true;
        }

        private void BtnCalendarPrev_Click(object sender, RoutedEventArgs e)
        {
            _calendarMonth = _calendarMonth.AddMonths(-1);
            RenderCalendar();
        }

        private void BtnCalendarNext_Click(object sender, RoutedEventArgs e)
        {
            _calendarMonth = _calendarMonth.AddMonths(1);
            RenderCalendar();
        }

        private void RenderCalendar()
        {
            TxtCalendarTitle.Text = _calendarMonth.ToString("yyyy-MM");
            CalendarDayGrid.Children.Clear();

            string[] week = { "一", "二", "三", "四", "五", "六", "日" };
            foreach (var w in week)
            {
                CalendarDayGrid.Children.Add(new TextBlock
                {
                    Text = w,
                    FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(2)
                });
            }

            int firstDay = ((int)_calendarMonth.DayOfWeek + 6) % 7;
            for (int i = 0; i < firstDay; i++)
            {
                CalendarDayGrid.Children.Add(new Border());
            }

            int days = DateTime.DaysInMonth(_calendarMonth.Year, _calendarMonth.Month);
            for (int d = 1; d <= days; d++)
            {
                var date = new DateTime(_calendarMonth.Year, _calendarMonth.Month, d);
                var btn = new System.Windows.Controls.Button
                {
                    Content = d.ToString("00"),
                    Tag = date,
                    Style = (Style)FindResource("PopupOptionButton"),
                    Margin = new Thickness(2),
                    Padding = new Thickness(4, 3, 4, 3)
                };

                if (date.Date == _todoDate.Date)
                {
                    btn.Background = System.Windows.Media.Brushes.Black;
                    btn.Foreground = System.Windows.Media.Brushes.White;
                }

                btn.Click += (_, _) =>
                {
                    _todoDate = date.Date;
                    BtnTodoDate.Content = _todoDate.ToString("yyyy-MM-dd");
                    PopupTodoDate.IsOpen = false;
                };

                CalendarDayGrid.Children.Add(btn);
            }

            int total = firstDay + days;
            int remain = (7 - (total % 7)) % 7;
            for (int i = 0; i < remain; i++)
            {
                CalendarDayGrid.Children.Add(new Border());
            }
        }

        private void LoadSettings()
        {
            var settings = AppSettings.Load();

            ChkStartWithWindows.IsChecked = IsStartupEnabled();
            ChkEnableAutoHide.IsChecked = settings.EnableAutoHide;

            ChkCleanSystemNotification.IsChecked = settings.CleanSystemNotification;
            ChkBluetoothNotification.IsChecked = settings.EnableBluetoothNotification;
            ChkUsbNotification.IsChecked = settings.EnableUsbNotification;
            ChkMessageNotification.IsChecked = settings.EnableMessageNotification;

            ChkShowMediaPlayer.IsChecked = settings.ShowMediaPlayer;
            ChkShowVisualizer.IsChecked = settings.ShowVisualizer;

            ChkSystemMonitor.IsChecked = settings.EnableSystemMonitor;
            ChkFocusMode.IsChecked = settings.EnableFocusMode;
            TxtPomodoroDuration.Text = settings.PomodoroDurationMinutes.ToString();
            PanelFocusSettings.Visibility = settings.EnableFocusMode ? Visibility.Visible : Visibility.Collapsed;

            ChkDrinkWater.IsChecked = settings.DrinkWaterEnabled;
            TxtDrinkWaterInterval.Text = settings.DrinkWaterIntervalMinutes.ToString();
            _drinkStartTime = settings.DrinkWaterStartTime;
            _drinkEndTime = settings.DrinkWaterEndTime;
            BtnDrinkStartTime.Content = _drinkStartTime;
            BtnDrinkEndTime.Content = _drinkEndTime;

            if (settings.DrinkWaterMode == DrinkWaterMode.Custom)
                RbModeCustom.IsChecked = true;
            else
                RbModeInterval.IsChecked = true;

            ListCustomDrinkTimes.ItemsSource = settings.CustomDrinkWaterTimes;

            ChkTodo.IsChecked = settings.TodoEnabled;
            ListTodo.ItemsSource = settings.TodoList;

            _todoDate = DateTime.Today;
            _todoTime = new TimeSpan(9, 0, 0);
            BtnTodoDate.Content = _todoDate.ToString("yyyy-MM-dd");
            BtnTodoTime.Content = _todoTime.ToString("hh\\:mm");

            UpdateDrinkWaterUI();
            UpdateTodoUI();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (ChkStartWithWindows.IsChecked == true) EnableStartup();
            else DisableStartup();

            var settings = AppSettings.Load();

            settings.EnableAutoHide = ChkEnableAutoHide.IsChecked == true;
            settings.CleanSystemNotification = ChkCleanSystemNotification.IsChecked == true;
            settings.EnableBluetoothNotification = ChkBluetoothNotification.IsChecked == true;
            settings.EnableUsbNotification = ChkUsbNotification.IsChecked == true;
            settings.EnableMessageNotification = ChkMessageNotification.IsChecked == true;
            settings.ShowMediaPlayer = ChkShowMediaPlayer.IsChecked == true;
            settings.ShowVisualizer = ChkShowVisualizer.IsChecked == true;

            settings.EnableSystemMonitor = ChkSystemMonitor.IsChecked == true;
            settings.EnableFocusMode = ChkFocusMode.IsChecked == true;
            if (int.TryParse(TxtPomodoroDuration.Text, out int minutes))
            {
                settings.PomodoroDurationMinutes = Math.Max(1, Math.Min(180, minutes));
            }

            settings.DrinkWaterEnabled = ChkDrinkWater.IsChecked == true;
            if (int.TryParse(TxtDrinkWaterInterval.Text, out int interval))
            {
                settings.DrinkWaterIntervalMinutes = Math.Max(1, interval);
            }
            settings.DrinkWaterStartTime = _drinkStartTime;
            settings.DrinkWaterEndTime = _drinkEndTime;
            settings.DrinkWaterMode = RbModeCustom.IsChecked == true ? DrinkWaterMode.Custom : DrinkWaterMode.Interval;

            settings.TodoEnabled = ChkTodo.IsChecked == true;

            settings.Save();

            var mainWindow = (Owner as MainWindow) ?? (System.Windows.Application.Current.MainWindow as MainWindow);
            mainWindow?.ReloadSettings();

            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private bool IsStartupEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
                return key?.GetValue("WinIsland") != null;
            }
            catch { return false; }
        }

        private void EnableStartup()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                key?.SetValue("WinIsland", System.Reflection.Assembly.GetExecutingAssembly().Location);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"无法启用开机自启: {ex.Message}");
            }
        }

        private void DisableStartup()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                key?.DeleteValue("WinIsland", false);
            }
            catch { }
        }

        private void ChkDrinkWater_Checked(object sender, RoutedEventArgs e) => UpdateDrinkWaterUI();
        private void ChkDrinkWater_Unchecked(object sender, RoutedEventArgs e) => UpdateDrinkWaterUI();
        private void RbMode_Checked(object sender, RoutedEventArgs e) => UpdateDrinkWaterUI();

        private void UpdateDrinkWaterUI()
        {
            if (PanelDrinkWaterSettings == null) return;

            if (ChkDrinkWater.IsChecked == true)
            {
                PanelDrinkWaterSettings.Visibility = Visibility.Visible;
                if (RbModeCustom.IsChecked == true)
                {
                    PanelModeInterval.Visibility = Visibility.Collapsed;
                    PanelModeCustom.Visibility = Visibility.Visible;
                }
                else
                {
                    PanelModeInterval.Visibility = Visibility.Visible;
                    PanelModeCustom.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                PanelDrinkWaterSettings.Visibility = Visibility.Collapsed;
            }
        }

        private void ChkTodo_Checked(object sender, RoutedEventArgs e) => UpdateTodoUI();
        private void ChkTodo_Unchecked(object sender, RoutedEventArgs e) => UpdateTodoUI();

        private void UpdateTodoUI()
        {
            if (PanelTodo == null) return;
            PanelTodo.Visibility = ChkTodo.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ChkCleanSystemNotification_Checked(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show(
                "开启后，WinIsland 会自动清理系统通知中心中的已接管通知。\n\n建议同时启用 Windows 的勿扰/专注模式。",
                "操作建议",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void BtnAddCustomDrinkTime_Click(object sender, RoutedEventArgs e)
        {
            string formattedTime = _selectedCustomDrinkTime ?? "";
            if (string.IsNullOrEmpty(formattedTime)) return;

            var currentList = (List<string>)ListCustomDrinkTimes.ItemsSource ?? new List<string>();
            if (!currentList.Contains(formattedTime))
            {
                currentList.Add(formattedTime);
                currentList.Sort();

                ListCustomDrinkTimes.ItemsSource = null;
                ListCustomDrinkTimes.ItemsSource = currentList;

                var s = AppSettings.Load();
                s.CustomDrinkWaterTimes = currentList;
                s.Save();
            }

            _selectedCustomDrinkTime = null;
            BtnCustomDrinkTime.Content = "选择时间";
        }

        private void BtnDeleteCustomDrinkTime_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is string timeStr)
            {
                var currentList = (List<string>)ListCustomDrinkTimes.ItemsSource;
                if (currentList != null && currentList.Remove(timeStr))
                {
                    ListCustomDrinkTimes.ItemsSource = null;
                    ListCustomDrinkTimes.ItemsSource = currentList;

                    var s = AppSettings.Load();
                    s.CustomDrinkWaterTimes = currentList;
                    s.Save();
                }
            }
        }

        private bool TryGetSelectedTodoDateTime(out DateTime value)
        {
            value = _todoDate.Date + _todoTime;
            return true;
        }

        private void BtnAddTodo_Click(object sender, RoutedEventArgs e)
        {
            if (!TryGetSelectedTodoDateTime(out DateTime reminderTime)) return;
            if (string.IsNullOrWhiteSpace(TxtTodoContent.Text)) return;

            var newItem = new TodoItem
            {
                ReminderTime = reminderTime,
                Content = TxtTodoContent.Text,
                IsCompleted = false
            };

            var currentList = (List<TodoItem>)ListTodo.ItemsSource ?? new List<TodoItem>();
            currentList.Add(newItem);
            currentList.Sort((a, b) => a.ReminderTime.CompareTo(b.ReminderTime));

            ListTodo.ItemsSource = null;
            ListTodo.ItemsSource = currentList;

            var s = AppSettings.Load();
            s.TodoList = currentList;
            s.Save();

            TxtTodoContent.Text = "";
        }

        private void BtnDeleteTodo_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is TodoItem item)
            {
                var currentList = (List<TodoItem>)ListTodo.ItemsSource;
                if (currentList != null)
                {
                    currentList.RemoveAll(x => x.Content == item.Content && x.ReminderTime == item.ReminderTime);

                    ListTodo.ItemsSource = null;
                    ListTodo.ItemsSource = currentList;

                    var s = AppSettings.Load();
                    s.TodoList = currentList;
                    s.Save();
                }
            }
        }

        private void ChkFocusMode_Checked(object sender, RoutedEventArgs e)
        {
            if (PanelFocusSettings == null) return;
            PanelFocusSettings.Visibility = ChkFocusMode.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            e.Handled = new System.Text.RegularExpressions.Regex("[^0-9]+").IsMatch(e.Text);
        }
    }
}
