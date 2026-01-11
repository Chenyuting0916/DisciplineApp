using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Tasks.v1;
using Google.Apis.Tasks.v1.Data;
using Google.Apis.Services;

namespace DisciplineApp.Services
{
    public class CalendarService
    {
        private readonly TokenProvider _tokenProvider;

        public CalendarService(TokenProvider tokenProvider)
        {
            _tokenProvider = tokenProvider;
        }

        public async Task<IList<Event>> GetEventsAsync(DateTime startDate, DateTime endDate)
        {
            var accessToken = _tokenProvider.AccessToken;
            if (string.IsNullOrEmpty(accessToken))
            {
                return new List<Event>();
            }

            var credential = GoogleCredential.FromAccessToken(accessToken);
            var service = new Google.Apis.Calendar.v3.CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "DisciplineApp"
            });

            var request = service.Events.List("primary");
            request.TimeMinDateTimeOffset = new DateTimeOffset(startDate);
            request.TimeMaxDateTimeOffset = new DateTimeOffset(endDate);
            request.SingleEvents = true;
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

            var events = await request.ExecuteAsync();
            return events.Items ?? new List<Event>();
        }

        public async Task<IList<Google.Apis.Tasks.v1.Data.Task>> GetTasksAsync()
        {
            var accessToken = _tokenProvider.AccessToken;
            if (string.IsNullOrEmpty(accessToken))
            {
                Console.WriteLine("GetTasksAsync: No access token.");
                return new List<Google.Apis.Tasks.v1.Data.Task>();
            }

            var credential = GoogleCredential.FromAccessToken(accessToken);
            var service = new TasksService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "DisciplineApp"
            });

            try
            {
                // Get all task lists
                var taskListsRequest = service.Tasklists.List();
                var taskLists = await taskListsRequest.ExecuteAsync();

                var allTasks = new List<Google.Apis.Tasks.v1.Data.Task>();

                if (taskLists.Items != null)
                {
                    foreach (var taskList in taskLists.Items)
                    {
                        var tasksRequest = service.Tasks.List(taskList.Id);
                        tasksRequest.ShowCompleted = false; // Only show incomplete tasks
                        // tasksRequest.DueMin = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fffK"); // Optional: Filter by due date
                        
                        try 
                        {
                            var tasks = await tasksRequest.ExecuteAsync();
                            if (tasks.Items != null)
                            {
                                allTasks.AddRange(tasks.Items);
                            }
                        }
                        catch (Exception ex)
                        {
                             Console.WriteLine($"Error fetching tasks for list {taskList.Title}: {ex.Message}");
                        }
                    }
                }

                return allTasks;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching Google Tasks: {ex.Message}");
                // If we get a 403, it might be because the scope is missing.
                if (ex.Message.Contains("403") || ex.Message.Contains("insufficient"))
                {
                     Console.WriteLine("Likely missing 'tasks.readonly' scope. Please re-login.");
                }
                return new List<Google.Apis.Tasks.v1.Data.Task>();
            }
        }
    }
}
