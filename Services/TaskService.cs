using DisciplineApp.Data;
using DisciplineApp.Models;
using Microsoft.EntityFrameworkCore;

namespace DisciplineApp.Services;

public class TaskService
{
    private readonly ApplicationDbContext _context;
    private readonly GamificationService _gamificationService;

    public TaskService(ApplicationDbContext context, GamificationService gamificationService)
    {
        _context = context;
        _gamificationService = gamificationService;
    }

    public async Task<List<UserTask>> GetTasksForDateAsync(string userId, DateTime date)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        var tasks = await _context.UserTasks
            .Where(t => t.UserId == userId && t.Date >= startOfDay && t.Date < endOfDay)
            .ToListAsync();

        // Reset routine tasks if they were completed before today
        foreach (var task in tasks.Where(t => t.IsRoutine && t.IsCompleted))
        {
            if (task.LastCompletedDate.HasValue && task.LastCompletedDate.Value.Date < DateTime.Today)
            {
                task.IsCompleted = false;
                task.CompletedAt = null;
            }
        }

        await _context.SaveChangesAsync();
        return tasks;
    }

    public async Task<UserTask> AddTaskAsync(string userId, string title, DateTime date, bool isRoutine = false)
    {
        var task = new UserTask
        {
            UserId = userId,
            Title = title,
            Date = date,
            IsRoutine = isRoutine,
            IsCompleted = false
        };

        _context.UserTasks.Add(task);
        await _context.SaveChangesAsync();
        return task;
    }

    public async Task<(bool success, int xpAwarded, int remainingDaily, bool capReached)> ToggleTaskCompletionAsync(int taskId, string userId)
    {
        var task = await _context.UserTasks.FindAsync(taskId);
        if (task == null || task.UserId != userId) return (false, 0, 0, false);

        task.IsCompleted = !task.IsCompleted;
        task.CompletedAt = task.IsCompleted ? DateTime.UtcNow : null;

        if (task.IsCompleted)
        {
            if (task.IsRoutine)
            {
                task.LastCompletedDate = DateTime.Today;
            }
            
            // Only award XP if not already awarded
            if (!task.XpAwarded)
            {
                var result = await _gamificationService.AddXpAsync(userId, 50);
                task.XpAwarded = result.success;
                await _context.SaveChangesAsync();
                return (result.success, result.xpAwarded, result.remainingDaily, !result.success);
            }
            else
            {
                await _context.SaveChangesAsync();
                return (false, 0, 0, false); // Already awarded
            }
        }
        else
        {
            // Reset XpAwarded when uncompleting
            task.XpAwarded = false;
            await _context.SaveChangesAsync();
            return (false, 0, 0, false);
        }
    }

    public async Task DeleteTaskAsync(int taskId, string userId)
    {
        var task = await _context.UserTasks.FindAsync(taskId);
        if (task == null || task.UserId != userId) return;

        _context.UserTasks.Remove(task);
        await _context.SaveChangesAsync();
    }
}
