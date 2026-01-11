namespace DisciplineApp.Models;

public class Challenge
{
    public int Id { get; set; }
    public string Type { get; set; } = "7-day-focus"; // Challenge type identifier
    public string ShareToken { get; set; } = string.Empty; // Unique token for sharing
    
    // Creator information
    public string CreatedByUserId { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
    
    // Acceptance tracking
    public string? AcceptedByUserId { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    // Status
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ApplicationUser? CreatedBy { get; set; }
    public ApplicationUser? AcceptedBy { get; set; }
}
