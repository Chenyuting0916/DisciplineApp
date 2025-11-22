using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;

namespace DisciplineApp.Services;

public class CalendarService
{
    public async Task<Events?> GetUpcomingEventsAsync(string accessToken)
    {
        if (string.IsNullOrEmpty(accessToken)) return null;

        var credential = GoogleCredential.FromAccessToken(accessToken);
        var service = new Google.Apis.Calendar.v3.CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "DisciplineApp"
        });

        var request = service.Events.List("primary");
        request.TimeMin = DateTime.UtcNow;
        request.ShowDeleted = false;
        request.SingleEvents = true;
        request.MaxResults = 10;
        request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

        return await request.ExecuteAsync();
    }
}
