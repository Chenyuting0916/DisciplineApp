namespace DisciplineApp.Models;

public static class IfThenStart
{
    public static string FocusTitle(string? thenAction)
        => InputGuard.Clamp(thenAction, InputGuard.FocusTaskMax);

    public static bool CanStart(string? thenAction)
        => !string.IsNullOrWhiteSpace(FocusTitle(thenAction));

    public static (string FocusTask, string CustomTitle) ForTimer(IEnumerable<UserTask>? tasks, string? thenAction)
    {
        var title = FocusTitle(thenAction);
        if (string.IsNullOrWhiteSpace(title)) return ("", "");

        var match = tasks?.FirstOrDefault(t => !t.IsCompleted && t.Title == title);
        if (match != null) return (match.Title, "");

        return tasks != null && tasks.Any() ? ("custom", title) : (title, title);
    }
}
