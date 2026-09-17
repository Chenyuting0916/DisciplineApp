using System.Text.Encodings.Web;
using System.Text.Json;

namespace DisciplineApp.Models;

public static class ManifestCopy
{
    public const int DescriptionMax = 160;

    public static string Lang(string? culture)
        => InputGuard.IsAllowedCulture(culture) ? culture! : "zh-TW";

    public static string Json(string? culture, string? description)
    {
        var lang = Lang(culture);
        var desc = InputGuard.Clamp(description, DescriptionMax);
        var payload = new Dictionary<string, object?>
        {
            ["name"] = "Discipline",
            ["short_name"] = "Discipline",
            ["lang"] = lang,
            ["description"] = desc,
            ["start_url"] = "/",
            ["display"] = "standalone",
            ["background_color"] = "#0b1018",
            ["theme_color"] = "#f5a524",
            ["icons"] = new object[]
            {
                new Dictionary<string, string>
                {
                    ["src"] = "favicon.png",
                    ["sizes"] = "192x192",
                    ["type"] = "image/png"
                }
            }
        };
        return JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
    }
}
