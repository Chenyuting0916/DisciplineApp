using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;

namespace DisciplineApp.Services;

public class CalendarService
{
    public Google.Apis.Calendar.v3.CalendarService? Service { get; private set; }

    public async Task<Events?> GetUpcomingEventsAsync(string accessToken)
    {
        if (string.IsNullOrEmpty(accessToken)) return null;

        var credential = GoogleCredential.FromAccessToken(accessToken);
        Service = new Google.Apis.Calendar.v3.CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "DisciplineApp"
        });

        var request = Service.Events.List("primary");
        request.TimeMin = DateTime.UtcNow;
        request.ShowDeleted = false;
        request.SingleEvents = true;
        request.MaxResults = 10;
        request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

        return await request.ExecuteAsync();
    }
}
