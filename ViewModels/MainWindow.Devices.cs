using System;
using System.Windows;
using System.Windows.Media;

namespace WinIsland
{
    public partial class MainWindow
    {
        private Windows.Devices.Enumeration.DeviceWatcher? _bluetoothWatcher;
        private Windows.Devices.Enumeration.DeviceWatcher? _usbWatcher;
        private System.Collections.Concurrent.ConcurrentDictionary<string, string> _deviceMap = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();
        private System.Collections.Concurrent.ConcurrentDictionary<string, (bool isConnected, DateTime lastUpdate)> _deviceStateCache = new System.Collections.Concurrent.ConcurrentDictionary<string, (bool, DateTime)>();
        private bool _isBluetoothEnumComplete = false;
        private bool _isUsbEnumComplete = false;

        private void InitializeDeviceWatcher()
        {
            try
            {
                LogDebug("Initializing Device Watchers...");

                string bluetoothSelector = "System.Devices.Aep.ProtocolId:=\"{e0cbf06c-cd8b-4647-bb8a-263b43f0f974}\"";
                var requestedProperties = new string[]
                {
                    "System.Devices.Aep.IsConnected",
                    "System.Devices.Aep.SignalStrength",
                    "System.Devices.Aep.Bluetooth.Le.IsConnectable"
                };

                _bluetoothWatcher = Windows.Devices.Enumeration.DeviceInformation.CreateWatcher(
                    bluetoothSelector,
                    requestedProperties,
                    Windows.Devices.Enumeration.DeviceInformationKind.AssociationEndpoint);

                _bluetoothWatcher.Added += BluetoothWatcher_Added;
                _bluetoothWatcher.Removed += BluetoothWatcher_Removed;
                _bluetoothWatcher.Updated += BluetoothWatcher_Updated;
                _bluetoothWatcher.EnumerationCompleted += (s, e) =>
                {
                    _isBluetoothEnumComplete = true;
                    LogDebug("Bluetooth enumeration completed");
                };
                _bluetoothWatcher.Start();
                LogDebug("Bluetooth watcher started");

                string usbSelector = "System.Devices.InterfaceClassGuid:=\"{a5dcbf10-6530-11d2-901f-00c04fb951ed}\""; 
                _usbWatcher = Windows.Devices.Enumeration.DeviceInformation.CreateWatcher(
                    usbSelector,
                    null,
                    Windows.Devices.Enumeration.DeviceInformationKind.DeviceInterface);

                _usbWatcher.Added += UsbWatcher_Added;
                _usbWatcher.Removed += UsbWatcher_Removed;
                _usbWatcher.EnumerationCompleted += (s, e) => { _isUsbEnumComplete = true; };
                _usbWatcher.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Device Watcher Error: {ex.Message}");
            }
        }

        private void BluetoothWatcher_Added(Windows.Devices.Enumeration.DeviceWatcher sender, Windows.Devices.Enumeration.DeviceInformation args)
        {
            LogDebug($"BT Added: {args.Name} (ID: {args.Id.Substring(0, Math.Min(30, args.Id.Length))})");

            if (string.IsNullOrEmpty(args.Name) || !IsValidDeviceName(args.Name))
                return;

            _deviceMap.TryAdd(args.Id, args.Name);

            if (args.Properties.ContainsKey("System.Devices.Aep.IsConnected"))
            {
                bool isConnected = (bool)args.Properties["System.Devices.Aep.IsConnected"];
                _deviceStateCache[args.Id] = (isConnected, DateTime.Now);
            }

            if (_isBluetoothEnumComplete && args.Properties.ContainsKey("System.Devices.Aep.IsConnected"))
            {
                bool isConnected = (bool)args.Properties["System.Devices.Aep.IsConnected"];
                if (isConnected)
                {
                    LogDebug($"BT Added Notification: {args.Name}");
                    var settings = AppSettings.Load();
                    if (settings.EnableBluetoothNotification)
                        Dispatcher.Invoke(() => ShowDeviceNotification($"蓝牙: {args.Name}", true));
                }
            }
        }

