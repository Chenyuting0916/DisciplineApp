using DisciplineApp.Data;
using DisciplineApp.Models;
using DisciplineApp.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DisciplineApp.Services;

public class IntentionService : IIntentionService
{
    private readonly ApplicationDbContext _context;
    private readonly IGamificationService _gamificationService;

    public IntentionService(ApplicationDbContext context, IGamificationService gamificationService)
    {
        _context = context;
        _gamificationService = gamificationService;
    }

    public async Task<DailyIntention?> GetTodayAsync(string userId)
    {
        var today = DateTime.UtcNow.Date;
        return await _context.DailyIntentions
            .FirstOrDefaultAsync(i => i.UserId == userId && i.Date == today);
    }

    public async Task<DailyIntention> SaveTodayAsync(string userId, string vow, string oneThing)
    {
        var today = DateTime.UtcNow.Date;
        var existing = await GetTodayAsync(userId);
        var isNew = existing == null;

        if (existing == null)
        {
            existing = new DailyIntention { UserId = userId, Date = today };
            _context.DailyIntentions.Add(existing);
        }

        existing.Vow = InputGuard.Clamp(vow, InputGuard.VowMax);
        existing.OneThing = InputGuard.Clamp(oneThing, InputGuard.TitleMax);
        await _context.SaveChangesAsync();

        if (isNew && (!string.IsNullOrWhiteSpace(existing.Vow) || !string.IsNullOrWhiteSpace(existing.OneThing)))
        {
            await _gamificationService.RecordActivityAsync(userId);
        }

        return existing;
    }
}
