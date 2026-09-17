using DisciplineApp.Models;

namespace DisciplineApp.Services.Interfaces;

public interface IHabitService
{
    Task<List<Habit>> GetHabitsAsync(string userId, bool includeArchived = false);
    Task<Habit?> AddHabitAsync(string userId, string title, string icon, string color, int targetDaysPerWeek = 7);
    Task<bool> ToggleHabitTodayAsync(string userId, int habitId);
    Task<bool> ArchiveHabitAsync(string userId, int habitId);
    Task<Dictionary<int, HashSet<DateTime>>> GetLogsForWeekAsync(string userId, DateTime weekStart);
    Task<int> GetHabitStreakAsync(Habit habit);
    Task<(int completed, int total)> GetTodayProgressAsync(string userId);
}
