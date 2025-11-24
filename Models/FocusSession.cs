using System;
using System.ComponentModel.DataAnnotations;

namespace DisciplineApp.Models
{
    public class FocusSession
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public double DurationMinutes { get; set; }

        public string? TaskTag { get; set; } // What the user was focusing on
        public bool IsPomodoro { get; set; } // True if Pomodoro mode, False if Stopwatch
    }
}
