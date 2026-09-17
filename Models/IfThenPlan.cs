using System.ComponentModel.DataAnnotations;

namespace DisciplineApp.Models;

public class IfThenPlan
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(InputGuard.TitleMax)]
    public string IfCue { get; set; } = string.Empty;

    [Required]
    [MaxLength(InputGuard.TitleMax)]
    public string ThenAction { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
