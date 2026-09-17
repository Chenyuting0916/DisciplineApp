using DisciplineApp.Data;
using DisciplineApp.Models;
using DisciplineApp.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DisciplineApp.Services;

public class ReflectionService : IReflectionService
{
    private readonly ApplicationDbContext _context;
    private readonly IGamificationService _gamificationService;

    public ReflectionService(ApplicationDbContext context, IGamificationService gamificationService)
    {
        _context = context;
        _gamificationService = gamificationService;
    }

    public async Task<DailyReflection?> GetTodayAsync(string userId)
    {
        var today = DateTime.UtcNow.Date;
        return await _context.DailyReflections
            .FirstOrDefaultAsync(r => r.UserId == userId && r.Date == today);
    }

    public async Task<DailyReflection> SaveTodayAsync(string userId, int mood, string? note)
    {
        var today = DateTime.UtcNow.Date;
        var existing = await GetTodayAsync(userId);
        var isNew = existing == null;

        if (existing == null)
        {
            existing = new DailyReflection
            {
                UserId = userId,
                Date = today
            };
            _context.DailyReflections.Add(existing);
        }

        existing.Mood = Math.Clamp(mood, 1, 5);
        existing.Note = string.IsNullOrWhiteSpace(note) ? null : InputGuard.Clamp(note, InputGuard.NoteMax);
        await _context.SaveChangesAsync();

        if (isNew)
        {
            await _gamificationService.RecordActivityAsync(userId);
        }

        return existing;
    }
}
