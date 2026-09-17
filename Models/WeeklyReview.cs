namespace DisciplineApp.Models;

public class WeeklyReview
{
    public double FocusMinutes { get; set; }
    public int SessionCount { get; set; }
    public int TasksCompleted { get; set; }
    public int HabitChecks { get; set; }
    public string? TopFocus { get; set; }
    public DateTime? BestDay { get; set; }
    public double BestDayMinutes { get; set; }
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public int Level { get; set; }
}
