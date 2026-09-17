using DisciplineApp.Models;

namespace DisciplineApp.Models;

public static class FocusTaskMatcher
{
    public static UserTask? FindOpen(IEnumerable<UserTask>? tasks, string? focusTask, string? customTitle)
    {
        if (tasks == null) return null;

        var title = string.Equals(focusTask, "custom", StringComparison.Ordinal)
            ? customTitle
            : focusTask;
        title = InputGuard.Clamp(title, InputGuard.FocusTaskMax);
        if (string.IsNullOrWhiteSpace(title)) return null;

        return tasks.FirstOrDefault(t => !t.IsCompleted && t.Title == title);
    }

    public static (string FocusTask, string CustomTitle) Prefill(IEnumerable<UserTask>? tasks, string? oneThing)
    {
        var title = InputGuard.Clamp(oneThing, InputGuard.FocusTaskMax);
        if (string.IsNullOrWhiteSpace(title)) return ("", "");

        var match = FindOpen(tasks, title, null);
        return match != null ? (match.Title, "") : ("custom", title);
    }
}
