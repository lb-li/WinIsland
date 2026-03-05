using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Windows.Threading;

namespace WinIsland.Services.SystemMonitor
{
    public struct SystemStats
    {
        public bool IsValid;
        public float CpuUsage;      // CPU 使用率 0-100
        public float RamUsage;      // 内存使用率 0-100
        public long NetUploadBps;   // 上行速度 Byte/s
        public long NetDownloadBps; // 下行速度 Byte/s

        public string GetFormattedCpu() => $"{Math.Round(CpuUsage)}%";
        public string GetFormattedRam() => $"{Math.Round(RamUsage)}%";
        public string GetFormattedUpload() => FormatSpeed(NetUploadBps);
        public string GetFormattedDownload() => FormatSpeed(NetDownloadBps);

        private static string FormatSpeed(long bps)
        {
            if (bps < 1024) return $"{bps} B/s";
            if (bps < 1024 * 1024) return $"{bps / 1024} KB/s";
            return $"{bps / 1024 / 1024} MB/s";
        }
    }

    /// <summary>
    /// 系统资源监控服务
    /// 负责获取 CPU、内存 loading 和网络流量
    /// </summary>
    public class SystemStatsService
    {
        private readonly DispatcherTimer _timer;
        public event EventHandler<SystemStats>? OnStatsUpdated;

        private PerformanceCounter? _cpuCounter;
        // 内存可以使用 PerformanceCounter 或 Microsoft.VisualBasic.Devices.ComputerInfo
        // 这里为了兼容性使用 GlobalMemoryStatusEx 的封装或者计算 Available MBytes

        // 网络接口统计
        private NetworkInterface[] _interfaces = Array.Empty<NetworkInterface>();
        private long _prevBytesSent = 0;
        private long _prevBytesReceived = 0;
        private DateTime _prevTime;

        public bool IsRunning { get; private set; }

        public SystemStatsService()
        {
            InitializeCounters();
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
        }

        // P/Invoke for accurate Physical Memory usage matching Task Manager
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        private void InitializeCounters()
        {
            try
            {
                // 获取总 CPU 使用率
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                
                // 内存改为使用 GlobalMemoryStatusEx，不再依赖 PerformanceCounter
                // _memCounter = new PerformanceCounter("Memory", "% Committed Bytes In Use");

                _interfaces = NetworkInterface.GetAllNetworkInterfaces();
                
                // 预热 Counter
                _cpuCounter.NextValue();
                UpdateNetworkStats();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SystemStats] Init Failed: {ex.Message}");
            }
        }

        public void Start()
        {
            if (IsRunning) return;
            IsRunning = true;
            _timer.Start();
        }

        public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;
            _timer.Stop();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (_cpuCounter == null) return;

            try
            {
                var stats = new SystemStats();
                stats.IsValid = true;
                stats.CpuUsage = _cpuCounter.NextValue();
                
                // 使用 Kernel32 获取真实的物理内存负载
                var memStatus = new MEMORYSTATUSEX();
                memStatus.dwLength = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(MEMORYSTATUSEX));
                if (GlobalMemoryStatusEx(ref memStatus))
                {
                    stats.RamUsage = memStatus.dwMemoryLoad; // 0-100
                }
                else
                {
                    stats.RamUsage = 0;
                }

                // 计算网速
                var (up, down) = UpdateNetworkStats();
                stats.NetUploadBps = up;
                stats.NetDownloadBps = down;

                OnStatsUpdated?.Invoke(this, stats);
            }
            catch { }
        }

        private (long uploadSpeed, long downloadSpeed) UpdateNetworkStats()
        {
            long totalSent = 0;
            long totalReceived = 0;

            try
            {
                // 动态获取接口，防止网络变动
                _interfaces = NetworkInterface.GetAllNetworkInterfaces();
                foreach (var ni in _interfaces)
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    
                    var ipStats = ni.GetIPStatistics();
                    totalSent += ipStats.BytesSent;
                    totalReceived += ipStats.BytesReceived;
                }

                var now = DateTime.Now;
                var dt = (now - _prevTime).TotalSeconds;
                if (dt <= 0) dt = 1;

                long uploadSpeed = (long)((totalSent - _prevBytesSent) / dt);
                long downloadSpeed = (long)((totalReceived - _prevBytesReceived) / dt);

                if (uploadSpeed < 0) uploadSpeed = 0;
                if (downloadSpeed < 0) downloadSpeed = 0;

                _prevBytesSent = totalSent;
                _prevBytesReceived = totalReceived;
                _prevTime = now;

                return (uploadSpeed, downloadSpeed);
            }
            catch
            {
                return (0, 0);
            }
        }
    }
}
