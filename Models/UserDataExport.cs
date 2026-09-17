namespace DisciplineApp.Models;

public class UserDataExport
{
    public string Version { get; set; } = "1";
    public DateTime ExportedAtUtc { get; set; }
    public string Source { get; set; } = "account";
    public List<TaskExportItem> Tasks { get; set; } = new();
    public List<HabitExportItem> Habits { get; set; } = new();
    public List<SessionExportItem> Sessions { get; set; } = new();
    public List<ReflectionExportItem> Reflections { get; set; } = new();
    public List<IntentionExportItem> Intentions { get; set; } = new();
    public List<IfThenExportItem> IfThenPlans { get; set; } = new();
}

public class TaskExportItem
{
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public bool IsRoutine { get; set; }
    public DateTime Date { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class HabitExportItem
{
    public string Title { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int TargetDaysPerWeek { get; set; }
    public bool IsArchived { get; set; }
    public List<DateTime> LogDates { get; set; } = new();
}

public class SessionExportItem
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public double DurationMinutes { get; set; }
    public string? TaskTag { get; set; }
    public bool IsPomodoro { get; set; }
}

public class ReflectionExportItem
{
    public DateTime Date { get; set; }
    public int Mood { get; set; }
    public string? Note { get; set; }
}

public class IntentionExportItem
{
    public DateTime Date { get; set; }
    public string Vow { get; set; } = string.Empty;
    public string OneThing { get; set; } = string.Empty;
}

public class IfThenExportItem
{
    public string IfCue { get; set; } = string.Empty;
    public string ThenAction { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
