using System.Text.Json;
using DisciplineApp.Data;
using DisciplineApp.Models;
using DisciplineApp.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DisciplineApp.Services;

public class DataExportService : IDataExportService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly ApplicationDbContext _context;

    public DataExportService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<string> ExportJsonAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("userId required", nameof(userId));
        }

        var payload = new UserDataExport
        {
            Version = "1",
            ExportedAtUtc = DateTime.UtcNow,
            Source = "account",
            Tasks = await _context.UserTasks
                .AsNoTracking()
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.Date)
                .Take(InputGuard.MaxExportTasks)
                .Select(t => new TaskExportItem
                {
                    Title = t.Title,
                    IsCompleted = t.IsCompleted,
                    IsRoutine = t.IsRoutine,
                    Date = t.Date,
                    CompletedAt = t.CompletedAt
                })
                .ToListAsync(),
            Habits = (await _context.Habits
                .AsNoTracking()
                .Include(h => h.Logs)
                .Where(h => h.UserId == userId)
                .OrderBy(h => h.CreatedAt)
                .Take(InputGuard.MaxHabits)
                .ToListAsync())
                .Select(h => new HabitExportItem
                {
                    Title = h.Title,
                    Icon = h.Icon,
                    Color = h.Color,
                    TargetDaysPerWeek = h.TargetDaysPerWeek,
                    IsArchived = h.IsArchived,
                    LogDates = h.Logs.Select(l => l.Date.Date).Distinct().ToList()
                })
                .ToList(),
            Sessions = await _context.FocusSessions
                .AsNoTracking()
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.EndTime)
                .Take(InputGuard.MaxExportSessions)
                .Select(s => new SessionExportItem
                {
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    DurationMinutes = s.DurationMinutes,
                    TaskTag = s.TaskTag,
                    IsPomodoro = s.IsPomodoro
                })
                .ToListAsync(),
            Reflections = await _context.DailyReflections
                .AsNoTracking()
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.Date)
                .Take(365)
                .Select(r => new ReflectionExportItem
                {
                    Date = r.Date,
                    Mood = r.Mood,
                    Note = r.Note
                })
                .ToListAsync(),
            Intentions = await _context.DailyIntentions
                .AsNoTracking()
                .Where(i => i.UserId == userId)
                .OrderByDescending(i => i.Date)
                .Take(365)
                .Select(i => new IntentionExportItem
                {
                    Date = i.Date,
                    Vow = i.Vow,
                    OneThing = i.OneThing
                })
                .ToListAsync(),
            IfThenPlans = await _context.IfThenPlans
                .AsNoTracking()
                .Where(p => p.UserId == userId)
                .OrderBy(p => p.CreatedAt)
                .Take(InputGuard.MaxIfThenPlans)
                .Select(p => new IfThenExportItem
                {
                    IfCue = p.IfCue,
                    ThenAction = p.ThenAction,
                    IsActive = p.IsActive,
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync()
        };

        return JsonSerializer.Serialize(payload, JsonOptions);
    }
}