        private void BluetoothWatcher_Removed(Windows.Devices.Enumeration.DeviceWatcher sender, Windows.Devices.Enumeration.DeviceInformationUpdate args)
        {
            LogDebug($"BT Removed: ID={args.Id.Substring(0, Math.Min(30, args.Id.Length))}");

            bool wasConnected = false;
            if (_deviceStateCache.TryRemove(args.Id, out var lastState))
            {
                wasConnected = lastState.isConnected;
            }

            if (_deviceMap.TryRemove(args.Id, out var deviceName))
            {
                if (_isBluetoothEnumComplete && !string.IsNullOrEmpty(deviceName) && wasConnected)
                {
                    LogDebug($"BT Removed Notification: {deviceName}");
                    var settings = AppSettings.Load();
                    if (settings.EnableBluetoothNotification)
                        Dispatcher.Invoke(() => ShowDeviceNotification($"蓝牙: {deviceName}", false));
                }
            }
        }

        private void BluetoothWatcher_Updated(Windows.Devices.Enumeration.DeviceWatcher sender, Windows.Devices.Enumeration.DeviceInformationUpdate args)
        {
            if (args.Properties.ContainsKey("System.Devices.Aep.IsConnected"))
            {
                bool isConnected = (bool)args.Properties["System.Devices.Aep.IsConnected"];

                if (_deviceMap.TryGetValue(args.Id, out var deviceName) && !string.IsNullOrEmpty(deviceName))
                {
                    if (!IsValidDeviceName(deviceName)) return;

                    var now = DateTime.Now;
                    bool shouldNotify = false;

                    if (_deviceStateCache.TryGetValue(args.Id, out var cachedState))
                    {
                        if (cachedState.isConnected == isConnected) return;
                        if ((now - cachedState.lastUpdate).TotalSeconds < 2) return;
                        shouldNotify = true;
                    }
                    else
                    {
                        shouldNotify = isConnected;
                    }

                    _deviceStateCache[args.Id] = (isConnected, now);

                    if (shouldNotify)
                    {
                        LogDebug($"BT Updated Notification: {deviceName} -> {(isConnected ? "Connected" : "Disconnected")}");
                        var settings = AppSettings.Load();
                        if (settings.EnableBluetoothNotification)
                            Dispatcher.Invoke(() => ShowDeviceNotification($"蓝牙: {deviceName}", isConnected));
                    }
                }
            }
        }

        private bool IsValidDeviceName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            if (name.Length < 4) return false;
            if (System.Text.RegularExpressions.Regex.IsMatch(name, @"^[A-Z0-9]{4,6}$")) return false;
            if (name.Contains("\\") || name.Contains("{") || name.Contains("}")) return false;
            return true;
        }

        private void UsbWatcher_Added(Windows.Devices.Enumeration.DeviceWatcher sender, Windows.Devices.Enumeration.DeviceInformation args)
        {
            _deviceMap.TryAdd(args.Id, args.Name);

            if (_isUsbEnumComplete && !string.IsNullOrEmpty(args.Name))
            {
                var settings = AppSettings.Load();
                if (settings.EnableUsbNotification)
                    Dispatcher.Invoke(() => ShowDeviceNotification($"USB: {args.Name}", true));
            }
        }

        private void UsbWatcher_Removed(Windows.Devices.Enumeration.DeviceWatcher sender, Windows.Devices.Enumeration.DeviceInformationUpdate args)
        {
            if (_deviceMap.TryRemove(args.Id, out var deviceName))
            {
                if (_isUsbEnumComplete && !string.IsNullOrEmpty(deviceName))
                {
                    var settings = AppSettings.Load();
                    if (settings.EnableUsbNotification)
                        Dispatcher.Invoke(() => ShowDeviceNotification($"USB: {deviceName}", false));
                }
            }
        }

        private void ShowDeviceNotification(string deviceName, bool isConnected)
        {
            ActivateNotification(50, 320);

            string type = deviceName.Contains("蓝牙") ? "BLUETOOTH" : (deviceName.Contains("USB") ? "USB DEVICE" : "SYSTEM");
            string name = deviceName.Replace("蓝牙: ", "").Replace("USB: ", "");

            NotificationTitle.Text = type;
            NotificationBody.Text = name;

            if (isConnected)
            {
                IconConnect.Visibility = Visibility.Visible;
                IconDisconnect.Visibility = Visibility.Collapsed;
                IconMessage.Visibility = Visibility.Collapsed;
                NotificationBody.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 255, 204));
            }
            else
            {
                IconConnect.Visibility = Visibility.Collapsed;
                IconDisconnect.Visibility = Visibility.Visible;
                IconMessage.Visibility = Visibility.Collapsed;
                NotificationBody.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 51, 51)); 
            }

            PlayFlipAnimation();
        }
    }
}
