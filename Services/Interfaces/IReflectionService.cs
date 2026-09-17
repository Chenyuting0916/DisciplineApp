using DisciplineApp.Models;

namespace DisciplineApp.Services.Interfaces;

public interface IReflectionService
{
    Task<DailyReflection?> GetTodayAsync(string userId);
    Task<DailyReflection> SaveTodayAsync(string userId, int mood, string? note);
}
