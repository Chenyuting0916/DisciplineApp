using Microsoft.Extensions.Localization;

namespace DisciplineApp.Services;

public class QuoteService
{
    private readonly IStringLocalizer<App> _localizer;

    public QuoteService(IStringLocalizer<App> localizer)
    {
        _localizer = localizer;
    }

    public string GetQuoteOfTheDay()
    {
        var index = (DateTime.UtcNow.DayOfYear % 10) + 1;
        return _localizer[$"Quote{index}"];
    }
}
