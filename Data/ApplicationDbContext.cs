using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using DisciplineApp.Models;

namespace DisciplineApp.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<UserTask> UserTasks { get; set; }
    public DbSet<PomodoroSession> PomodoroSessions { get; set; }
    public DbSet<FocusSession> FocusSessions { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<AnalyticsEvent> AnalyticsEvents { get; set; }
    public DbSet<Challenge> Challenges { get; set; }
    public DbSet<Habit> Habits { get; set; }
    public DbSet<HabitLog> HabitLogs { get; set; }
    public DbSet<DailyReflection> DailyReflections { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Work", ColorCode = "#3B82F6" }, // Blue
            new Category { Id = 2, Name = "Personal", ColorCode = "#10B981" }, // Green
            new Category { Id = 3, Name = "Health", ColorCode = "#EF4444" }, // Red
            new Category { Id = 4, Name = "Learning", ColorCode = "#F59E0B" } // Amber
        );

        builder.Entity<HabitLog>()
            .HasIndex(l => new { l.HabitId, l.Date })
            .IsUnique();

        builder.Entity<DailyReflection>()
            .HasIndex(r => new { r.UserId, r.Date })
            .IsUnique();

        builder.Entity<Habit>()
            .HasMany(h => h.Logs)
            .WithOne(l => l.Habit)
            .HasForeignKey(l => l.HabitId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
