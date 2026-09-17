using DisciplineApp.Models;

namespace DisciplineApp.Services.Interfaces;

public interface IIntentionService
{
    Task<DailyIntention?> GetTodayAsync(string userId);
    Task<DailyIntention> SaveTodayAsync(string userId, string vow, string oneThing);
}
