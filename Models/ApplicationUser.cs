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
}
