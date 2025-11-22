using DisciplineApp.Data;
using DisciplineApp.Models;
using Microsoft.EntityFrameworkCore;

namespace DisciplineApp.Services;

public class GamificationService
{
    private readonly ApplicationDbContext _context;

    public GamificationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(bool success, int xpAwarded, int remainingDaily)> AddXpAsync(string userId, int xpAmount)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return (false, 0, 0);

        // Reset daily XP if it's a new day
        if (user.LastXpResetDate == null || user.LastXpResetDate.Value.Date < DateTime.Today)
        {
            user.DailyXpEarned = 0;
            user.LastXpResetDate = DateTime.Today;
        }

        // Check daily cap (500 XP)
        const int DAILY_XP_CAP = 500;
        int remainingDaily = DAILY_XP_CAP - user.DailyXpEarned;
        
        if (remainingDaily <= 0)
        {
            return (false, 0, 0); // Daily cap reached
        }

        // Award XP up to the daily cap
        int actualXpAwarded = Math.Min(xpAmount, remainingDaily);
        
        user.CurrentXP += actualXpAwarded;
        user.TotalXP += actualXpAwarded;
        user.DailyXpEarned += actualXpAwarded;

        // Simple leveling logic: Level * 100 XP required for next level
        int xpRequired = user.Level * 100;
        while (user.CurrentXP >= xpRequired)
        {
            user.CurrentXP -= xpRequired;
            user.Level++;
            xpRequired = user.Level * 100;
        }

        await _context.SaveChangesAsync();
        
        remainingDaily = DAILY_XP_CAP - user.DailyXpEarned;
        return (true, actualXpAwarded, remainingDaily);
    }

    public async Task RecordPomodoroSessionAsync(string userId, int durationMinutes)
    {
        var session = new PomodoroSession
        {
            UserId = userId,
            DurationMinutes = durationMinutes,
            CompletedAt = DateTime.UtcNow
        };

        _context.PomodoroSessions.Add(session);
        
        // Award 10 XP per 25 minutes (approx)
        int xpToAward = (durationMinutes / 5) * 2; 
        if (xpToAward < 1) xpToAward = 1;

        await AddXpAsync(userId, xpToAward);
    }

    public async Task<Dictionary<DateTime, int>> GetActivityDataAsync(string userId, DateTime startDate)
    {
        var tasks = await _context.UserTasks
            .Where(t => t.UserId == userId && t.CompletedAt >= startDate && t.IsCompleted)
            .ToListAsync();

        var tasksGrouped = tasks
            .GroupBy(t => t.CompletedAt!.Value.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() });

        var sessions = await _context.PomodoroSessions
            .Where(s => s.UserId == userId && s.CompletedAt >= startDate)
            .ToListAsync();

        var sessionsGrouped = sessions
            .GroupBy(s => s.CompletedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() });

        var activity = new Dictionary<DateTime, int>();

        foreach (var t in tasksGrouped)
        {
            if (!activity.ContainsKey(t.Date)) activity[t.Date] = 0;
            activity[t.Date] += t.Count;
        }

        foreach (var s in sessionsGrouped)
        {
            if (!activity.ContainsKey(s.Date)) activity[s.Date] = 0;
            activity[s.Date] += s.Count;
        }

        return activity;
    }

    public async Task<List<ApplicationUser>> GetLeaderboardAsync(int count = 10)
    {
        return await _context.Users
            .OrderByDescending(u => u.TotalXP)
            .Take(count)
            .ToListAsync();
    }

    public async Task<ApplicationUser?> GetUserStatsAsync(string userId)
    {
        return await _context.Users.FindAsync(userId);
    }
}
