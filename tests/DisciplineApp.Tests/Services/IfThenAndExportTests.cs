using DisciplineApp.Data;
using DisciplineApp.Models;
using DisciplineApp.Services;
using DisciplineApp.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Globalization;
using Xunit;

namespace DisciplineApp.Tests.Services;

public class IfThenAndExportTests
{
    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task IfThen_ClampsAndEnforcesLimit()
    {
        await using var db = CreateDb();
        var service = new IfThenService(db);

        var empty = await service.AddAsync("u1", "   ", "do it");
        Assert.False(empty.ok);
        Assert.Equal("empty", empty.reason);

        var longCue = new string('a', 200);
        var added = await service.AddAsync("u1", longCue, "open timer");
        Assert.True(added.ok);

        var plans = await service.GetActiveAsync("u1");
        Assert.Single(plans);
        Assert.Equal(InputGuard.TitleMax, plans[0].IfCue.Length);

        for (int i = 0; i < InputGuard.MaxIfThenPlans - 1; i++)
        {
            var result = await service.AddAsync("u1", $"cue {i}", $"act {i}");
            Assert.True(result.ok);
        }

        var over = await service.AddAsync("u1", "overflow", "nope");
        Assert.False(over.ok);
        Assert.Equal("limit", over.reason);
        Assert.Equal(InputGuard.MaxIfThenPlans, await db.IfThenPlans.CountAsync(p => p.UserId == "u1"));
    }

    [Fact]
    public void IfThenStart_ClampsAndMapsToTimer()
    {
        Assert.False(IfThenStart.CanStart("   "));
        Assert.Equal("", IfThenStart.FocusTitle("   "));
        Assert.Equal(InputGuard.FocusTaskMax, IfThenStart.FocusTitle(new string('a', 200)).Length);

        var empty = IfThenStart.ForTimer(null, "  open notes  ");
        Assert.Equal("open notes", empty.FocusTask);
        Assert.Equal("open notes", empty.CustomTitle);

        var tasks = new List<UserTask>
        {
            new() { Title = "Inbox", IsCompleted = false },
            new() { Title = "Done", IsCompleted = true }
        };

        var listed = IfThenStart.ForTimer(tasks, "Inbox");
        Assert.Equal("Inbox", listed.FocusTask);
        Assert.Equal("", listed.CustomTitle);

        var completed = IfThenStart.ForTimer(tasks, "Done");
        Assert.Equal("custom", completed.FocusTask);
        Assert.Equal("Done", completed.CustomTitle);

        var custom = IfThenStart.ForTimer(tasks, "write the essay");
        Assert.Equal("custom", custom.FocusTask);
        Assert.Equal("write the essay", custom.CustomTitle);
    }

    [Fact]
    public async Task IfThen_DeleteOnlyOwnPlan()
    {
        await using var db = CreateDb();
        var service = new IfThenService(db);
        await service.AddAsync("u1", "phone", "timer");
        await service.AddAsync("u2", "night", "sleep");
        var other = await db.IfThenPlans.SingleAsync(p => p.UserId == "u2");

        var deleted = await service.DeleteAsync("u1", other.Id);
        Assert.False(deleted);
        Assert.Equal(1, await db.IfThenPlans.CountAsync(p => p.UserId == "u2"));
    }

    [Fact]
    public async Task Export_OnlyIncludesThatUser()
    {
        await using var db = CreateDb();
        db.UserTasks.Add(new UserTask { UserId = "u1", Title = "mine", Date = DateTime.Today });
        db.UserTasks.Add(new UserTask { UserId = "u2", Title = "secret-other-user", Date = DateTime.Today });
        db.FocusSessions.Add(new FocusSession
        {
            UserId = "u1",
            TaskTag = "essay",
            DurationMinutes = 25,
            StartTime = DateTime.UtcNow.AddMinutes(-25),
            EndTime = DateTime.UtcNow,
            IsPomodoro = true
        });
        db.FocusSessions.Add(new FocusSession
        {
            UserId = "u2",
            TaskTag = "private",
            DurationMinutes = 50,
            StartTime = DateTime.UtcNow.AddMinutes(-50),
            EndTime = DateTime.UtcNow
        });
        db.IfThenPlans.Add(new IfThenPlan { UserId = "u1", IfCue = "desk", ThenAction = "sit", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var json = await new DataExportService(db).ExportJsonAsync("u1");

        Assert.Contains("mine", json);
        Assert.Contains("essay", json);
        Assert.Contains("desk", json);
        Assert.DoesNotContain("secret-other-user", json);
        Assert.DoesNotContain("private", json);
        Assert.DoesNotContain("u2", json);
    }

    [Fact]
    public void InputGuard_HexColorAndExportName()
    {
        Assert.True(InputGuard.IsSafeHexColor("#F5A524"));
        Assert.False(InputGuard.IsSafeHexColor("red"));
        Assert.False(InputGuard.IsSafeHexColor("#fff"));
        Assert.False(InputGuard.IsSafeHexColor("#GG0000"));
        Assert.False(InputGuard.IsSafeHexColor("javascript:alert(1)"));
        Assert.Matches("^discipline-export-\\d{8}\\.json$", InputGuard.ExportFileName());
        Assert.Equal(5, InputGuard.ClampPomodoroMinutes(1));
        Assert.Equal(90, InputGuard.ClampPomodoroMinutes(400));
        Assert.Equal(40, InputGuard.ClampPomodoroMinutes(40));
    }

    [Fact]
    public async Task HabitService_RejectsBeyondCapAndUnsafeColor()
    {
        await using var db = CreateDb();
        var habits = new HabitService(db, new Mock<IGamificationService>().Object);

        var first = await habits.AddHabitAsync("u1", "Read", "📚", "not-a-color");
        Assert.NotNull(first);
        Assert.Equal("#F5A524", first!.Color);

        for (int i = 0; i < InputGuard.MaxHabits - 1; i++)
        {
            var added = await habits.AddHabitAsync("u1", $"h{i}", "✅", "#38bdf8");
            Assert.NotNull(added);
        }

        var overflow = await habits.AddHabitAsync("u1", "too many", "✅", "#38bdf8");
        Assert.Null(overflow);
    }

    [Fact]
    public void CalendarCopy_FormatsAndRejectsBadDue()
    {
        var en = CultureInfo.GetCultureInfo("en-US");
        var timed = CalendarCopy.EventWhen(new DateTimeOffset(2026, 9, 17, 15, 30, 0, TimeSpan.Zero), null, en, "All day");
        Assert.Contains("9/17/2026", timed);

        var allDay = CalendarCopy.EventWhen(null, "2026-09-17", en, "All day");
        Assert.Contains("9/17/2026", allDay);
        Assert.Contains("All day", allDay);

        Assert.Equal("", CalendarCopy.EventWhen(null, "not-a-date", en, "All day"));
        Assert.Equal("", CalendarCopy.TaskDue("javascript:alert(1)", en, "Due:"));
        Assert.Equal("", CalendarCopy.TaskDue("   ", en, "Due:"));

        var due = CalendarCopy.TaskDue("2026-09-18", en, "Due:");
        Assert.Contains("Due:", due);
        Assert.Contains("9/18/2026", due);
    }
}
