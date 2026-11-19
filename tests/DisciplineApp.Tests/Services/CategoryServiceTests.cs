using DisciplineApp.Data;
using DisciplineApp.Models;
using DisciplineApp.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DisciplineApp.Tests.Services;

public class CategoryServiceTests
{
    private readonly ApplicationDbContext _db;
    private readonly CategoryService _service;

    public CategoryServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);
        _service = new CategoryService(_db);
    }

    [Fact]
    public async Task Delete_RejectsSystemAndOtherUsers()
    {
        var system = new Category { Name = "Work", ColorCode = "#3B82F6", UserId = null };
        var mine = new Category { Name = "Mine", ColorCode = "#112233", UserId = "u1" };
        var other = new Category { Name = "Other", ColorCode = "#112233", UserId = "u2" };
        _db.Categories.AddRange(system, mine, other);
        await _db.SaveChangesAsync();

        Assert.False(await _service.DeleteCategoryAsync(system.Id, "u1"));
        Assert.False(await _service.DeleteCategoryAsync(other.Id, "u1"));
        Assert.False(await _service.DeleteCategoryAsync(mine.Id, ""));
        Assert.True(await _service.DeleteCategoryAsync(mine.Id, "u1"));
        Assert.Equal(2, await _db.Categories.CountAsync());
    }

    [Fact]
    public async Task Delete_RejectsOwnedCategoryThatIsInUse()
    {
        var mine = new Category { Name = "Mine", ColorCode = "#112233", UserId = "u1" };
        _db.Categories.Add(mine);
        await _db.SaveChangesAsync();
        _db.UserTasks.Add(new UserTask { UserId = "u1", Title = "Ship", CategoryId = mine.Id, Date = DateTime.Today });
        await _db.SaveChangesAsync();

        Assert.False(await _service.DeleteCategoryAsync(mine.Id, "u1"));
        Assert.Equal(1, await _db.Categories.CountAsync());
    }

    [Fact]
    public async Task Create_ClampsNameAndRejectsBadColor()
    {
        var created = await _service.CreateCategoryAsync("u1", new string('名', 200), "javascript:alert(1)");
        Assert.Equal(InputGuard.TitleMax, created.Name.Length);
        Assert.Equal("#808080", created.ColorCode);
        Assert.Equal("u1", created.UserId);
    }
}
