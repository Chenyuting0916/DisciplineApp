using DisciplineApp.Models;

namespace DisciplineApp.Services.Interfaces;

public interface IShopService
{
    IReadOnlyList<ShopItem> Catalog { get; }
    HashSet<string> ParseOwned(ApplicationUser user);
    Task<(bool success, string reason, ApplicationUser? user)> PurchaseAsync(string userId, string itemId);
    Task<(bool success, ApplicationUser? user)> EquipTitleAsync(string userId, string titleId);
    Task<(bool success, ApplicationUser? user)> EquipThemeAsync(string userId, string themeKey);
}
