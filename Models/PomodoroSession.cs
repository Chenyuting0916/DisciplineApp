using System.ComponentModel.DataAnnotations;

namespace DisciplineApp.Models;

public class PomodoroSession
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public int DurationMinutes { get; set; }
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}
