using DisciplineApp.Models;

namespace DisciplineApp.Models;

public class GuestDayJournal
{
    public DateTime Date { get; set; }
    public string Vow { get; set; } = string.Empty;
    public string OneThing { get; set; } = string.Empty;
    public int Mood { get; set; }
    public bool HasMood { get; set; }
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
        int? mood,
        string? note)
    {
        var today = utcNow.Date;
        var existing = days.FirstOrDefault(d => d.Date.Date == today);
        if (existing == null)
        {
            existing = new GuestDayJournal { Date = today };
            days.Add(existing);
        }

        if (vow != null) existing.Vow = InputGuard.Clamp(vow, InputGuard.VowMax);
        if (oneThing != null) existing.OneThing = InputGuard.Clamp(oneThing, InputGuard.TitleMax);
        if (mood.HasValue)
        {
            existing.Mood = Math.Clamp(mood.Value, 1, 5);
            existing.HasMood = true;
        }
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
        DateTime utcNow,
        IEnumerable<GuestFocusSession>? sessions = null)
    {
        var weekStart = HabitMath.WeekStart(utcNow);
        var weekEnd = weekStart.AddDays(7);
        var weekDays = days.Where(d => d.Date.Date >= weekStart && d.Date.Date < weekEnd).ToList();
        var moodDays = weekDays.Where(d => d.HasMood).ToList();
        var weekTasks = tasks.Where(t =>
            (t.CompletedAt.HasValue && t.CompletedAt.Value.Date >= weekStart && t.CompletedAt.Value.Date < weekEnd)
            || (t.IsCompleted && t.Date.Date >= weekStart && t.Date.Date < weekEnd)).ToList();
        var weekFocus = sessions?
            .Where(s =>
            {
                var day = s.EndTime.Kind == DateTimeKind.Utc ? s.EndTime.Date : s.EndTime.ToUniversalTime().Date;
                return day >= weekStart && day < weekEnd;
            })
            .Sum(s => s.DurationMinutes) ?? 0;

        var weekFocusSessions = sessions ?? Array.Empty<GuestFocusSession>();
        var (bestDay, bestMinutes) = GuestFocusLogic.BestDay(weekFocusSessions, weekStart);

        return new WeeklyReview
        {
            HabitChecks = habits.Sum(h => HabitMath.ChecksThisWeek(h, weekStart)),
            TasksCompleted = weekTasks.Count,
            SessionCount = GuestFocusLogic.WeekSessionCount(weekFocusSessions, weekStart),
            VowDays = weekDays.Count(d => !string.IsNullOrWhiteSpace(d.Vow) || !string.IsNullOrWhiteSpace(d.OneThing)),
            MoodCheckIns = moodDays.Count,
            AverageMood = MoodMath.AverageRounded(moodDays.Select(d => d.Mood)),
            CurrentStreak = HabitMath.LongestOpenStreak(habits.SelectMany(h => h.Logs.Select(l => l.Date)), utcNow),
            FocusMinutes = weekFocus,
            BestDay = bestDay,
            BestDayMinutes = bestMinutes,
            TopFocus = GuestFocusLogic.TopTaggedFocus(weekFocusSessions, weekStart)
        };
    }
}

public static class MoodMath
{
    public static string Emoji(int mood) => mood switch
    {
        1 => "😞",
        2 => "😕",
        3 => "😐",
        4 => "🙂",
        5 => "😄",
        _ => "😐"
    };

    public static int AverageRounded(IEnumerable<int> moods)
    {
        var values = moods.Where(m => m is >= 1 and <= 5).ToList();
        if (values.Count == 0) return 0;
        return (int)Math.Round(values.Average(), MidpointRounding.AwayFromZero);
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

    public static (StreakStatus Status, int Streak) ActivityStreakStatus(IEnumerable<DateTime> days, DateTime utcNow)
    {
        var dates = days.Select(d => d.Date).Distinct().ToHashSet();
        if (dates.Count == 0) return (StreakStatus.None, 0);

        var today = utcNow.Date;
        var last = dates.Max();
        if (last == today) return (StreakStatus.Active, CountBack(dates, today));
        if (last == today.AddDays(-1)) return (StreakStatus.Pending, CountBack(dates, last));
        if (last == today.AddDays(-2)) return (StreakStatus.AtRisk, CountBack(dates, last));
        return (StreakStatus.Broken, 0);
    }

    private static int CountBack(HashSet<DateTime> dates, DateTime start)
    {
        var streak = 0;
        var cursor = start.Date;
        while (dates.Contains(cursor))
        {
            streak++;
            cursor = cursor.AddDays(-1);
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
