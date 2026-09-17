using DisciplineApp.Data;
using DisciplineApp.Models;
using DisciplineApp.Services;
using DisciplineApp.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace DisciplineApp.Tests.Services;

public class StreakAndHabitTests
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly List<ApplicationUser> _users = new();
    private readonly GamificationService _gamification;
    private readonly HabitService _habits;

    public StreakAndHabitTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        var store = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(store.Object, null, null, null, null, null, null, null, null);
        _mockUserManager.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string id) => _users.FirstOrDefault(u => u.Id == id));
        _mockUserManager.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        _gamification = new GamificationService(_context, _mockUserManager.Object);
        _habits = new HabitService(_context, _gamification);
    }

    [Fact]
    public async Task RecordActivity_StartsStreakAtOne()
    {
        var user = new ApplicationUser { Id = "u1" };
        _users.Add(user);

        var status = await _gamification.RecordActivityAsync("u1");

        Assert.Equal(StreakStatus.Active, status);
        Assert.Equal(1, user.CurrentStreak);
        Assert.Equal(1, user.LongestStreak);
        Assert.Equal(DateTime.UtcNow.Date, user.LastActiveDate?.Date);
    }

    [Fact]
    public async Task RecordActivity_IncrementsWhenYesterday()
    {
        var user = new ApplicationUser
        {
            Id = "u1",
            CurrentStreak = 4,
            LongestStreak = 4,
            LastActiveDate = DateTime.UtcNow.Date.AddDays(-1)
        };
        _users.Add(user);

        await _gamification.RecordActivityAsync("u1");

        Assert.Equal(5, user.CurrentStreak);
        Assert.Equal(5, user.LongestStreak);
    }

    [Fact]
    public async Task RecordActivity_ResetsWhenGap()
    {
        var user = new ApplicationUser
        {
            Id = "u1",
            CurrentStreak = 10,
            LongestStreak = 10,
            LastActiveDate = DateTime.UtcNow.Date.AddDays(-3)
        };
        _users.Add(user);

        await _gamification.RecordActivityAsync("u1");

        Assert.Equal(1, user.CurrentStreak);
        Assert.Equal(10, user.LongestStreak);
    }

    [Fact]
    public async Task FreezeStreak_CostsCoinsAndPreservesPendingDay()
    {
        var user = new ApplicationUser
        {
            Id = "u1",
            CurrentStreak = 7,
            GoldCoins = 40,
            LastActiveDate = DateTime.UtcNow.Date.AddDays(-2)
        };
        _users.Add(user);

        var result = await _gamification.FreezeStreakAsync("u1");

        Assert.True(result.success);
        Assert.Equal(10, result.remainingCoins);
        Assert.Equal(DateTime.UtcNow.Date.AddDays(-1), user.LastActiveDate?.Date);
        Assert.Equal(7, user.CurrentStreak);
    }

    [Fact]
    public async Task ToggleHabit_CreatesLogAndAwardsActivity()
    {
        var user = new ApplicationUser { Id = "u1" };
        _users.Add(user);
        var habit = await _habits.AddHabitAsync("u1", "Read", "📚", "#a78bfa");

        var completed = await _habits.ToggleHabitTodayAsync("u1", habit.Id);

        Assert.True(completed);
        Assert.Equal(1, user.CurrentStreak);
        var logs = await _context.HabitLogs.CountAsync();
        Assert.Equal(1, logs);
    }

    [Fact]
    public async Task UpdateDailyFocusGoal_ClampsValue()
    {
        var user = new ApplicationUser { Id = "u1", DailyFocusGoalMinutes = 60 };
        _users.Add(user);

        await _gamification.UpdateDailyFocusGoalAsync("u1", 5);
        Assert.Equal(10, user.DailyFocusGoalMinutes);

        await _gamification.UpdateDailyFocusGoalAsync("u1", 900);
        Assert.Equal(720, user.DailyFocusGoalMinutes);
    }
}
