using Microsoft.AspNetCore.Identity;

namespace DisciplineApp.Models;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
    public int Level { get; set; } = 1;
    public int CurrentXP { get; set; } = 0;
    public int TotalXP { get; set; } = 0;
    public int DailyXpEarned { get; set; } = 0;
    public DateTime? LastXpResetDate { get; set; }
    public DateTime JoinedDate { get; set; } = DateTime.UtcNow;
    public string? PhotoUrl { get; set; }

    // New Gamification Properties
    public int GoldCoins { get; set; }
    public double TotalFocusMinutes { get; set; }

    // Streak & daily goal
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public DateTime? LastActiveDate { get; set; }
    public int DailyFocusGoalMinutes { get; set; } = 60;

    // Identity cosmetics — never trust client-supplied values without an allowlist.
    public string? EquippedTitle { get; set; }
    public string ThemeKey { get; set; } = "ember";
    public string OwnedItems { get; set; } = ShopCatalog.DefaultOwned;
    public int StreakFreezeTokens { get; set; }
}
