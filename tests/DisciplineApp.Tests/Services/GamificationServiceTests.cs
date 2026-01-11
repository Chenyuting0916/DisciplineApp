using DisciplineApp.Data;
using DisciplineApp.Models;
using DisciplineApp.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace DisciplineApp.Tests.Services;

public class GamificationServiceTests
{
    private GamificationService _gamificationService;
    private Mock<UserManager<ApplicationUser>> _mockUserManager;
    private ApplicationDbContext _context;
    private List<ApplicationUser> _users;

    public GamificationServiceTests()
    {
        // Setup InMemory DB
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        // Setup Mock UserManager
        _users = new List<ApplicationUser>();
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null, null, null, null, null, null, null, null);

        // Common setup for FindByIdAsync
        _mockUserManager.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string id) => _users.FirstOrDefault(u => u.Id == id));
            
        _mockUserManager.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        _gamificationService = new GamificationService(_context, _mockUserManager.Object);
    }

    [Fact]
    public async Task AddXpAsync_ShouldAddXp_WhenBelowCap()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1", DailyXpEarned = 0, CurrentXP = 0, TotalXP = 0 };
        _users.Add(user);

        // Act
        var result = await _gamificationService.AddXpAsync("user1", 50);

        // Assert
        Assert.True(result.success);
        Assert.Equal(50, result.xpAwarded);
        Assert.Equal(50, user.CurrentXP);
        Assert.Equal(50, user.DailyXpEarned);
    }

    [Fact]
    public async Task AddXpAsync_ShouldCapDailyXp()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1", DailyXpEarned = 450, CurrentXP = 450, TotalXP = 450, LastXpResetDate = DateTime.UtcNow };
        _users.Add(user);

        // Act - Try to add 100 XP (Cap is 500)
        var result = await _gamificationService.AddXpAsync("user1", 100);

        // Assert
        Assert.True(result.success);
        Assert.Equal(50, result.xpAwarded); // Only 50 remaining until 500
        Assert.Equal(500, user.DailyXpEarned);
        
        // Try to add more
        var result2 = await _gamificationService.AddXpAsync("user1", 10);
        Assert.False(result2.success);
        Assert.Equal(0, result2.xpAwarded);
    }

    [Fact]
    public async Task AddXpAsync_ShouldResetDailyXp_OnNewDay()
    {
        // Arrange
        var user = new ApplicationUser 
        { 
            Id = "user1", 
            DailyXpEarned = 500, 
            LastXpResetDate = DateTime.UtcNow.AddDays(-1) 
        };
        _users.Add(user);

        // Act
        var result = await _gamificationService.AddXpAsync("user1", 50);

        // Assert
        Assert.True(result.success);
        Assert.Equal(50, result.xpAwarded);
        Assert.Equal(50, user.DailyXpEarned); // Reset to 0 then add 50
        Assert.Equal(DateTime.UtcNow.Date, user.LastXpResetDate?.Date);
    }
}
