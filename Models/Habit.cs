using System.ComponentModel.DataAnnotations;

namespace DisciplineApp.Models;

public class Habit
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    [Required]
    [MaxLength(80)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(8)]
    public string Icon { get; set; } = "✅";

    [MaxLength(16)]
    public string Color { get; set; } = "#F5A524";

    public int TargetDaysPerWeek { get; set; } = 7;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsArchived { get; set; }

    public ICollection<HabitLog> Logs { get; set; } = new List<HabitLog>();
}
