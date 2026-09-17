using DisciplineApp.Data;
using DisciplineApp.Models;
using DisciplineApp.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DisciplineApp.Services;

public class IfThenService : IIfThenService
{
    private readonly ApplicationDbContext _context;

    public IfThenService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<IfThenPlan>> GetActiveAsync(string userId)
    {
        return await _context.IfThenPlans
            .Where(p => p.UserId == userId && p.IsActive)
            .OrderBy(p => p.CreatedAt)
            .Take(InputGuard.MaxIfThenPlans)
            .ToListAsync();
    }

    public async Task<(bool ok, string reason)> AddAsync(string userId, string ifCue, string thenAction)
    {
        var cue = InputGuard.Clamp(ifCue, InputGuard.TitleMax);
        var action = InputGuard.Clamp(thenAction, InputGuard.TitleMax);
        if (string.IsNullOrWhiteSpace(cue) || string.IsNullOrWhiteSpace(action))
        {
            return (false, "empty");
        }

        var count = await _context.IfThenPlans.CountAsync(p => p.UserId == userId && p.IsActive);
        if (count >= InputGuard.MaxIfThenPlans)
        {
            return (false, "limit");
        }

        _context.IfThenPlans.Add(new IfThenPlan
        {
            UserId = userId,
            IfCue = cue,
            ThenAction = action,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        return (true, "ok");
    }

    public async Task<bool> DeleteAsync(string userId, int id)
    {
        var plan = await _context.IfThenPlans.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
        if (plan == null) return false;
        _context.IfThenPlans.Remove(plan);
        await _context.SaveChangesAsync();
        return true;
    }
}
