using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using DisciplineApp.Models;

namespace DisciplineApp.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorModel : PageModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    public bool ShowDevelopmentHelp { get; private set; }

    public IStringLocalizer<App> Localizer { get; }

    private readonly IWebHostEnvironment _environment;

    public ErrorModel(ILogger<ErrorModel> logger, IStringLocalizer<App> localizer, IWebHostEnvironment environment)
    {
        Localizer = localizer;
        _environment = environment;
        _ = logger;
    }

    public void OnGet()
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        ShowDevelopmentHelp = ErrorCopy.ShowDevelopmentHelp(_environment.EnvironmentName);
    }
}
