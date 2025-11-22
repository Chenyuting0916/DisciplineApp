using System.ComponentModel.DataAnnotations;

namespace DisciplineApp.Models;

public class UserTask
{
    public int Id { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    [Required]
    public string Title { get; set; } = string.Empty;
    
    public bool IsCompleted { get; set; }
    public bool IsRoutine { get; set; }
    public bool XpAwarded { get; set; }
    public DateTime Date { get; set; } = DateTime.Today;
    public DateTime? CompletedAt { get; set; }
    public DateTime? LastCompletedDate { get; set; }
}
