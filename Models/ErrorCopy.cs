namespace DisciplineApp.Models;

public static class ErrorCopy
{
    public static string HtmlLang(string? culture)
        => InputGuard.IsAllowedCulture(culture) ? culture! : "zh-TW";

    public static bool ShowDevelopmentHelp(string? environmentName)
        => string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase);
}
