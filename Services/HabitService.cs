using DisciplineApp.Data;
using DisciplineApp.Models;
using DisciplineApp.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DisciplineApp.Services;

public class HabitService : IHabitService
{
    private readonly ApplicationDbContext _context;
    private readonly IGamificationService _gamificationService;

    public HabitService(ApplicationDbContext context, IGamificationService gamificationService)
    {
        _context = context;
        _gamificationService = gamificationService;
    }

    public async Task<List<Habit>> GetHabitsAsync(string userId, bool includeArchived = false)
    {
        var query = _context.Habits
            .Include(h => h.Logs)
            .Where(h => h.UserId == userId);

        if (!includeArchived)
        {
            query = query.Where(h => !h.IsArchived);
        }

        return await query.OrderBy(h => h.CreatedAt).ToListAsync();
    }

    public async Task<Habit?> AddHabitAsync(string userId, string title, string icon, string color, int targetDaysPerWeek = 7)
    {
        var count = await _context.Habits.CountAsync(h => h.UserId == userId && !h.IsArchived);
        if (count >= InputGuard.MaxHabits)
        {
            return null;
        }

        var habit = new Habit
        {
            UserId = userId,
            Title = InputGuard.Clamp(title, InputGuard.TitleMax),
            Icon = string.IsNullOrWhiteSpace(icon) ? "✅" : InputGuard.Clamp(icon, 8),
            Color = InputGuard.IsSafeHexColor(color) ? color : "#F5A524",
            TargetDaysPerWeek = Math.Clamp(targetDaysPerWeek, 1, 7),
            CreatedAt = DateTime.UtcNow
        };

        _context.Habits.Add(habit);
        await _context.SaveChangesAsync();
        return habit;
    }

    public async Task<bool> ToggleHabitTodayAsync(string userId, int habitId)
    {
        var habit = await _context.Habits
            .Include(h => h.Logs)
            .FirstOrDefaultAsync(h => h.Id == habitId && h.UserId == userId && !h.IsArchived);

        if (habit == null) return false;

        var today = DateTime.UtcNow.Date;
        var existing = habit.Logs.FirstOrDefault(l => l.Date.Date == today);

        if (existing != null)
        {
            _context.HabitLogs.Remove(existing);
            await _context.SaveChangesAsync();
            return false;
        }

        _context.HabitLogs.Add(new HabitLog
        {
            HabitId = habit.Id,
            Date = today
        });
        await _context.SaveChangesAsync();
        await _gamificationService.RecordActivityAsync(userId);
        await _gamificationService.AddXpAsync(userId, 20);
        return true;
    }

    public async Task<bool> ArchiveHabitAsync(string userId, int habitId)
    {
        var habit = await _context.Habits.FirstOrDefaultAsync(h => h.Id == habitId && h.UserId == userId);
        if (habit == null) return false;

        habit.IsArchived = true;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<Dictionary<int, HashSet<DateTime>>> GetLogsForWeekAsync(string userId, DateTime weekStart)
    {
        var start = weekStart.Date;
        var end = start.AddDays(7);

        var logs = await _context.HabitLogs
            .Where(l => l.Habit != null && l.Habit.UserId == userId && l.Date >= start && l.Date < end)
            .ToListAsync();

        return logs
            .GroupBy(l => l.HabitId)
            .ToDictionary(g => g.Key, g => g.Select(l => l.Date.Date).ToHashSet());
    }

    public Task<int> GetHabitStreakAsync(Habit habit)
    {
        var dates = habit.Logs
            .Select(l => l.Date.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .ToList();

        if (dates.Count == 0) return Task.FromResult(0);

        var cursor = DateTime.UtcNow.Date;
        if (dates[0] < cursor.AddDays(-1)) return Task.FromResult(0);
        if (dates[0] == cursor.AddDays(-1)) cursor = dates[0];

        int streak = 0;
        foreach (var date in dates)
        {
            if (date == cursor)
            {
                streak++;
                cursor = cursor.AddDays(-1);
            }
            else if (date < cursor)
            {
                break;
            }
        }

        return Task.FromResult(streak);
    }

    public async Task<(int completed, int total)> GetTodayProgressAsync(string userId)
    {
        var habits = await GetHabitsAsync(userId);
        var today = DateTime.UtcNow.Date;
        var completed = habits.Count(h => h.Logs.Any(l => l.Date.Date == today));
        return (completed, habits.Count);
    }
}
