using System.ComponentModel.DataAnnotations;

namespace DisciplineApp.Models;

public class DailyReflection
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public DateTime Date { get; set; }
    public int Mood { get; set; } = 3;
    public string? Note { get; set; }
}
