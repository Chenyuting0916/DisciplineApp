using System.Globalization;

namespace DisciplineApp.Models;

public static class CalendarCopy
{
    public static string EventWhen(DateTimeOffset? dateTime, string? dateOnly, CultureInfo culture, string allDayLabel)
    {
        culture ??= CultureInfo.InvariantCulture;
        if (dateTime.HasValue)
        {
            return dateTime.Value.ToLocalTime().DateTime.ToString("g", culture);
        }

        if (!string.IsNullOrWhiteSpace(dateOnly)
            && DateTime.TryParse(dateOnly, CultureInfo.InvariantCulture, DateTimeStyles.None, out var day))
        {
            return $"{day.ToString("d", culture)} · {allDayLabel}";
        }

        return string.Empty;
    }

    public static string TaskDue(string? due, CultureInfo culture, string dueLabel)
    {
        culture ??= CultureInfo.InvariantCulture;
        if (string.IsNullOrWhiteSpace(due)
            || !DateTime.TryParse(due, CultureInfo.InvariantCulture, DateTimeStyles.None, out var day))
        {
            return string.Empty;
        }

        return $"{dueLabel} {day.ToString("d", culture)}";
    }
}
