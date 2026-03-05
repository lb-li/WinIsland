using System;
using System.Windows.Threading;

namespace WinIsland.Services.FocusTimer
{
    public enum PomodoroState
    {
        Idle,       // 空闲
        Running,    // 专注进行中
        Paused,     // 暂停
        Finished    // 完成/休息时间
    }

    /// <summary>
    /// 番茄钟服务
    /// 管理专注倒计时逻辑
    /// </summary>
    public class PomodoroService
    {
        private DispatcherTimer _timer;
        private TimeSpan _remainingTime;
        private TimeSpan _totalTime;

        public PomodoroState CurrentState { get; private set; } = PomodoroState.Idle;
        
        // 默认 25 分钟
        public int DefaultFocusMinutes { get; set; } = 25;

        // 事件：每秒触发，返回 (剩余时间, 进度百分比 0~1)
        public event Action<TimeSpan, double>? OnTick;
        public event Action<PomodoroState>? OnStateChanged;

        public PomodoroService()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
        }

        public void StartFocus(int minutes = -1)
        {
            if (minutes <= 0) minutes = DefaultFocusMinutes;

            _totalTime = TimeSpan.FromMinutes(minutes);
            _remainingTime = _totalTime;
            
            CurrentState = PomodoroState.Running;
            _timer.Start();
            
            OnStateChanged?.Invoke(CurrentState);
            // 立即触发一次更新 UI
            InvokeTick();
        }

        public void TogglePause()
        {
            if (CurrentState == PomodoroState.Running)
            {
                CurrentState = PomodoroState.Paused;
                _timer.Stop();
            }
            else if (CurrentState == PomodoroState.Paused)
            {
                CurrentState = PomodoroState.Running;
                _timer.Start();
            }
            OnStateChanged?.Invoke(CurrentState);
        }

        public void Stop()
        {
            CurrentState = PomodoroState.Idle;
            _timer.Stop();
            OnStateChanged?.Invoke(CurrentState);
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (CurrentState != PomodoroState.Running) return;

            _remainingTime = _remainingTime.Subtract(TimeSpan.FromSeconds(1));

            if (_remainingTime.TotalSeconds <= 0)
            {
                // 完成
                _remainingTime = TimeSpan.Zero;
                CurrentState = PomodoroState.Finished;
                _timer.Stop();
                OnStateChanged?.Invoke(CurrentState);
            }
            
            InvokeTick();
        }

        private void InvokeTick()
        {
            double progress = 0;
            if (_totalTime.TotalSeconds > 0)
            {
                progress = 1.0 - (_remainingTime.TotalSeconds / _totalTime.TotalSeconds);
            }
            OnTick?.Invoke(_remainingTime, progress);
        }

        public bool IsActive => CurrentState == PomodoroState.Running || CurrentState == PomodoroState.Paused;
    }
}
