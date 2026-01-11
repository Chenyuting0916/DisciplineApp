using DisciplineApp.Data;
using DisciplineApp.Models;
using DisciplineApp.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace DisciplineApp.Services;

public class ChallengeService : IChallengeService
{
    private readonly ApplicationDbContext _context;
    private readonly ITaskService _taskService;
    private readonly GuestTaskService _guestTaskService;
    private readonly IStringLocalizer<App> _localizer;

    public ChallengeService(
        ApplicationDbContext context, 
        ITaskService taskService,
        GuestTaskService guestTaskService,
        IStringLocalizer<App> localizer)
    {
        _context = context;
        _taskService = taskService;
        _guestTaskService = guestTaskService;
        _localizer = localizer;
    }

    public async Task<string> CreateChallengeAsync(string userId, string userName, string challengeType = "7-day-focus")
    {
        var shareToken = GenerateShareToken();
        
        var challenge = new Challenge
        {
            Type = challengeType,
            ShareToken = shareToken,
            CreatedByUserId = userId,
            CreatedByName = userName,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.Challenges.Add(challenge);
        await _context.SaveChangesAsync();

        return shareToken;
    }

    public async Task<Challenge?> GetChallengeByTokenAsync(string token)
    {
        return await _context.Challenges
            .Include(c => c.CreatedBy)
            .FirstOrDefaultAsync(c => c.ShareToken == token && c.IsActive);
    }

    public async Task<bool> AcceptChallengeAsync(string token, string? userId = null)
    {
        var challenge = await GetChallengeByTokenAsync(token);
        if (challenge == null) return false;

        // Mark challenge as accepted
        challenge.AcceptedByUserId = userId ?? "guest";
        challenge.AcceptedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Create challenge tasks based on type
        await CreateChallengeTasks(challenge, userId);

        return true;
    }

    public async Task<List<Challenge>> GetUserChallengesAsync(string userId)
    {
        return await _context.Challenges
            .Where(c => c.CreatedByUserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .Include(c => c.AcceptedBy)
            .ToListAsync();
    }

    public async Task<bool> CheckChallengeCompletionAsync(int challengeId)
    {
        var challenge = await _context.Challenges.FindAsync(challengeId);
        if (challenge == null || challenge.AcceptedByUserId == null) return false;

        // For 7-day-focus: check if user completed at least 1 task per day for 7 days
        if (challenge.Type == "7-day-focus" && challenge.AcceptedAt.HasValue)
        {
            var startDate = challenge.AcceptedAt.Value.Date;
            var endDate = startDate.AddDays(7);

            if (challenge.AcceptedByUserId == "guest")
            {
                // For guests, we can't reliably check completion
                return false;
            }

            // Check if user has completed tasks for 7 consecutive days
            var completedDays = await _context.UserTasks
                .Where(t => t.UserId == challenge.AcceptedByUserId 
                    && t.IsCompleted 
                    && t.Date >= startDate 
                    && t.Date < endDate)
                .Select(t => t.Date.Date)
                .Distinct()
                .CountAsync();

            if (completedDays >= 7)
            {
                challenge.CompletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return true;
            }
        }

        return false;
    }

    private async Task CreateChallengeTasks(Challenge challenge, string? userId)
    {
        if (challenge.Type == "7-day-focus")
        {
            var startDate = DateTime.Today;
            var categories = await _taskService.GetCategoriesAsync(userId ?? "");

            for (int i = 0; i < 7; i++)
            {
                var taskDate = startDate.AddDays(i);
                var taskTitle = string.Format(_localizer["ChallengeTask7Day"], i + 1, challenge.CreatedByName);

                if (userId != null)
                {
                    // Authenticated user - save to database
                    await _taskService.AddTaskAsync(userId, taskTitle, taskDate, isRoutine: false, categoryId: 1);
                }
                else
                {
                    // Guest user - save to LocalStorage via GuestTaskService
                    await _guestTaskService.AddTaskAsync(taskTitle, isRoutine: false, categoryId: 1, categories);
                }
            }
        }
    }

    private string GenerateShareToken()
    {
        // Generate a short, URL-friendly token
        return Guid.NewGuid().ToString("N").Substring(0, 8);
    }
}
