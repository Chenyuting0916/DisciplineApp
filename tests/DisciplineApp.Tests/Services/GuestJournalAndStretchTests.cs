using DisciplineApp.Models;
using DisciplineApp.Services;
using Xunit;

namespace DisciplineApp.Tests.Services;

public class GuestJournalAndStretchTests
{
    [Fact]
    public void Upsert_ClampsFieldsAndCapsHistory()
    {
        var days = new List<GuestDayJournal>();
        var now = new DateTime(2026, 9, 17, 8, 0, 0, DateTimeKind.Utc);

        var saved = GuestJournalLogic.Upsert(days, now, new string('誓', 400), new string('a', 200), 9, new string('n', 500));
        Assert.Equal(InputGuard.VowMax, saved.Vow.Length);
        Assert.Equal(InputGuard.TitleMax, saved.OneThing.Length);
        Assert.Equal(5, saved.Mood);
        Assert.True(saved.HasMood);
        Assert.Equal(InputGuard.NoteMax, saved.Note!.Length);
        Assert.Single(days);

        for (int i = 1; i <= GuestJournalLogic.MaxDays + 5; i++)
        {
            GuestJournalLogic.Upsert(days, now.AddDays(i), "vow", "one", 3, null);
        }

        Assert.Equal(GuestJournalLogic.MaxDays, days.Count);
        Assert.DoesNotContain(days, d => d.Date.Date == now.Date);

        var vowOnly = new List<GuestDayJournal>();
        var vow = GuestJournalLogic.Upsert(vowOnly, now, "stay", "write", null, null);
        Assert.False(vow.HasMood);
        Assert.Equal(0, vow.Mood);
    }

    [Fact]
    public void BuildReview_CountsOnlyThisWeekAndOwnShapes()
    {
        var weekStart = new DateTime(2026, 9, 13); // Sunday
        var habit = new Habit
        {
            Title = "water",
            TargetDaysPerWeek = 7,
            Logs =
            {
                new HabitLog { Date = weekStart },
                new HabitLog { Date = weekStart.AddDays(1) },
                new HabitLog { Date = weekStart.AddDays(-2) }
            }
        };
        var tasks = new List<UserTask>
        {
            new() { Title = "mine", IsCompleted = true, CompletedAt = weekStart.AddDays(2), Date = weekStart.AddDays(2) },
            new() { Title = "old", IsCompleted = true, CompletedAt = weekStart.AddDays(-8), Date = weekStart.AddDays(-8) }
        };
        var days = new List<GuestDayJournal>
        {
            new() { Date = weekStart.AddDays(1), Vow = "stay", OneThing = "write" },
            new() { Date = weekStart.AddDays(2), HasMood = true, Mood = 5 },
            new() { Date = weekStart.AddDays(3), HasMood = true, Mood = 3 },
            new() { Date = weekStart.AddDays(-10), Vow = "old", HasMood = true, Mood = 1 }
        };

        var review = GuestJournalLogic.BuildReview(new[] { habit }, tasks, days, weekStart.AddDays(4));
        Assert.Equal(2, review.HabitChecks);
        Assert.Equal(1, review.TasksCompleted);
        Assert.Equal(0, review.SessionCount);
        Assert.Equal(1, review.VowDays);
        Assert.Equal(2, review.MoodCheckIns);
        Assert.Equal(4, review.AverageMood);
        Assert.Equal("🙂", MoodMath.Emoji(4));
    }

    [Fact]
    public void HabitMath_WeekProgressAndStreak()
    {
        var start = HabitMath.WeekStart(new DateTime(2026, 9, 17));
        Assert.Equal(DayOfWeek.Sunday, start.DayOfWeek);

        var habit = new Habit
        {
            Logs =
            {
                new HabitLog { Date = DateTime.UtcNow.Date },
                new HabitLog { Date = DateTime.UtcNow.Date.AddDays(-1) }
            }
        };
        Assert.True(HabitMath.ChecksThisWeek(habit, HabitMath.WeekStart(DateTime.UtcNow)) >= 1);
        Assert.Equal(2, HabitMath.LongestOpenStreak(habit.Logs.Select(l => l.Date), DateTime.UtcNow));
    }

    [Fact]
    public void BreakStretch_CyclesWithoutThrowing()
    {
        Assert.Equal("StretchNeck", BreakStretchCatalog.KeyFor(0));
        Assert.Equal("StretchShoulders", BreakStretchCatalog.KeyFor(1));
        Assert.Equal(BreakStretchCatalog.KeyFor(0), BreakStretchCatalog.KeyFor(BreakStretchCatalog.Keys.Length));
        Assert.Equal("StretchNeck", BreakStretchCatalog.KeyFor(-5));
    }

