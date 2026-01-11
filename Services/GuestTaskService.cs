using DisciplineApp.Models;
using Microsoft.Extensions.Localization;

namespace DisciplineApp.Services;

public class GuestTaskService
{
    private readonly LocalStorageService _localStorage;
    private readonly IStringLocalizer<App> _localizer;
    private const string STORAGE_KEY = "guest_tasks";

    public GuestTaskService(LocalStorageService localStorage, IStringLocalizer<App> localizer)
    {
        _localStorage = localStorage;
        _localizer = localizer;
    }

    public async Task<List<UserTask>> GetTasksForTodayAsync(List<Category>? categories = null)
    {
        var allTasks = await _localStorage.GetItemAsync<List<UserTask>>(STORAGE_KEY);

        if (allTasks == null || !allTasks.Any())
        {
            // Initialize with default onboarding tasks
            allTasks = CreateDefaultTasks(categories);
            await _localStorage.SetItemAsync(STORAGE_KEY, allTasks);
        }

        // Filter for today's tasks OR routine tasks
        var todayTasks = allTasks
            .Where(t => t.Date.Date == DateTime.Today || (t.IsRoutine && t.Date.Date <= DateTime.Today))
            .ToList();

        // Reset routine tasks if they were completed before today
        bool needsSave = false;
        foreach (var task in todayTasks.Where(t => t.IsRoutine && t.IsCompleted))
        {
            if (task.CompletedAt.HasValue && task.CompletedAt.Value.Date < DateTime.Today)
            {
                task.IsCompleted = false;
                task.CompletedAt = null;
                task.XpAwarded = false;
                needsSave = true;
            }
        }

        if (needsSave)
        {
            await _localStorage.SetItemAsync(STORAGE_KEY, allTasks);
        }

        return todayTasks;
    }

    public async Task<UserTask> AddTaskAsync(string title, bool isRoutine, int? categoryId, List<Category>? categories)
    {
        var allTasks = await _localStorage.GetItemAsync<List<UserTask>>(STORAGE_KEY) ?? new List<UserTask>();
        
        var newTask = new UserTask
        {
            Id = allTasks.Any() ? allTasks.Max(t => t.Id) + 1 : 1,
            UserId = "guest",
            Title = title,
            Date = DateTime.Today,
            IsRoutine = isRoutine,
            CategoryId = categoryId,
            Category = categories?.FirstOrDefault(c => c.Id == categoryId)
        };
        
        allTasks.Add(newTask);
        await _localStorage.SetItemAsync(STORAGE_KEY, allTasks);
        
        return newTask;
    }

    public async Task<bool> ToggleTaskAsync(int taskId)
    {
        var allTasks = await _localStorage.GetItemAsync<List<UserTask>>(STORAGE_KEY) ?? new List<UserTask>();
        var task = allTasks.FirstOrDefault(t => t.Id == taskId);
        
        if (task == null) return false;

        task.IsCompleted = !task.IsCompleted;
        task.CompletedAt = task.IsCompleted ? DateTime.UtcNow : null;
        await _localStorage.SetItemAsync(STORAGE_KEY, allTasks);
        
        return task.IsCompleted;
    }

    public async Task DeleteTaskAsync(int taskId)
    {
        var allTasks = await _localStorage.GetItemAsync<List<UserTask>>(STORAGE_KEY) ?? new List<UserTask>();
        var task = allTasks.FirstOrDefault(t => t.Id == taskId);
        
        if (task != null)
        {
            allTasks.Remove(task);
            await _localStorage.SetItemAsync(STORAGE_KEY, allTasks);
        }
    }

    private List<UserTask> CreateDefaultTasks(List<Category>? categories)
    {
        var workCat = categories?.FirstOrDefault(c => c.Id == 1) ?? new Category { Id = 1, Name = "Work", ColorCode = "#3B82F6" };
        var personalCat = categories?.FirstOrDefault(c => c.Id == 2) ?? new Category { Id = 2, Name = "Personal", ColorCode = "#10B981" };
        var learningCat = categories?.FirstOrDefault(c => c.Id == 4) ?? new Category { Id = 4, Name = "Learning", ColorCode = "#F59E0B" };

        return new List<UserTask>
        {
            new UserTask
            {
                Id = 1,
                UserId = "guest",
                Title = _localizer["GuestTask1"],
                Date = DateTime.Today,
                IsRoutine = false,
                CategoryId = 4,
                Category = learningCat
            },
            new UserTask
            {
                Id = 2,
                UserId = "guest",
                Title = _localizer["GuestTask2"],
                Date = DateTime.Today,
                IsRoutine = false,
                CategoryId = 1,
                Category = workCat
            },
            new UserTask
            {
                Id = 3,
                UserId = "guest",
                Title = _localizer["GuestTask3"],
                Date = DateTime.Today,
                IsRoutine = false,
                CategoryId = 2,
                Category = personalCat
            }
        };
    }
}
