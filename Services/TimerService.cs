using System;
using System.Timers;

namespace DisciplineApp.Services;

public class TimerService : IDisposable
{
    private System.Timers.Timer _timer;
    
    // State
    public TimeSpan TimeLeft { get; private set; }
    public TimeSpan TimeElapsed { get; private set; }
    public bool IsRunning { get; private set; } = false;
    public bool IsPomodoroMode { get; private set; } = false; // Default to Stopwatch
    public string FocusTask { get; set; } = "";
    public TimeSpan DefaultPomodoroTime { get; set; } = TimeSpan.FromMinutes(25);

    // Events
    public event Action? OnTick;
    public event Action? OnTimerCompleted;

    public TimerService()
    {
        _timer = new System.Timers.Timer(1000);
        _timer.Elapsed += OnTimerElapsed;
        ResetTimer();
    }

    public void SetMode(bool isPomodoro)
    {
        if (IsRunning) return;
        IsPomodoroMode = isPomodoro;
        ResetTimer();
    }

    public void Start()
    {
        if (!IsRunning)
        {
            IsRunning = true;
            _timer.Start();
        }
    }

    public void Stop()
    {
        if (IsRunning)
        {
            IsRunning = false;
            _timer.Stop();
        }
    }

    public bool IsPaused => !IsRunning && (IsPomodoroMode ? TimeLeft < DefaultPomodoroTime : TimeElapsed > TimeSpan.Zero);

    public void ResetTimer()
    {
        TimeLeft = DefaultPomodoroTime;
        TimeElapsed = TimeSpan.Zero;
        OnTick?.Invoke();
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (IsPomodoroMode)
        {
            if (TimeLeft.TotalSeconds > 0)
            {
                TimeLeft = TimeLeft.Subtract(TimeSpan.FromSeconds(1));
            }
            else
            {
                Stop();
                OnTimerCompleted?.Invoke();
            }
        }
        else
        {
            TimeElapsed = TimeElapsed.Add(TimeSpan.FromSeconds(1));
        }
        OnTick?.Invoke();
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