    [Fact]
    public void TimerService_ClampsCustomMinutes()
    {
        using var timer = new TimerService();
        timer.SetPomodoroMinutes(2);
        Assert.Equal(InputGuard.PomodoroMinMinutes, timer.PomodoroMinutes);
        timer.SetPomodoroMinutes(240);
        Assert.Equal(InputGuard.PomodoroMaxMinutes, timer.PomodoroMinutes);
        timer.SetPomodoroMinutes(40);
        Assert.Equal(40, timer.PomodoroMinutes);
        Assert.Equal(40, timer.DefaultPomodoroTime.TotalMinutes);
    }

    [Fact]
    public void FocusTaskMatcher_FindsOpenTaskOnly()
    {
        var tasks = new List<UserTask>
        {
            new() { Id = 1, Title = "Write", IsCompleted = false },
            new() { Id = 2, Title = "Write", IsCompleted = true },
            new() { Id = 3, Title = "Read", IsCompleted = false }
        };

        var found = FocusTaskMatcher.FindOpen(tasks, "Write", null);
        Assert.NotNull(found);
        Assert.Equal(1, found!.Id);

        Assert.Null(FocusTaskMatcher.FindOpen(tasks, "custom", "Missing"));
        var custom = FocusTaskMatcher.FindOpen(tasks, "custom", "Read");
        Assert.Equal(3, custom!.Id);
        Assert.Null(FocusTaskMatcher.FindOpen(tasks, "", null));
        Assert.Null(FocusTaskMatcher.FindOpen(null, "Write", null));
    }

    [Fact]
    public void FocusTaskMatcher_PrefillsFromOneThing()
    {
        var tasks = new List<UserTask>
        {
            new() { Id = 1, Title = "Complete testing", IsCompleted = false }
        };

        var match = FocusTaskMatcher.Prefill(tasks, "Complete testing");
        Assert.Equal("Complete testing", match.FocusTask);
        Assert.Equal("", match.CustomTitle);

        var custom = FocusTaskMatcher.Prefill(tasks, "Write the essay");
        Assert.Equal("custom", custom.FocusTask);
        Assert.Equal("Write the essay", custom.CustomTitle);

        var empty = FocusTaskMatcher.Prefill(tasks, "   ");
        Assert.Equal("", empty.FocusTask);
    }

    [Fact]
    public void FocusTaskMatcher_KeepsCustomAfterStartWhenNotListed()
    {
        var tasks = new List<UserTask>
        {
            new() { Id = 1, Title = "Inbox", IsCompleted = false }
        };

        var custom = FocusTaskMatcher.KeepAfterStart(tasks, "custom", "寫完今日報告", "寫完今日報告");
        Assert.Equal("custom", custom.FocusTask);
        Assert.Equal("寫完今日報告", custom.CustomTitle);

        var listed = FocusTaskMatcher.KeepAfterStart(tasks, "Inbox", "", "Inbox");
        Assert.Equal("Inbox", listed.FocusTask);

        var none = FocusTaskMatcher.KeepAfterStart(Array.Empty<UserTask>(), "", "Solo", "Solo");
        Assert.Equal("Solo", none.FocusTask);
        Assert.Equal("Solo", none.CustomTitle);
    }

    [Fact]
    public void FocusTaskMatcher_StartsOpenTaskAndSkipsDoneOnes()
    {
        Assert.Equal("寫報告", FocusTaskMatcher.StartFromTask(new UserTask { Title = "  寫報告  " }));
        Assert.Equal("", FocusTaskMatcher.StartFromTask(null));
        Assert.Equal("", FocusTaskMatcher.StartFromTask(new UserTask { Title = "   " }));
        Assert.Equal(InputGuard.FocusTaskMax, FocusTaskMatcher.StartFromTask(new UserTask { Title = new string('a', 200) }).Length);

        Assert.True(FocusTaskMatcher.CanStartFocus(new UserTask { Title = "Write", IsCompleted = false }));
        Assert.False(FocusTaskMatcher.CanStartFocus(new UserTask { Title = "Done", IsCompleted = true }));
        Assert.True(FocusTaskMatcher.CanStartFocus(new UserTask { Title = "Daily", IsCompleted = true, IsRoutine = true }));
        Assert.Equal("", FocusTaskMatcher.StartFromTask(new UserTask { Title = "Done", IsCompleted = true }));
        Assert.Equal("Daily", FocusTaskMatcher.StartFromTask(new UserTask { Title = "Daily", IsCompleted = true, IsRoutine = true }));
    }
}
