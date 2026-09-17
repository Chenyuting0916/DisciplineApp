using System.Text;

namespace DisciplineApp.Models;

public static class InputGuard
{
    public const int TitleMax = 80;
    public const int NoteMax = 280;
    public const int VowMax = 140;
    public const int DisplayNameMax = 40;
    public const int FocusTaskMax = 80;
    public const int MaxHabits = 30;
    public const int MaxIfThenPlans = 12;
    public const int MaxExportTasks = 500;
    public const int MaxExportSessions = 400;

    public static readonly string[] AllowedCultures = { "zh-TW", "en", "ja" };
    public static readonly string[] AllowedThemes = { "ember", "forest", "midnight", "rose" };

    public static string Clamp(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var trimmed = value.Trim();
        // Strip control characters that can break pages or logs.
        var cleaned = new StringBuilder(trimmed.Length);
        foreach (var c in trimmed)
        {
            if (char.IsControl(c) && c != '\n' && c != '\r' && c != '\t') continue;
            cleaned.Append(c);
        }

        var text = cleaned.ToString();
        return text.Length <= max ? text : text[..max];
    }

    public static bool IsHttpsUrl(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
               && uri.Scheme == Uri.UriSchemeHttps
               && !string.IsNullOrEmpty(uri.Host);
    }

    public static bool IsAllowedCulture(string? culture)
        => !string.IsNullOrWhiteSpace(culture) && AllowedCultures.Contains(culture);

    public static bool IsAllowedTheme(string? theme)
        => !string.IsNullOrWhiteSpace(theme) && AllowedThemes.Contains(theme);

    public static bool IsSafeHexColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color) || color.Length != 7 || color[0] != '#') return false;
        return color.Skip(1).All(Uri.IsHexDigit);
    }

    public static string ExportFileName()
        => $"discipline-export-{DateTime.UtcNow:yyyyMMdd}.json";
}
