using DisciplineApp.Models;

namespace DisciplineApp.Services;

public class GuestFocusService
{
    public const string StorageKey = "guest_focus_v1";

    private readonly LocalStorageService _localStorage;

    public GuestFocusService(LocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public async Task<GuestFocusStore> LoadAsync()
    {
        var store = await _localStorage.GetItemAsync<GuestFocusStore>(StorageKey) ?? new GuestFocusStore();
        store.Sessions ??= new List<GuestFocusSession>();
        store.DailyGoalMinutes = GuestFocusLogic.ClampGoal(store.DailyGoalMinutes);
        return store;
    }

    public async Task<GuestFocusSession?> RecordAsync(double minutes, string? task, bool isPomodoro, DateTime? endedAt = null)
    {
        var store = await LoadAsync();
        var session = GuestFocusLogic.Record(store.Sessions, minutes, task, isPomodoro, DateTime.UtcNow, endedAt);
        if (session == null) return null;
        await SaveAsync(store);
        return session;
    }

    public async Task<int> GetGoalAsync()
        => (await LoadAsync()).DailyGoalMinutes;

    public async Task<int> SetGoalAsync(int minutes)
    {
        var store = await LoadAsync();
        store.DailyGoalMinutes = GuestFocusLogic.ClampGoal(minutes);
        await SaveAsync(store);
        return store.DailyGoalMinutes;
    }

    public async Task<double> GetTodayMinutesAsync()
    {
        var store = await LoadAsync();
        return GuestFocusLogic.TodayMinutes(store.Sessions, DateTime.UtcNow);
    }

    public async Task<List<FocusSession>> GetRecentAsync(int take = 20)
    {
        var store = await LoadAsync();
        return store.Sessions
            .OrderByDescending(s => s.EndTime)
            .Take(take)
            .Select(GuestFocusLogic.ToFocusSession)
            .ToList();
    }

    public async Task<List<GuestFocusSession>> GetAllAsync()
        => (await LoadAsync()).Sessions.ToList();

    public async Task<(double Total, double ThisWeek, double LastWeek)> GetTotalsAsync()
    {
        var store = await LoadAsync();
        var now = DateTime.UtcNow;
        return (
            store.Sessions.Sum(s => s.DurationMinutes),
            GuestFocusLogic.WeekMinutes(store.Sessions, now),
            GuestFocusLogic.WeekMinutes(store.Sessions, now, 1));
    }

    public async Task<Dictionary<string, double>> GetDailyActivityAsync(int days = 14)
    {
        var store = await LoadAsync();
        return GuestFocusLogic.DailyActivity(store.Sessions, DateTime.UtcNow, days);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var store = await LoadAsync();
        if (!GuestFocusLogic.Delete(store.Sessions, id)) return false;
        await SaveAsync(store);
        return true;
    }

    private Task SaveAsync(GuestFocusStore store)
        => _localStorage.SetItemAsync(StorageKey, store);
}
