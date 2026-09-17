namespace DisciplineApp.Models;

public static class HeaderCopy
{
    public const string MainContentId = "main-content";

    public static string DisplayName(string? displayName, string? identityName, string userFallback)
    {
        if (!string.IsNullOrWhiteSpace(displayName)) return displayName.Trim();
        if (!string.IsNullOrWhiteSpace(identityName)) return identityName.Trim();
        return string.IsNullOrWhiteSpace(userFallback) ? "" : userFallback.Trim();
    }

    public static string LevelBadge(string? levelLabel, int level)
    {
        var label = levelLabel?.Trim() ?? "";
        return string.IsNullOrEmpty(label) ? level.ToString() : $"{label} {level}";
    }
}
