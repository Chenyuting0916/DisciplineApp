using DisciplineApp.Models;

namespace DisciplineApp.Services.Interfaces;

public interface IIfThenService
{
    Task<List<IfThenPlan>> GetActiveAsync(string userId);
    Task<(bool ok, string reason)> AddAsync(string userId, string ifCue, string thenAction);
    Task<bool> DeleteAsync(string userId, int id);
}
