using DisciplineApp.Data;
using DisciplineApp.Models;
using DisciplineApp.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace DisciplineApp.Tests.Services;

public class ShopAndSecurityTests
{
    private readonly Mock<UserManager<ApplicationUser>> _users;
    private readonly List<ApplicationUser> _store = new();
    private readonly ShopService _shop;
    private readonly ApplicationDbContext _db;
    private readonly IntentionService _intentions;

    public ShopAndSecurityTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);

        var userStore = new Mock<IUserStore<ApplicationUser>>();
        _users = new Mock<UserManager<ApplicationUser>>(userStore.Object, null, null, null, null, null, null, null, null);
        _users.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string id) => _store.FirstOrDefault(u => u.Id == id));
        _users.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        _shop = new ShopService(_users.Object);
        var gamification = new GamificationService(_db, _users.Object);
        _intentions = new IntentionService(_db, gamification);
    }

    [Fact]
    public async Task Purchase_RejectsUnknownItem()
    {
        _store.Add(new ApplicationUser { Id = "u1", GoldCoins = 999 });
        var result = await _shop.PurchaseAsync("u1", "hack_admin");
        Assert.False(result.success);
        Assert.Equal("unknown", result.reason);
        Assert.Equal(999, _store[0].GoldCoins);
    }

    [Fact]
    public async Task Purchase_RejectsIfBroke()
    {
        _store.Add(new ApplicationUser { Id = "u1", GoldCoins = 10 });
        var result = await _shop.PurchaseAsync("u1", "title_monk");
        Assert.False(result.success);
        Assert.Equal("coins", result.reason);
    }

    [Fact]
    public async Task Purchase_TitleOnceThenOwned()
    {
        _store.Add(new ApplicationUser { Id = "u1", GoldCoins = 200, OwnedItems = ShopCatalog.DefaultOwned });
        var first = await _shop.PurchaseAsync("u1", "title_monk");
        Assert.True(first.success);
        Assert.Equal(120, _store[0].GoldCoins);

        var second = await _shop.PurchaseAsync("u1", "title_monk");
        Assert.False(second.success);
        Assert.Equal("owned", second.reason);
    }

    [Fact]
    public async Task EquipTheme_RejectsUnowned()
    {
        _store.Add(new ApplicationUser { Id = "u1", ThemeKey = "ember", OwnedItems = ShopCatalog.DefaultOwned });
        var result = await _shop.EquipThemeAsync("u1", "forest");
        Assert.False(result.success);
        Assert.Equal("ember", _store[0].ThemeKey);
    }

    [Fact]
    public async Task EquipTheme_RejectsUnknownKey()
    {
        _store.Add(new ApplicationUser { Id = "u1", ThemeKey = "ember" });
        var result = await _shop.EquipThemeAsync("u1", "javascript:alert(1)");
        Assert.False(result.success);
    }

    [Fact]
    public async Task FreezePack_AddsTokenNotOwnershipDup()
    {
        _store.Add(new ApplicationUser { Id = "u1", GoldCoins = 120, StreakFreezeTokens = 0 });
        var a = await _shop.PurchaseAsync("u1", "pack_freeze");
        var b = await _shop.PurchaseAsync("u1", "pack_freeze");
        Assert.True(a.success);
        Assert.True(b.success);
        Assert.Equal(2, _store[0].StreakFreezeTokens);
        Assert.Equal(20, _store[0].GoldCoins);
    }

    [Fact]
    public void InputGuard_ClampsAndStripsControls()
    {
        var text = "hello\u0000world" + new string('x', 200);
        var clamped = InputGuard.Clamp(text, 20);
        Assert.DoesNotContain('\0', clamped);
        Assert.True(clamped.Length <= 20);
        Assert.False(InputGuard.IsHttpsUrl("javascript:alert(1)"));
        Assert.False(InputGuard.IsHttpsUrl("http://insecure.example/x.png"));
        Assert.True(InputGuard.IsHttpsUrl("https://lh3.googleusercontent.com/a"));
        Assert.False(InputGuard.IsAllowedTheme("ember; background:red"));
    }

    [Fact]
    public async Task Intention_ClampsVowLength()
    {
        _store.Add(new ApplicationUser { Id = "u1" });
        var saved = await _intentions.SaveTodayAsync("u1", new string('我', 400), new string('a', 200));
        Assert.Equal(InputGuard.VowMax, saved.Vow.Length);
        Assert.Equal(InputGuard.TitleMax, saved.OneThing.Length);
    }
}
