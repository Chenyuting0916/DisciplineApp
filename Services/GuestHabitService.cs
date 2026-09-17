using DisciplineApp.Models;
using Microsoft.Extensions.Localization;

namespace DisciplineApp.Services;

public class GuestHabitService
{
    private const string StorageKey = "guest_habits_v1";
    private readonly LocalStorageService _localStorage;
    private readonly IStringLocalizer<App> _localizer;

    public GuestHabitService(LocalStorageService localStorage, IStringLocalizer<App> localizer)
    {
        _localStorage = localStorage;
        _localizer = localizer;
    }

    public async Task<List<Habit>> GetHabitsAsync()
    {
        var stored = await LoadAsync();
        return stored.Where(h => !h.IsArchived).Select(ToHabit).ToList();
    }

    public async Task<List<Habit>> GetAllIncludingArchivedAsync()
    {
        var stored = await LoadAsync();
        return stored.Select(ToHabit).ToList();
    }

    public async Task<Habit?> AddHabitAsync(string title, string icon, string color, int targetDaysPerWeek = 7)
    {
        var stored = await LoadAsync();
        if (stored.Count(h => !h.IsArchived) >= InputGuard.MaxHabits)
        {
            return null;
        }

        var nextId = stored.Count == 0 ? 1 : stored.Max(h => h.Id) + 1;
        var item = new StoredHabit
        {
            Id = nextId,
            Title = InputGuard.Clamp(title, InputGuard.TitleMax),
            Icon = string.IsNullOrWhiteSpace(icon) ? "✅" : InputGuard.Clamp(icon, 8),
            Color = InputGuard.IsSafeHexColor(color) ? color : "#F5A524",
            TargetDaysPerWeek = Math.Clamp(targetDaysPerWeek, 1, 7),
            CreatedAt = DateTime.UtcNow
        };
        if (string.IsNullOrWhiteSpace(item.Title)) return null;

        stored.Add(item);
        await SaveAsync(stored);
        return ToHabit(item);
    }

    public async Task<bool> ToggleTodayAsync(int habitId)
    {
        var stored = await LoadAsync();
        var habit = stored.FirstOrDefault(h => h.Id == habitId && !h.IsArchived);
        if (habit == null) return false;

        var today = DateTime.UtcNow.Date;
        if (habit.Logs.Any(d => d.Date == today))
        {
            habit.Logs.RemoveAll(d => d.Date == today);
            await SaveAsync(stored);
            return false;
        }

        habit.Logs.Add(today);
        await SaveAsync(stored);
        return true;
    }

    public async Task<bool> ArchiveAsync(int habitId)
    {
        var stored = await LoadAsync();
        var habit = stored.FirstOrDefault(h => h.Id == habitId);
        if (habit == null) return false;
        habit.IsArchived = true;
        await SaveAsync(stored);
        return true;
    }

    private async Task<List<StoredHabit>> LoadAsync()
    {
        var stored = await _localStorage.GetItemAsync<List<StoredHabit>>(StorageKey);
        if (stored != null && stored.Any())
        {
            return stored;
        }

        stored = CreateDefaults();
        await SaveAsync(stored);
        return stored;
    }

    private Task SaveAsync(List<StoredHabit> stored)
        => _localStorage.SetItemAsync(StorageKey, stored);

    private List<StoredHabit> CreateDefaults()
    {
        return new List<StoredHabit>
        {
            new()
            {
                Id = 1,
                Title = _localizer["HabitWater"],
                Icon = "💧",
                Color = "#38bdf8",
                TargetDaysPerWeek = 7,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = 2,
                Title = _localizer["HabitRead"],
                Icon = "📚",
                Color = "#a78bfa",
                TargetDaysPerWeek = 5,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = 3,
                Title = _localizer["HabitSleep"],
                Icon = "😴",
                Color = "#818cf8",
                TargetDaysPerWeek = 7,
                CreatedAt = DateTime.UtcNow
            }
        };
    }

    private static Habit ToHabit(StoredHabit item)
    {
        return new Habit
        {
            Id = item.Id,
            UserId = "guest",
            Title = item.Title,
            Icon = item.Icon,
            Color = item.Color,
            TargetDaysPerWeek = item.TargetDaysPerWeek,
            CreatedAt = item.CreatedAt,
            IsArchived = item.IsArchived,
            Logs = item.Logs.Select(d => new HabitLog { HabitId = item.Id, Date = d.Date }).ToList()
        };
    }

    private class StoredHabit
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Icon { get; set; } = "✅";
        public string Color { get; set; } = "#F5A524";
        public int TargetDaysPerWeek { get; set; } = 7;
        public bool IsArchived { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public List<DateTime> Logs { get; set; } = new();
    }
}
