using DisciplineApp.Models;

namespace DisciplineApp.Services.Interfaces;

public interface ITaskService
{
    Task<List<UserTask>> GetTasksForDateAsync(string userId, DateTime date);
    Task<UserTask> AddTaskAsync(string userId, string title, DateTime date, bool isRoutine = false, int? categoryId = null);
    Task<List<Category>> GetCategoriesAsync(string userId);
    Task<(bool success, int xpAwarded, int remainingDaily, bool capReached)> ToggleTaskCompletionAsync(int taskId, string userId);
    Task DeleteTaskAsync(int taskId, string userId);
}
