namespace DisciplineApp.Models;

public static class GuestLocalData
{
    public static readonly IReadOnlyList<string> Keys = new[]
    {
        "guest_focus_v1",
        "guest_tasks",
        "guest_habits_v1",
        "guest_journal_v1",
        "guest_ifthen_v1"
    };

    public static bool IsAllowedKey(string? key)
        => !string.IsNullOrWhiteSpace(key) && Keys.Contains(key);

    public static IReadOnlyList<string> KeysToRemove(IEnumerable<string?> candidates)
        => candidates.Where(IsAllowedKey).Select(k => k!).Distinct().ToList();
}
