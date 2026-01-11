using System.Threading.Tasks;

namespace DisciplineApp.Services.Interfaces;

public interface IAnalyticsService
{
    Task TrackEventAsync(string eventName, string? category = null, string? data = null);
}
