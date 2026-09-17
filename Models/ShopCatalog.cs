namespace DisciplineApp.Models;

public record ShopItem(string Id, string Kind, int Price, string Icon, string TitleKey);

public static class ShopCatalog
{
    public const string DefaultTheme = "ember";
    public const string DefaultOwned = "theme_ember,sound_rain,sound_brown,sound_cafe,sound_fire";

    public static readonly ShopItem[] Items =
    {
        new("title_monk", "title", 80, "🧘", "ShopTitleMonk"),
        new("title_iron", "title", 140, "⚔️", "ShopTitleIron"),
        new("title_flame", "title", 220, "🔥", "ShopTitleFlame"),
        new("title_dawn", "title", 160, "🌅", "ShopTitleDawn"),
        new("theme_forest", "theme", 120, "🌲", "ShopThemeForest"),
        new("theme_midnight", "theme", 120, "🌙", "ShopThemeMidnight"),
        new("theme_rose", "theme", 120, "🌸", "ShopThemeRose"),
        new("pack_freeze", "consumable", 50, "🧊", "ShopFreezePack"),
    };

    public static ShopItem? Find(string id) => Items.FirstOrDefault(i => i.Id == id);

    public static string TitleFor(string? equippedTitleId)
    {
        var item = Items.FirstOrDefault(i => i.Id == equippedTitleId && i.Kind == "title");
        return item?.TitleKey ?? "";
    }

    public static string ThemeFromItem(string itemId) => itemId.StartsWith("theme_") ? itemId["theme_".Length..] : DefaultTheme;
}
