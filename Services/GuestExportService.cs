using System.Text.Json;
using DisciplineApp.Models;

namespace DisciplineApp.Services;

public class GuestExportService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly GuestTaskService _tasks;
    private readonly GuestHabitService _habits;
    private readonly GuestIfThenService _plans;

    public GuestExportService(GuestTaskService tasks, GuestHabitService habits, GuestIfThenService plans)
    {
        _tasks = tasks;
        _habits = habits;
        _plans = plans;
    }

    public async Task<string> ExportJsonAsync()
    {
        var tasks = await _tasks.GetAllAsync();
        var habits = await _habits.GetAllIncludingArchivedAsync();
        var plans = await _plans.GetAllAsync();

        var payload = new UserDataExport
        {
            Version = "1",
            ExportedAtUtc = DateTime.UtcNow,
            Source = "guest",
            Tasks = tasks.Take(InputGuard.MaxExportTasks).Select(t => new TaskExportItem
            {
                Title = t.Title,
                IsCompleted = t.IsCompleted,
                IsRoutine = t.IsRoutine,
                Date = t.Date,
                CompletedAt = t.CompletedAt
            }).ToList(),
            Habits = habits.Take(InputGuard.MaxHabits).Select(h => new HabitExportItem
            {
                Title = h.Title,
                Icon = h.Icon,
                Color = h.Color,
                TargetDaysPerWeek = h.TargetDaysPerWeek,
                IsArchived = h.IsArchived,
                LogDates = h.Logs.Select(l => l.Date.Date).Distinct().ToList()
            }).ToList(),
            IfThenPlans = plans.Take(InputGuard.MaxIfThenPlans).Select(p => new IfThenExportItem
            {
                IfCue = p.IfCue,
                ThenAction = p.ThenAction,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt
            }).ToList()
        };

        return JsonSerializer.Serialize(payload, JsonOptions);
    }
}
