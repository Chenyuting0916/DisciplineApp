using DisciplineApp.Models;
using Xunit;

namespace DisciplineApp.Tests.Services;

public class GuestFocusTests
{
    [Fact]
    public void Record_ClampsDurationTaskAndCapsHistory()
    {
        var sessions = new List<GuestFocusSession>();
        var now = new DateTime(2026, 9, 17, 8, 0, 0, DateTimeKind.Utc);

        Assert.Null(GuestFocusLogic.Record(sessions, 0.01, "x", true, now));

        var recorded = GuestFocusLogic.Record(sessions, 12.4, "  寫報告  ", true, now);
        Assert.NotNull(recorded);
        Assert.Equal(12.4, recorded!.DurationMinutes);
        Assert.Equal("寫報告", recorded.TaskTag);
        Assert.True(recorded.IsPomodoro);
        Assert.Equal(now.AddMinutes(-12.4), recorded.StartTime);

        var huge = GuestFocusLogic.Record(sessions, 9999, new string('a', 200), false, now);
        Assert.Equal(GuestFocusLogic.MaxRecordMinutes, huge!.DurationMinutes);
        Assert.Equal(InputGuard.FocusTaskMax, huge.TaskTag.Length);

        for (var i = 0; i < InputGuard.MaxExportSessions + 5; i++)
        {
            GuestFocusLogic.Record(sessions, 1, "cap", false, now.AddMinutes(i));
        }

        Assert.Equal(InputGuard.MaxExportSessions, sessions.Count);
        Assert.DoesNotContain(sessions, s => s.TaskTag == "寫報告");
    }

    [Fact]
    public void Goal_And_WeekTotals_UseUtcDayBounds()
    {
        Assert.Equal(10, GuestFocusLogic.ClampGoal(1));
        Assert.Equal(720, GuestFocusLogic.ClampGoal(900));
        Assert.Equal(60, GuestFocusLogic.ClampGoal(0));
        Assert.True(GuestFocusLogic.JustReachedGoal(9, 10, 10));
        Assert.False(GuestFocusLogic.JustReachedGoal(10, 15, 10));
        Assert.False(GuestFocusLogic.JustReachedGoal(0, 5, 10));

        var weekStart = HabitMath.WeekStart(new DateTime(2026, 9, 17, 12, 0, 0, DateTimeKind.Utc));
        var sessions = new List<GuestFocusSession>
        {
            new() { DurationMinutes = 25, EndTime = weekStart.AddDays(1) },
            new() { DurationMinutes = 10, EndTime = weekStart.AddDays(8) },
            new() { DurationMinutes = 5, EndTime = weekStart.AddDays(-1) }
        };

        Assert.Equal(25, GuestFocusLogic.WeekMinutes(sessions, weekStart.AddDays(3)));
        Assert.Equal(5, GuestFocusLogic.WeekMinutes(sessions, weekStart.AddDays(3), 1));
        Assert.Equal(25, GuestFocusLogic.TodayMinutes(sessions, weekStart.AddDays(1)));

        Assert.False(GuestFocusLogic.Delete(sessions, 0));
        Assert.False(GuestFocusLogic.Delete(sessions, 99));
        sessions[0].Id = 7;
        Assert.True(GuestFocusLogic.Delete(sessions, 7));
        Assert.Equal(2, sessions.Count);
    }

    [Fact]
    public void BuildReview_FillsFocusMinutesWithoutLosingVowCount()
    {
        var weekStart = HabitMath.WeekStart(new DateTime(2026, 9, 17));
        var days = new List<GuestDayJournal>
        {
            new() { Date = weekStart.AddDays(1), Vow = "stay" }
        };
        var sessions = new List<GuestFocusSession>
        {
            new() { DurationMinutes = 40, EndTime = weekStart.AddDays(2) },
            new() { DurationMinutes = 15, EndTime = weekStart.AddDays(-2) }
        };

        var review = GuestJournalLogic.BuildReview(
            Array.Empty<Habit>(),
            Array.Empty<UserTask>(),
            days,
            weekStart.AddDays(4),
            sessions);

        Assert.Equal(1, review.SessionCount);
        Assert.Equal(1, review.VowDays);
        Assert.Equal(40, review.FocusMinutes);
        Assert.Equal(weekStart.AddDays(2).Date, review.BestDay!.Value.Date);
        Assert.Equal(40, review.BestDayMinutes);
    }

