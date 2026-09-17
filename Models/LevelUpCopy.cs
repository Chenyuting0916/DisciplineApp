namespace DisciplineApp.Models;

public static class LevelUpCopy
{
    public static string NowAt(string? template, int level)
    {
        var format = string.IsNullOrWhiteSpace(template) ? "{0}" : template.Trim();
        try
        {
            return string.Format(format, level);
        }
        catch (FormatException)
        {
            return $"{level}";
        }
    }
}
