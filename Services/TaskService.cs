using DisciplineApp.Data;
using DisciplineApp.Models;
using Microsoft.EntityFrameworkCore;

using DisciplineApp.Services.Interfaces;

namespace DisciplineApp.Services;

public class TaskService : ITaskService
{
    private readonly ApplicationDbContext _context;
    private readonly IGamificationService _gamificationService;

    public TaskService(ApplicationDbContext context, IGamificationService gamificationService)
    {
        _context = context;
        _gamificationService = gamificationService;
    }

    public async Task<List<UserTask>> GetTasksForDateAsync(string userId, DateTime date)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        var tasks = await _context.UserTasks
            .Include(t => t.Category)
            .Where(t => t.UserId == userId && 
                        ((t.Date >= startOfDay && t.Date < endOfDay) || 
                         (t.IsRoutine && t.Date < endOfDay)))
            .ToListAsync();

        // Reset routine tasks if they were completed before today
        foreach (var task in tasks.Where(t => t.IsRoutine && t.IsCompleted))
        {
            if (task.LastCompletedDate.HasValue && task.LastCompletedDate.Value.Date < DateTime.Today)
            {
                task.IsCompleted = false;
                task.CompletedAt = null;
                task.XpAwarded = false;
            }
        }

        await _context.SaveChangesAsync();
        return tasks;
    }



    public async Task<List<Category>> GetCategoriesAsync(string userId)
    {
        return await _context.Categories
            .Where(c => c.UserId == null || c.UserId == userId)
            .ToListAsync();
    }

    public async Task<UserTask> AddTaskAsync(string userId, string title, DateTime date, bool isRoutine = false, int? categoryId = null)
    {
        var task = new UserTask
        {
            UserId = userId,
            Title = title,
            Date = date,
            IsRoutine = isRoutine,
            CategoryId = categoryId,
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
            // Check if XP should be awarded
            bool shouldAwardXp = false;

            if (task.IsRoutine)
            {
                // For routines, check if already completed today
                if (task.LastCompletedDate == null || task.LastCompletedDate.Value.Date < DateTime.Today)
                {
                    shouldAwardXp = true;
                    task.LastCompletedDate = DateTime.Today;
                }
            }
            else
            {
                // For one-off tasks, check if already awarded
                if (!task.XpAwarded)
                {
                    shouldAwardXp = true;
                }
            }
            
            if (shouldAwardXp)
            {
                var result = await _gamificationService.AddXpAsync(userId, 50);
                
                // Only mark as awarded if XP was actually added (or cap reached, but we still consider it "done" for the task)
                // Actually, if cap reached, we don't want to award again today? 
                // No, if cap reached, they shouldn't get XP, but the task is still "completed" and shouldn't give XP again if toggled.
                
                task.XpAwarded = true; 
                await _context.SaveChangesAsync();
                return (result.success, result.xpAwarded, result.remainingDaily, !result.success && result.remainingDaily <= 0);
            }
            else
            {
                await _context.SaveChangesAsync();
                return (false, 0, 0, false); // Already awarded
            }
        }
        else
        {
            // If uncompleting, we generally don't take away XP to avoid negative feelings, 
            // but we should reset XpAwarded if it was a mistake?
            // If we reset XpAwarded, they can toggle again to farm XP. 
            // So we should NOT reset XpAwarded for one-off tasks if they uncomplete.
            // But for routines, it resets daily anyway.
            
            // However, if they uncomplete a routine today, they can complete it again.
            // If we don't reset LastCompletedDate, they won't get XP again (good).
            // If we DO reset LastCompletedDate, they can farm XP.
            
            // So: Do NOT reset XpAwarded or LastCompletedDate when uncompleting.
            // This prevents farming.
            
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
