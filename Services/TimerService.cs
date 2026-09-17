using System;
using System.Timers;

namespace DisciplineApp.Services;

public class TimerService : IDisposable
{
    private System.Timers.Timer _timer;
    
    public TimeSpan TimeLeft { get; private set; }
    public TimeSpan TimeElapsed { get; private set; }
    public bool IsRunning { get; private set; } = false;
    public bool IsPomodoroMode { get; private set; } = true;
    public bool IsBreakMode { get; private set; }
    public bool IsLongBreak { get; private set; }
    public string FocusTask { get; set; } = "";
    public int PomodoroMinutes { get; private set; } = 25;
    public int ShortBreakMinutes { get; private set; } = 5;
    public int LongBreakMinutes { get; private set; } = 15;
    public int CompletedPomodoros { get; private set; }
    public TimeSpan DefaultPomodoroTime { get; set; } = TimeSpan.FromMinutes(25);

    public event Action? OnTick;
    public event Action? OnTimerCompleted;

    public TimerService()
    {
        _timer = new System.Timers.Timer(1000);
        _timer.Elapsed += OnTimerElapsed;
        ResetTimer();
    }

    public double Progress
    {
        get
        {
            if (!IsPomodoroMode) return 0;
            var total = DefaultPomodoroTime.TotalSeconds;
            if (total <= 0) return 0;
            return Math.Clamp(1 - (TimeLeft.TotalSeconds / total), 0, 1);
        }
    }

    public void SetMode(bool isPomodoro)
    {
        if (IsRunning) return;
        IsPomodoroMode = isPomodoro;
        IsBreakMode = false;
        DefaultPomodoroTime = TimeSpan.FromMinutes(PomodoroMinutes);
        ResetTimer();
    }

    public void SetPomodoroMinutes(int minutes)
    {
        if (IsRunning) return;
        PomodoroMinutes = Math.Clamp(minutes, 1, 180);
        if (IsPomodoroMode && !IsBreakMode)
        {
            DefaultPomodoroTime = TimeSpan.FromMinutes(PomodoroMinutes);
            ResetTimer();
        }
    }

    public void StartBreak(bool isLong)
    {
        if (IsRunning) return;
        IsPomodoroMode = true;
        IsBreakMode = true;
        IsLongBreak = isLong;
        DefaultPomodoroTime = TimeSpan.FromMinutes(isLong ? LongBreakMinutes : ShortBreakMinutes);
        ResetTimer();
    }

    public void EndBreak()
    {
        if (IsRunning) return;
        IsBreakMode = false;
        IsLongBreak = false;
        IsPomodoroMode = true;
        DefaultPomodoroTime = TimeSpan.FromMinutes(PomodoroMinutes);
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

    public void RegisterCompletedPomodoro()
    {
        CompletedPomodoros++;
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
                if (!IsBreakMode)
                {
                    CompletedPomodoros++;
                }
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
