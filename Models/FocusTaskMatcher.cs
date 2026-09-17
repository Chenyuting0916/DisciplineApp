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

    public static (string FocusTask, string CustomTitle) KeepAfterStart(
        IEnumerable<UserTask>? tasks, string? focusTask, string? customTitle, string resolved)
    {
        resolved = InputGuard.Clamp(resolved, InputGuard.FocusTaskMax);
        var listed = tasks != null && tasks.Any();
        if (string.Equals(focusTask, "custom", StringComparison.Ordinal) || !listed)
        {
            return listed ? ("custom", resolved) : (resolved, resolved);
        }

        return (resolved, customTitle ?? "");
    }

    public static bool CanStartFocus(UserTask? task)
        => task != null && (!task.IsCompleted || task.IsRoutine);

    public static string StartFromTask(UserTask? task)
        => CanStartFocus(task) ? InputGuard.Clamp(task!.Title, InputGuard.FocusTaskMax) : "";
}
