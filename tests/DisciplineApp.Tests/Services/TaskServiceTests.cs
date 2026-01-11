using DisciplineApp.Data;
using DisciplineApp.Models;
using DisciplineApp.Services;
using DisciplineApp.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace DisciplineApp.Tests.Services;

public class TaskServiceTests
{
    private TaskService _taskService;
    private Mock<IGamificationService> _mockGamificationService;
    private ApplicationDbContext _context;

    public TaskServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique DB per test
            .Options;

        _context = new ApplicationDbContext(options);
        
        _mockGamificationService = new Mock<IGamificationService>();

        // Setup default mock behavior
        _mockGamificationService.Setup(s => s.AddXpAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync((true, 50, 450, false)); 

        _taskService = new TaskService(_context, _mockGamificationService.Object);
    }

    [Fact]
    public async Task AddTaskAsync_ShouldAddTaskToDatabase()
    {
        // Arrange
        string userId = "user1";
        string title = "New Task";
        DateTime date = DateTime.Today;

        // Act
        var result = await _taskService.AddTaskAsync(userId, title, date);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(title, result.Title);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(date, result.Date);
        
        var dbTask = await _context.UserTasks.FirstOrDefaultAsync(t => t.Id == result.Id);
        Assert.NotNull(dbTask);
    }

    [Fact]
    public async Task ToggleTaskCompletionAsync_ShouldAwardXp_WhenCompletedFirstTime()
    {
        // Arrange
        string userId = "user1";
        var task = await _taskService.AddTaskAsync(userId, "Test Task", DateTime.Today);
        
        // Act
        var result = await _taskService.ToggleTaskCompletionAsync(task.Id, userId);

        // Assert
        Assert.True(result.success);
        Assert.Equal(50, result.xpAwarded);
        
        _mockGamificationService.Verify(s => s.AddXpAsync(userId, 50), Times.Once);
        
        var dbTask = await _context.UserTasks.FindAsync(task.Id);
        Assert.True(dbTask.IsCompleted);
        Assert.True(dbTask.XpAwarded);
    }

    [Fact]
    public async Task ToggleTaskCompletionAsync_ShouldNotAwardXp_WhenAlreadyAwarded()
    {
        // Arrange
        string userId = "user1";
        var task = await _taskService.AddTaskAsync(userId, "Test Task", DateTime.Today);
        task.XpAwarded = true;
        await _context.SaveChangesAsync();
        
        // Act
        var result = await _taskService.ToggleTaskCompletionAsync(task.Id, userId);

        // Assert
        // Result logic: if XP not awarded, success is false? Let's check logic.
        // TaskService: if (shouldAwardXp) ... else return (false, 0, 0, false);
        Assert.False(result.success);
        Assert.Equal(0, result.xpAwarded);
        
        _mockGamificationService.Verify(s => s.AddXpAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        
        var dbTask = await _context.UserTasks.FindAsync(task.Id);
        Assert.True(dbTask.IsCompleted);
    }
    [Fact]
    public async Task AddTaskAsync_WithCategory_ShouldAssignCategory()
    {
        // Arrange
        string userId = "user1";
        string title = "Task with Category";
        DateTime date = DateTime.Today;
        var category = new Category { Name = "Work", UserId = userId };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        
        // Act
        var result = await _taskService.AddTaskAsync(userId, title, date, categoryId: category.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(category.Id, result.CategoryId);
        
        var dbTask = await _context.UserTasks.FindAsync(result.Id);
        Assert.Equal(category.Id, dbTask.CategoryId);
    }

    [Fact]
    public async Task GetCategoriesAsync_ShouldReturnSystemAndUserCategories()
    {
        // Arrange
        string userId = "user1";
        
        // System category
        _context.Categories.Add(new Category { Name = "System Cat", UserId = null });
        // User category
        _context.Categories.Add(new Category { Name = "User Cat", UserId = userId });
        // Other user category
        _context.Categories.Add(new Category { Name = "Other Cat", UserId = "other" });
        await _context.SaveChangesAsync();

        // Act
        var categories = await _taskService.GetCategoriesAsync(userId);

        // Assert
        Assert.Equal(2, categories.Count); // System + User
        Assert.Contains(categories, c => c.Name == "System Cat");
        Assert.Contains(categories, c => c.Name == "User Cat");
        Assert.DoesNotContain(categories, c => c.Name == "Other Cat");
    }
}
