using DisciplineApp.Data;
using DisciplineApp.Models;
using DisciplineApp.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace DisciplineApp.Services;

public class ShopService : IShopService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ShopService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public IReadOnlyList<ShopItem> Catalog => ShopCatalog.Items;

    public HashSet<string> ParseOwned(ApplicationUser user)
    {
        var raw = string.IsNullOrWhiteSpace(user.OwnedItems) ? ShopCatalog.DefaultOwned : user.OwnedItems;
        var allowed = ShopCatalog.Items.Select(i => i.Id)
            .Concat(new[] { "theme_ember", "sound_rain", "sound_brown", "sound_cafe", "sound_fire" })
            .ToHashSet();
        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(allowed.Contains)
            .ToHashSet();
    }

    public async Task<(bool success, string reason, ApplicationUser? user)> PurchaseAsync(string userId, string itemId)
    {
        var item = ShopCatalog.Find(itemId);
        if (item == null || item.Price < 0) return (false, "unknown", null);

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return (false, "unknown", null);

        var owned = ParseOwned(user);
        if (item.Kind != "consumable" && owned.Contains(item.Id))
        {
            return (false, "owned", user);
        }

        if (user.GoldCoins < item.Price)
        {
            return (false, "coins", user);
        }

        user.GoldCoins -= item.Price;
        if (item.Kind == "consumable" && item.Id == "pack_freeze")
        {
            user.StreakFreezeTokens++;
        }
        else
        {
            owned.Add(item.Id);
            user.OwnedItems = string.Join(',', owned.OrderBy(x => x));
        }

        await _userManager.UpdateAsync(user);
        return (true, "ok", user);
    }

    public async Task<(bool success, ApplicationUser? user)> EquipTitleAsync(string userId, string titleId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return (false, null);

        if (string.IsNullOrEmpty(titleId))
        {
            user.EquippedTitle = null;
            await _userManager.UpdateAsync(user);
            return (true, user);
        }

        var item = ShopCatalog.Find(titleId);
        if (item is not { Kind: "title" }) return (false, user);
        if (!ParseOwned(user).Contains(titleId)) return (false, user);

        user.EquippedTitle = titleId;
        await _userManager.UpdateAsync(user);
        return (true, user);
    }

    public async Task<(bool success, ApplicationUser? user)> EquipThemeAsync(string userId, string themeKey)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return (false, null);
        if (!InputGuard.IsAllowedTheme(themeKey)) return (false, user);

        var owned = ParseOwned(user);
        var itemId = $"theme_{themeKey}";
        if (themeKey != ShopCatalog.DefaultTheme && !owned.Contains(itemId))
        {
            return (false, user);
        }

        user.ThemeKey = themeKey;
        await _userManager.UpdateAsync(user);
        return (true, user);
    }
}
