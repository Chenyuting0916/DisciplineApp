using DisciplineApp.Models;

namespace DisciplineApp.Models;

public class GuestDayJournal
{
    public DateTime Date { get; set; }
    public string Vow { get; set; } = string.Empty;
    public string OneThing { get; set; } = string.Empty;
    public int Mood { get; set; } = 3;
    public string? Note { get; set; }
}

public static class GuestJournalLogic
{
    public const int MaxDays = 60;

    public static GuestDayJournal? FindToday(IEnumerable<GuestDayJournal> days, DateTime utcNow)
        => days.FirstOrDefault(d => d.Date.Date == utcNow.Date);

    public static GuestDayJournal Upsert(
        List<GuestDayJournal> days,
        DateTime utcNow,
        string? vow,
        string? oneThing,
        int mood,
        string? note)
    {
        var today = utcNow.Date;
        var existing = days.FirstOrDefault(d => d.Date.Date == today);
        if (existing == null)
        {
            existing = new GuestDayJournal { Date = today, Mood = 3 };
            days.Add(existing);
        }

        if (vow != null) existing.Vow = InputGuard.Clamp(vow, InputGuard.VowMax);
        if (oneThing != null) existing.OneThing = InputGuard.Clamp(oneThing, InputGuard.TitleMax);
        existing.Mood = Math.Clamp(mood, 1, 5);
        if (note != null)
        {
            existing.Note = string.IsNullOrWhiteSpace(note) ? null : InputGuard.Clamp(note, InputGuard.NoteMax);
        }

        if (days.Count > MaxDays)
        {
            foreach (var extra in days.OrderBy(d => d.Date).Take(days.Count - MaxDays).ToList())
            {
                days.Remove(extra);
            }
        }

        return existing;
    }

    public static WeeklyReview BuildReview(
        IEnumerable<Habit> habits,
        IEnumerable<UserTask> tasks,
        IEnumerable<GuestDayJournal> days,
        DateTime utcNow)
    {
        var weekStart = HabitMath.WeekStart(utcNow);
        var weekEnd = weekStart.AddDays(7);
        var weekDays = days.Where(d => d.Date.Date >= weekStart && d.Date.Date < weekEnd).ToList();
        var weekTasks = tasks.Where(t =>
            (t.CompletedAt.HasValue && t.CompletedAt.Value.Date >= weekStart && t.CompletedAt.Value.Date < weekEnd)
            || (t.IsCompleted && t.Date.Date >= weekStart && t.Date.Date < weekEnd)).ToList();

        return new WeeklyReview
        {
            HabitChecks = habits.Sum(h => HabitMath.ChecksThisWeek(h, weekStart)),
            TasksCompleted = weekTasks.Count,
            SessionCount = weekDays.Count(d => !string.IsNullOrWhiteSpace(d.Vow) || !string.IsNullOrWhiteSpace(d.OneThing)),
            CurrentStreak = HabitMath.LongestOpenStreak(habits.SelectMany(h => h.Logs.Select(l => l.Date)), utcNow),
            FocusMinutes = 0
        };
    }
}

public static class HabitMath
{
    public static DateTime WeekStart(DateTime utcNow)
        => utcNow.Date.AddDays(-(int)utcNow.Date.DayOfWeek);

    public static int ChecksThisWeek(Habit habit, DateTime weekStart)
    {
        var end = weekStart.AddDays(7);
        return habit.Logs.Select(l => l.Date.Date).Distinct().Count(d => d >= weekStart && d < end);
    }

    public static int LongestOpenStreak(IEnumerable<DateTime> dates, DateTime utcNow)
    {
        var set = dates.Select(d => d.Date).Distinct().OrderByDescending(d => d).ToList();
        if (set.Count == 0) return 0;
        var cursor = utcNow.Date;
        if (set[0] < cursor.AddDays(-1)) return 0;
        if (set[0] == cursor.AddDays(-1)) cursor = set[0];
        int streak = 0;
        foreach (var date in set)
        {
            if (date == cursor)
            {
                streak++;
                cursor = cursor.AddDays(-1);
            }
            else if (date < cursor) break;
        }
        return streak;
    }
}

public static class BreakStretchCatalog
{
    public static readonly string[] Keys =
    {
        "StretchNeck",
        "StretchShoulders",
        "StretchStand",
        "StretchEyes",
        "StretchWalk"
    };

    public static string KeyFor(int completedPomodoros)
    {
        var index = Math.Abs(completedPomodoros) % Keys.Length;
        return Keys[index];
    }
}
