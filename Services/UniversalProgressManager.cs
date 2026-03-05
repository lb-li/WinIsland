using System.Diagnostics;
using System.Windows.Automation; // 需要引用 UIAutomationClient/Types

namespace WinIsland
{
    public class UniversalProgressDetails
    {
        public string AppName { get; set; } = "";
        public double ProgressValue { get; set; } // 0 到 100
        public bool IsActive { get; set; } // 如果进度正在进行中，则为 True
    }

    public class UniversalProgressManager : IDisposable
    {
        public event EventHandler<UniversalProgressDetails>? OnProgressChanged;

        private AutomationElement? _taskbarList;
        private System.Threading.Timer? _scanTimer;
        private bool _isScanning = false;
        private bool _disposed;

        public UniversalProgressManager()
        {
            // 初始化监控
            try
            {
                InitializeTaskbarAccess();
                // 对于 UIA 来说，轮询往往比事件更安全/简单，尤其是对于频繁更新的任务栏
                _scanTimer = new System.Threading.Timer(ScanForProgress, null, 1000, 1000);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UniversalProgress] 初始化失败: {ex.Message}");
            }
        }

        private void InitializeTaskbarAccess()
        {
            // 首先尝试查找特定的 TaskList (标准 Windows 10)
            var desktop = AutomationElement.RootElement;
            var taskbarCondition = new PropertyCondition(AutomationElement.ClassNameProperty, "Shell_TrayWnd");
            var taskbar = desktop.FindFirst(TreeScope.Children, taskbarCondition);

            if (taskbar != null)
            {
                // 为了性能，优先尝试特定类名
                var listCondition = new PropertyCondition(AutomationElement.ClassNameProperty, "MSTaskListWClass");
                _taskbarList = taskbar.FindFirst(TreeScope.Descendants, listCondition);

                // 降级方案: 如果未找到特定列表 (Win11 或其他)，则扫描整个任务栏
                if (_taskbarList == null)
                {
                    _taskbarList = taskbar;
                    Debug.WriteLine("[UniversalProgress] 未找到 'MSTaskListWClass'，使用 'Shell_TrayWnd' 作为根节点。");
                }
                else
                {
                    Debug.WriteLine("[UniversalProgress] 已找到 'MSTaskListWClass'。");
                }
            }
        }

        private void ScanForProgress(object? state)
        {
            if (_disposed || _isScanning || _taskbarList == null) return;
            _isScanning = true;

            try
            {
                // 专门搜索支持 RangeValuePattern (进度条) 的元素
                // 使用 PropertyCondition for IsRangeValuePatternAvailableProperty 会触发对该功能的搜索
                var progressCondition = new PropertyCondition(AutomationElement.IsRangeValuePatternAvailableProperty, true);

                // 由于结构各异，必须搜索后代
                var progressElements = _taskbarList.FindAll(TreeScope.Descendants, progressCondition);

                double maxProgress = -1;
                string activeAppName = "";

                foreach (AutomationElement element in progressElements)
                {
                    try
                    {
                        if (element.TryGetCurrentPattern(RangeValuePattern.Pattern, out object patternObj))
                        {
                            var rangePattern = (RangeValuePattern)patternObj;

                            // 验证它看起来像是一个有效的进度条
                            // 如果值为 0 (通常为不活动/不确定状态)，则忽略
                            if (rangePattern.Current.Maximum > 0 && rangePattern.Current.Value > 0)
                            {
                                double percent = (rangePattern.Current.Value / rangePattern.Current.Maximum) * 100;

                                // 阈值:
                                // > 0.1: 避免“刚开始”的空状态
                                // < 100: 避免“已完成”的绿色条依然存在
                                // 注意: 某些浏览器会短暂保持在 100%。我们可以放宽到 99.9%
                                if (percent > 0.1 && percent < 99.9)
                                {
                                    maxProgress = percent;
                                    activeAppName = element.Current.Name;

                                    // 优化: 找到第一个有效的就停止
                                    break;
                                }
                            }
                        }
                    }
                    catch { /* 单个元素访问错误，继续 */ }
                }

                if (maxProgress >= 0)
                {
                    OnProgressChanged?.Invoke(this, new UniversalProgressDetails
                    {
                        IsActive = true,
                        ProgressValue = maxProgress,
                        AppName = activeAppName
                    });
                }
                else
                {
                    OnProgressChanged?.Invoke(this, new UniversalProgressDetails { IsActive = false });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UniversalProgress] 扫描错误: {ex.Message}");
                // 如果遇到顶级错误 (例如任务栏重启)，重新初始化
                try { InitializeTaskbarAccess(); } catch { }
            }
            finally
            {
                _isScanning = false;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try { _scanTimer?.Dispose(); } catch { }
            _scanTimer = null;
            _taskbarList = null;
        }
    }
}
