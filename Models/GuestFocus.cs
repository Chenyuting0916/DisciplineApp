namespace DisciplineApp.Models;

public class GuestFocusSession
{
    public int Id { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public double DurationMinutes { get; set; }
    public string TaskTag { get; set; } = string.Empty;
    public bool IsPomodoro { get; set; }
}

public class GuestFocusStore
{
    public int DailyGoalMinutes { get; set; } = GuestFocusLogic.DefaultGoal;
    public List<GuestFocusSession> Sessions { get; set; } = new();
}

public static class GuestFocusLogic
{
    public const int GoalMin = 10;
    public const int GoalMax = 720;
    public const int DefaultGoal = 60;
    public const double MinRecordMinutes = 0.1;
    public const double MaxRecordMinutes = 720;

    public static int ClampGoal(int minutes)
        => Math.Clamp(minutes <= 0 ? DefaultGoal : minutes, GoalMin, GoalMax);

    public static bool JustReachedGoal(double beforeMinutes, double afterMinutes, int goalMinutes)
    {
        var goal = ClampGoal(goalMinutes);
        return beforeMinutes < goal && afterMinutes >= goal;
    }

    public static GuestFocusSession? Record(
        List<GuestFocusSession> sessions,
        double minutes,
        string? task,
        bool isPomodoro,
        DateTime utcNow,
        DateTime? endedAt = null)
    {
        if (minutes < MinRecordMinutes) return null;
        var duration = Math.Clamp(minutes, MinRecordMinutes, MaxRecordMinutes);
        var end = NormalizeUtc(endedAt ?? utcNow);
        var start = end.AddMinutes(-duration);
        var session = new GuestFocusSession
        {
            Id = sessions.Count == 0 ? 1 : sessions.Max(s => s.Id) + 1,
            StartTime = start,
            EndTime = end,
            DurationMinutes = Math.Round(duration, 2),
            TaskTag = InputGuard.Clamp(task, InputGuard.FocusTaskMax),
            IsPomodoro = isPomodoro
        };
        sessions.Add(session);
        Trim(sessions);
        return session;
    }

    public static void Trim(List<GuestFocusSession> sessions)
    {
        if (sessions.Count <= InputGuard.MaxExportSessions) return;
        foreach (var extra in sessions.OrderBy(s => s.EndTime).Take(sessions.Count - InputGuard.MaxExportSessions).ToList())
        {
            sessions.Remove(extra);
        }
    }

    public static bool Delete(List<GuestFocusSession> sessions, int id)
        => id > 0 && sessions.RemoveAll(s => s.Id == id) > 0;

    public static double TodayMinutes(IEnumerable<GuestFocusSession> sessions, DateTime utcNow)
        => sessions.Where(s => NormalizeUtc(s.EndTime).Date == utcNow.Date).Sum(s => s.DurationMinutes);

    public static int TodayPomodoroCount(IEnumerable<GuestFocusSession> sessions, DateTime utcNow)
        => sessions.Count(s => s.IsPomodoro && NormalizeUtc(s.EndTime).Date == utcNow.Date);

    public static int PercentTowardGoal(double minutes, int goalMinutes)
    {
        var goal = ClampGoal(goalMinutes);
        return (int)Math.Clamp(minutes / goal * 100, 0, 100);
    }

    public static double WeekMinutes(IEnumerable<GuestFocusSession> sessions, DateTime utcNow, int weeksAgo = 0)
    {
        var weekStart = HabitMath.WeekStart(utcNow).AddDays(-7 * weeksAgo);
        var weekEnd = weekStart.AddDays(7);
        return sessions
            .Where(s =>
            {
                var day = NormalizeUtc(s.EndTime).Date;
                return day >= weekStart && day < weekEnd;
            })
            .Sum(s => s.DurationMinutes);
    }

    public static Dictionary<string, double> WeekBreakdown(IEnumerable<GuestFocusSession> sessions, DateTime weekStart)
    {
        var start = weekStart.Date;
        var end = start.AddDays(7);
        return sessions
            .Where(s =>
            {
                var day = NormalizeUtc(s.EndTime).Date;
                return day >= start && day < end;
            })
            .GroupBy(s => string.IsNullOrWhiteSpace(s.TaskTag) ? string.Empty : s.TaskTag.Trim())
            .ToDictionary(g => g.Key, g => g.Sum(s => s.DurationMinutes));
    }

    public static Dictionary<string, double> DailyActivity(IEnumerable<GuestFocusSession> sessions, DateTime utcNow, int days)
    {
        var startDate = utcNow.Date.AddDays(-(Math.Max(days, 1) - 1));
        return sessions
            .Where(s => NormalizeUtc(s.EndTime).Date >= startDate)
            .GroupBy(s => NormalizeUtc(s.EndTime).ToLocalTime().Date)
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key.ToString("MM/dd"), g => g.Sum(s => s.DurationMinutes));
    }

    public static Dictionary<DateTime, int> BuildHeatmap(
        IEnumerable<GuestFocusSession>? sessions,
        IEnumerable<UserTask>? tasks,
        IEnumerable<Habit>? habits,
        DateTime startDate)
    {
        var activity = new Dictionary<DateTime, int>();
        var start = startDate.Date;

        void Add(DateTime date, int count)
        {
            var day = date.Date;
            if (day < start) return;
            activity[day] = activity.GetValueOrDefault(day) + count;
        }

        if (sessions != null)
        {
            foreach (var session in sessions)
            {
                Add(NormalizeUtc(session.EndTime).ToLocalTime(), 1);
            }
        }

        if (tasks != null)
        {
            foreach (var task in tasks.Where(t => t.IsCompleted && t.CompletedAt.HasValue))
            {
                Add(task.CompletedAt!.Value.ToLocalTime(), 1);
            }
        }

        if (habits != null)
        {
            foreach (var date in habits.SelectMany(h => h.Logs.Select(l => l.Date)))
            {
                Add(date, 1);
            }
        }

        return activity;
    }

    public static int ActivityStreak(IEnumerable<DateTime> days, DateTime utcNow)
        => HabitMath.LongestOpenStreak(days, utcNow);

    public static FocusSession ToFocusSession(GuestFocusSession session)
        => new()
        {
            Id = session.Id,
            UserId = "guest",
            StartTime = session.StartTime,
            EndTime = session.EndTime,
            DurationMinutes = session.DurationMinutes,
            TaskTag = session.TaskTag,
            IsPomodoro = session.IsPomodoro
        };

    private static DateTime NormalizeUtc(DateTime value)
        => value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
}
