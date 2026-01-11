using System.ComponentModel.DataAnnotations;

namespace DisciplineApp.Models;

public class Category
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public string ColorCode { get; set; } = "#808080"; // Default: Grey

    public string? UserId { get; set; } // Nullable, null means system default
    public ApplicationUser? User { get; set; }
}
