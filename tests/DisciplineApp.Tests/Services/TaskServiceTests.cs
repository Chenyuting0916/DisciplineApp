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
            .ReturnsAsync((true, 50, 450)); 

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
}
