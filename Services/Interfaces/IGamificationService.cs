using DisciplineApp.Models;

namespace DisciplineApp.Services.Interfaces;

public interface IGamificationService
{
    Task<(bool success, int xpAwarded, int remainingDaily, bool levelUp)> AddXpAsync(string userId, int xpAmount);
    Task AwardCoinsAsync(string userId, int min, int max);
    Task<(bool success, int xpAwarded, int coinsAwarded, bool levelUp)> RecordFocusSessionAsync(string userId, double minutes, string taskTag, bool isPomodoro, DateTime? endTime = null);
    Task<List<ApplicationUser>> GetLeaderboardAsync(int count = 10, string sortBy = "xp");
    Task<double> GetWeeklyFocusTimeAsync(string userId, DateTime? referenceDate = null);
    Task<Dictionary<string, double>> GetWeeklyFocusBreakdownAsync(string userId, DateTime? referenceDate = null);
    Task<List<FocusSession>> GetFocusSessionsAsync(string userId, int limit);
    Task<Dictionary<string, double>> GetDailyFocusActivityAsync(string userId, int days = 7);
    Task<ApplicationUser?> GetUserStatsAsync(string userId);
    Task<Dictionary<DateTime, int>> GetActivityDataAsync(string userId, DateTime startDate);
    Task<bool> DeleteFocusSessionAsync(int sessionId, string userId);
    Task<StreakStatus> RecordActivityAsync(string userId);
    Task<StreakStatus> GetStreakStatusAsync(string userId);
    Task<(bool success, int remainingCoins)> FreezeStreakAsync(string userId, int cost = 30);
    Task<double> GetTodayFocusMinutesAsync(string userId);
    Task<int> GetTodayPomodoroCountAsync(string userId);
    Task<bool> UpdateDailyFocusGoalAsync(string userId, int minutes);
    Task<WeeklyReview> GetWeeklyReviewAsync(string userId);
}
