using DisciplineApp.Data;
using DisciplineApp.Models;
using Microsoft.EntityFrameworkCore;

namespace DisciplineApp.Services;

public class GamificationService
{
    private readonly ApplicationDbContext _context;
    private readonly Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> _userManager;

    public GamificationService(ApplicationDbContext context, Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<(bool success, int xpAwarded, int remainingDaily)> AddXpAsync(string userId, int xpAmount)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return (false, 0, 0);

        // Reset daily XP if it's a new day
        if (user.LastXpResetDate == null || user.LastXpResetDate.Value.Date < DateTime.UtcNow.Date)
        {
            user.DailyXpEarned = 0;
            user.LastXpResetDate = DateTime.UtcNow;
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
            // Bonus coins for leveling up
            user.GoldCoins += 50;
        }

        await _userManager.UpdateAsync(user);
        
        remainingDaily = DAILY_XP_CAP - user.DailyXpEarned;
        return (true, actualXpAwarded, remainingDaily);
    }

    public async Task AwardCoinsAsync(string userId, int min, int max)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user != null)
        {
            var coins = new Random().Next(min, max + 1);
            user.GoldCoins += coins;
            await _userManager.UpdateAsync(user);
        }
    }

    public async Task<(bool success, int xpAwarded, int coinsAwarded)> RecordFocusSessionAsync(string userId, double minutes, string taskTag, bool isPomodoro)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return (false, 0, 0);

        // Save Session
        var session = new FocusSession
        {
            UserId = userId,
            StartTime = DateTime.UtcNow.AddMinutes(-minutes),
            EndTime = DateTime.UtcNow,
            DurationMinutes = minutes,
            TaskTag = taskTag,
            IsPomodoro = isPomodoro
        };
        _context.FocusSessions.Add(session);

        // Update Total Focus Time
        user.TotalFocusMinutes += minutes;

        // Calculate XP (e.g., 1 XP per minute)
        int xpToAward = (int)minutes;
        
        // Calculate Coins (e.g., random 1-5 coins for every 20 mins)
        int coinsToAward = 0;
        if (minutes >= 20)
        {
            // 1-10 coins for a good session
            coinsToAward = new Random().Next(1, 11);
            // Bonus for longer sessions
            if (minutes >= 50) coinsToAward += new Random().Next(5, 15);
            
            user.GoldCoins += coinsToAward;
        }

        // Apply XP with cap logic (reusing logic or calling AddXpAsync logic internally, but we need to return coins too)
        // Let's just duplicate the logic slightly or refactor. 
        // For simplicity, I'll inline the XP logic here to ensure we update the user object once.
        
        if (user.LastXpResetDate == null || user.LastXpResetDate.Value.Date < DateTime.UtcNow.Date)
        {
            user.DailyXpEarned = 0;
            user.LastXpResetDate = DateTime.UtcNow;
        }

        const int DAILY_XP_CAP = 500;
        int actualXpAwarded = 0;
        if (user.DailyXpEarned < DAILY_XP_CAP)
        {
            int remainingCap = DAILY_XP_CAP - user.DailyXpEarned;
            actualXpAwarded = Math.Min(xpToAward, remainingCap);
            
            user.CurrentXP += actualXpAwarded;
            user.TotalXP += actualXpAwarded;
            user.DailyXpEarned += actualXpAwarded;

            // Level Up Check
            int xpForNextLevel = user.Level * 100;
            while (user.CurrentXP >= xpForNextLevel)
            {
                user.CurrentXP -= xpForNextLevel;
                user.Level++;
                xpForNextLevel = user.Level * 100;
                user.GoldCoins += 50; 
            }
        }

        await _userManager.UpdateAsync(user);
        await _context.SaveChangesAsync();

        return (true, actualXpAwarded, coinsToAward);
    }

    public async Task<List<ApplicationUser>> GetLeaderboardAsync(int count = 10, string sortBy = "xp")
    {
        var query = _userManager.Users.AsQueryable();

        switch (sortBy)
        {
            case "coins":
                query = query.OrderByDescending(u => u.GoldCoins);
                break;
            case "focus":
                query = query.OrderByDescending(u => u.TotalFocusMinutes);
                break;
            case "xp":
            default:
                query = query.OrderByDescending(u => u.TotalXP);
                break;
        }

        return await query.Take(count).ToListAsync();
    }

    public async Task<double> GetWeeklyFocusTimeAsync(string userId, DateTime? referenceDate = null)
    {
        // Calculate start of week (Sunday)
        var today = referenceDate?.Date ?? DateTime.UtcNow.Date;
        var diff = (7 + (today.DayOfWeek - DayOfWeek.Sunday)) % 7;
        var startOfWeek = today.AddDays(-1 * diff);
        var endOfWeek = startOfWeek.AddDays(7);

        return await _context.FocusSessions
            .Where(s => s.UserId == userId && s.EndTime >= startOfWeek && s.EndTime < endOfWeek)
            .SumAsync(s => s.DurationMinutes);
    }

    public async Task<Dictionary<string, double>> GetWeeklyFocusBreakdownAsync(string userId, DateTime? referenceDate = null)
    {
        // Calculate start of week (Sunday)
        var today = referenceDate?.Date ?? DateTime.UtcNow.Date;
        var diff = (7 + (today.DayOfWeek - DayOfWeek.Sunday)) % 7;
        var startOfWeek = today.AddDays(-1 * diff);
        var endOfWeek = startOfWeek.AddDays(7);

        var sessions = await _context.FocusSessions
            .Where(s => s.UserId == userId && s.EndTime >= startOfWeek && s.EndTime < endOfWeek)
            .ToListAsync();

        var breakdown = sessions
            .GroupBy(s => string.IsNullOrEmpty(s.TaskTag) ? "Uncategorized" : s.TaskTag)
            .ToDictionary(g => g.Key, g => g.Sum(s => s.DurationMinutes));

        return breakdown;
    }
    
    public async Task<List<FocusSession>> GetFocusSessionsAsync(string userId, int limit)
    {
        return await _context.FocusSessions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.EndTime)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<Dictionary<string, double>> GetDailyFocusActivityAsync(string userId, int days = 7)
    {
        var startDate = DateTime.UtcNow.Date.AddDays(-(days - 1));
        
        var sessions = await _context.FocusSessions
            .Where(s => s.UserId == userId && s.EndTime >= startDate)
            .ToListAsync();

        var activity = sessions
            .GroupBy(s => s.EndTime.ToLocalTime().Date)
            .ToDictionary(g => g.Key.ToString("MM/dd"), g => g.Sum(s => s.DurationMinutes));
            
        return activity;
    }

    public async Task<ApplicationUser?> GetUserStatsAsync(string userId)
    {
        return await _userManager.FindByIdAsync(userId);
    }
    public async Task<Dictionary<DateTime, int>> GetActivityDataAsync(string userId, DateTime startDate)
    {
        // Fetch data first
        var tasks = await _context.UserTasks
            .Where(t => t.UserId == userId && t.CompletedAt >= startDate && t.IsCompleted)
            .ToListAsync();

        // Convert to Local Time for grouping (assuming server local time matches user for now)
        var tasksGrouped = tasks
            .GroupBy(t => t.CompletedAt!.Value.ToLocalTime().Date)
            .Select(g => new { Date = g.Key, Count = g.Count() });

        var sessions = await _context.FocusSessions
            .Where(s => s.UserId == userId && s.EndTime >= startDate)
            .ToListAsync();

        var sessionsGrouped = sessions
            .GroupBy(s => s.EndTime.ToLocalTime().Date)
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
}
