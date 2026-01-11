using System;
using System.ComponentModel.DataAnnotations;

namespace DisciplineApp.Models;

public class AnalyticsEvent
{
    public int Id { get; set; }

    public string? UserId { get; set; }

    [Required]
    public string EventName { get; set; } = string.Empty;

    public string? Category { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public string? Data { get; set; } // JSON or simple string for metadata
}
