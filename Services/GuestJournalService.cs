using DisciplineApp.Models;

namespace DisciplineApp.Services;

public class GuestJournalService
{
    private const string StorageKey = "guest_journal_v1";
    private readonly LocalStorageService _localStorage;

    public GuestJournalService(LocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public async Task<List<GuestDayJournal>> GetAllAsync()
        => await _localStorage.GetItemAsync<List<GuestDayJournal>>(StorageKey) ?? new List<GuestDayJournal>();

    public async Task<GuestDayJournal?> GetTodayAsync()
    {
        var days = await GetAllAsync();
        return GuestJournalLogic.FindToday(days, DateTime.UtcNow);
    }

    public async Task<GuestDayJournal> SaveVowAsync(string vow, string oneThing)
    {
        var days = await GetAllAsync();
        var today = GuestJournalLogic.FindToday(days, DateTime.UtcNow);
        var saved = GuestJournalLogic.Upsert(
            days,
            DateTime.UtcNow,
            vow,
            oneThing,
            today?.Mood ?? 3,
            today?.Note);
        await _localStorage.SetItemAsync(StorageKey, days);
        return saved;
    }

    public async Task<GuestDayJournal> SaveMoodAsync(int mood, string? note)
    {
        var days = await GetAllAsync();
        var today = GuestJournalLogic.FindToday(days, DateTime.UtcNow);
        var saved = GuestJournalLogic.Upsert(
            days,
            DateTime.UtcNow,
            today?.Vow,
            today?.OneThing,
            mood,
            note ?? "");
        await _localStorage.SetItemAsync(StorageKey, days);
        return saved;
    }
}
