namespace DisciplineApp.Models;

public sealed record ShortcutRow(string Keys, string ActionKey);

public static class ShortcutCopy
{
    public static IReadOnlyList<ShortcutRow> Rows { get; } = new ShortcutRow[]
    {
        new("Space", "ShortcutSpace"),
        new("1 / 2 / 3", "ShortcutMinutes"),
        new("S", "ShortcutSkipBreath"),
        new("Esc", "ShortcutLeaveSanctuary")
    };
}