    [Fact]
    public void BuildHeatmap_CountsSessionsTasksHabitsAndDropsOldDays()
    {
        var start = new DateTime(2026, 1, 1);
        var day = new DateTime(2026, 9, 17);
        var sessions = new List<GuestFocusSession>
        {
            new() { EndTime = new DateTime(2026, 9, 17, 8, 0, 0, DateTimeKind.Utc) }
        };
        var tasks = new List<UserTask>
        {
            new() { IsCompleted = true, CompletedAt = day.AddHours(10) },
            new() { IsCompleted = false, CompletedAt = day.AddHours(11) }
        };
        var habits = new List<Habit>
        {
            new()
            {
                Logs =
                {
                    new HabitLog { Date = day },
                    new HabitLog { Date = new DateTime(2025, 6, 1) }
                }
            }
        };

        var map = GuestFocusLogic.BuildHeatmap(sessions, tasks, habits, start);
        Assert.Equal(3, map[day.Date]);
        Assert.False(map.ContainsKey(new DateTime(2025, 6, 1)));
        Assert.Empty(GuestFocusLogic.BuildHeatmap(null, null, null, start));
        Assert.Equal(1, GuestFocusLogic.ActivityStreak(map.Keys, day.AddHours(12)));
        Assert.Equal(0, GuestFocusLogic.ActivityStreak(Array.Empty<DateTime>(), day));
        Assert.Equal(2, GuestFocusLogic.ActivityStreak(new[] { day, day.AddDays(-1) }, day));

        var today = new DateTime(2026, 9, 17);
        Assert.Equal(StreakStatus.None, HabitMath.ActivityStreakStatus(Array.Empty<DateTime>(), today).Status);
        Assert.Equal(StreakStatus.Active, HabitMath.ActivityStreakStatus(new[] { today, today.AddDays(-1) }, today).Status);
        Assert.Equal(2, HabitMath.ActivityStreakStatus(new[] { today, today.AddDays(-1) }, today).Streak);
        var pending = HabitMath.ActivityStreakStatus(new[] { today.AddDays(-1), today.AddDays(-2) }, today);
        Assert.Equal(StreakStatus.Pending, pending.Status);
        Assert.Equal(2, pending.Streak);
        var atRisk = HabitMath.ActivityStreakStatus(new[] { today.AddDays(-2), today.AddDays(-3) }, today);
        Assert.Equal(StreakStatus.AtRisk, atRisk.Status);
        Assert.Equal(2, atRisk.Streak);
        Assert.Equal(StreakStatus.Broken, HabitMath.ActivityStreakStatus(new[] { today.AddDays(-3) }, today).Status);
        Assert.Equal(0, HabitMath.ActivityStreakStatus(new[] { today.AddDays(-3) }, today).Streak);
    }

    [Fact]
    public void TodayPomodoroCount_IgnoresStopwatchAndOtherDays()
    {
        var day = new DateTime(2026, 9, 17, 8, 0, 0, DateTimeKind.Utc);
        var sessions = new List<GuestFocusSession>
        {
            new() { IsPomodoro = true, DurationMinutes = 25, EndTime = day },
            new() { IsPomodoro = true, DurationMinutes = 25, EndTime = day.AddHours(2) },
            new() { IsPomodoro = false, DurationMinutes = 40, EndTime = day.AddHours(3) },
            new() { IsPomodoro = true, DurationMinutes = 25, EndTime = day.AddDays(-1) },
            new() { IsPomodoro = true, DurationMinutes = 25, EndTime = day.AddDays(1) }
        };

        Assert.Equal(2, GuestFocusLogic.TodayPomodoroCount(sessions, day));
        Assert.Equal(90, GuestFocusLogic.TodayMinutes(sessions, day));
        Assert.Equal(0, GuestFocusLogic.TodayPomodoroCount(Array.Empty<GuestFocusSession>(), day));
        Assert.Equal(100, GuestFocusLogic.PercentTowardGoal(10, 10));
        Assert.Equal(50, GuestFocusLogic.PercentTowardGoal(5, 10));
        Assert.Equal(0, GuestFocusLogic.PercentTowardGoal(0, 60));
        Assert.Equal(100, GuestFocusLogic.PercentTowardGoal(80, 60));
        Assert.True(GuestFocusLogic.ShouldOfferReview(0.1, false));
        Assert.True(GuestFocusLogic.ShouldOfferReview(25, false));
        Assert.False(GuestFocusLogic.ShouldOfferReview(0.01, false));
        Assert.False(GuestFocusLogic.ShouldOfferReview(25, true));
    }

    [Fact]
    public void WeekBreakdown_GroupsTagsAndSkipsOtherWeeks()
    {
        var weekStart = HabitMath.WeekStart(new DateTime(2026, 9, 17, 12, 0, 0, DateTimeKind.Utc));
        var sessions = new List<GuestFocusSession>
        {
            new() { TaskTag = "寫報告", DurationMinutes = 25, EndTime = weekStart.AddDays(1) },
            new() { TaskTag = "  寫報告  ", DurationMinutes = 15, EndTime = weekStart.AddDays(2) },
            new() { TaskTag = "", DurationMinutes = 10, EndTime = weekStart.AddDays(3) },
            new() { TaskTag = "   ", DurationMinutes = 5, EndTime = weekStart.AddDays(4) },
            new() { TaskTag = "寫報告", DurationMinutes = 40, EndTime = weekStart.AddDays(-1) },
            new() { TaskTag = "寫報告", DurationMinutes = 30, EndTime = weekStart.AddDays(7) }
        };

        var breakdown = GuestFocusLogic.WeekBreakdown(sessions, weekStart);
        Assert.Equal(40, breakdown["寫報告"]);
        Assert.Equal(15, breakdown[""]);
        Assert.Equal(2, breakdown.Count);
        Assert.Equal(40, GuestFocusLogic.WeekBreakdown(sessions, weekStart.AddDays(-7))["寫報告"]);
        Assert.Equal(30, GuestFocusLogic.WeekBreakdown(sessions, weekStart.AddDays(7))["寫報告"]);
        Assert.Equal("寫報告", GuestFocusLogic.DisplayTag("  寫報告  "));
        Assert.Equal("", GuestFocusLogic.DisplayTag("custom"));
        Assert.Equal("", GuestFocusLogic.DisplayTag("Uncategorized"));
        Assert.Equal("", GuestFocusLogic.DisplayTag("   "));
        Assert.Equal(4, GuestFocusLogic.WeekSessionCount(sessions, weekStart));
        Assert.Equal("寫報告", GuestFocusLogic.TopTaggedFocus(sessions, weekStart));
        var (bestDay, bestMinutes) = GuestFocusLogic.BestDay(sessions, weekStart);
        Assert.Equal(weekStart.AddDays(1).Date, bestDay!.Value.Date);
        Assert.Equal(25, bestMinutes);
    }
}
