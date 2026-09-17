using System.Globalization;

namespace DisciplineApp.Models;

public static class HeatmapCopy
{
    public static string DayLabel(DayOfWeek day, CultureInfo culture)
        => culture.DateTimeFormat.GetAbbreviatedDayName(day);

    public static string MonthLabel(DateTime date, CultureInfo culture)
        => date.ToString("MMM", culture);

    public static string Tooltip(DateTime date, int count, string activityFormat)
    {
        var day = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var activity = string.Format(CultureInfo.InvariantCulture, activityFormat, Math.Max(count, 0));
        return $"{day}: {activity}";
    }
}
