using DisciplineApp.Models;

namespace DisciplineApp.Services;

public class GuestIfThenService
{
    private const string StorageKey = "guest_ifthen_v1";
    private readonly LocalStorageService _localStorage;

    public GuestIfThenService(LocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public async Task<List<IfThenPlan>> GetActiveAsync()
    {
        var stored = await LoadAsync();
        return stored.Where(p => p.IsActive).OrderBy(p => p.CreatedAt).Take(InputGuard.MaxIfThenPlans).ToList();
    }

    public async Task<List<IfThenPlan>> GetAllAsync()
    {
        return await LoadAsync();
    }

    public async Task<(bool ok, string reason)> AddAsync(string ifCue, string thenAction)
    {
        var cue = InputGuard.Clamp(ifCue, InputGuard.TitleMax);
        var action = InputGuard.Clamp(thenAction, InputGuard.TitleMax);
        if (string.IsNullOrWhiteSpace(cue) || string.IsNullOrWhiteSpace(action))
        {
            return (false, "empty");
        }

        var stored = await LoadAsync();
        if (stored.Count(p => p.IsActive) >= InputGuard.MaxIfThenPlans)
        {
            return (false, "limit");
        }

        stored.Add(new IfThenPlan
        {
            Id = stored.Count == 0 ? 1 : stored.Max(p => p.Id) + 1,
            UserId = "guest",
            IfCue = cue,
            ThenAction = action,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _localStorage.SetItemAsync(StorageKey, stored);
        return (true, "ok");
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var stored = await LoadAsync();
        var plan = stored.FirstOrDefault(p => p.Id == id);
        if (plan == null) return false;
        stored.Remove(plan);
        await _localStorage.SetItemAsync(StorageKey, stored);
        return true;
    }

    private async Task<List<IfThenPlan>> LoadAsync()
        => await _localStorage.GetItemAsync<List<IfThenPlan>>(StorageKey) ?? new List<IfThenPlan>();
}
