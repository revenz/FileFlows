using System.Text;
using FileFlows.Services.SystemOverview;

namespace FileFlows.WebServer.Controllers.ServerSideEventControllers;

/// <summary>
/// Controller for Server Side Events
/// </summary>
[Route("/api/sse")]
[ApiExplorerSettings(IgnoreApi = true)]
[FileFlowsAuthorize]
public class SseController : Controller
{ 
    /// <summary>
    /// Gets a SSE connection
    /// </summary>
    /// <param name="initialData">if the initial data should be sent</param>
    [HttpGet]
    public async Task Get([FromQuery] bool initialData = false)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        using var writer = new StreamWriter(Response.Body);
        var _broker = ServiceLoader.Load<SseEventBroker>();
        var clientId = _broker.AddClient(writer);
        
        try
        {
            if (initialData)
            { 
                var data = await GetInitialData(HttpContext);
                if (HttpContext.RequestAborted.IsCancellationRequested)
                    return; // in case it took too long and the request was closed while waiting
                
                await writer.WriteLineAsync("data: id:" + JsonSerializer.Serialize(data) + "\n");
                await writer.FlushAsync();
                await Task.Delay(5000, HttpContext.RequestAborted);
            }

            while (HttpContext.RequestAborted.IsCancellationRequested == false)
            {
                await writer.WriteLineAsync(":heartbeat\n");
                await writer.FlushAsync();
                await Task.Delay(5000, HttpContext.RequestAborted);
            }
        }
        catch
        {
            // Client disconnected
        }
        finally
        {
            _broker.RemoveClient(clientId);
        }
    }

    /// <summary>
    /// Gets the initial data to send to the client
    /// </summary>
    private async Task<InitialClientData> GetInitialData(HttpContext context)
    {
        var logSummary = new StringBuilder();
        var stopwatch = new System.Diagnostics.Stopwatch();

        // Prepare all tasks
        var flowsTask = ServiceLoader.Load<FlowService>().GetAllAsync();
        var librariesTask = ServiceLoader.Load<LibraryService>().GetAllAsync();
        var executorsTask = ServiceLoader.Load<FlowRunnerService>().GetExecutors();
        var nodeStatusesTask = ServiceLoader.Load<NodeService>().GetStatusSummaries();
        var tagsTask = ServiceLoader.Load<TagService>().GetAllAsync();
        var profileTask = context.GetProfile();

        var allTasks = new List<Task>
        {
            flowsTask,
            librariesTask,
            executorsTask,
            nodeStatusesTask,
            tagsTask,
            profileTask
        };

        // Log the start time for all tasks
        foreach (var task in allTasks)
        {
            logSummary.AppendLine($"Started {task.GetType().Name} at {DateTime.UtcNow}");
        }

        var completedTasks = new List<Task>();

        // Process tasks as they complete
        while (completedTasks.Count < allTasks.Count)
        {
            // Wait for any task to complete
            var completedTask = await Task.WhenAny(allTasks);

            // Measure the time taken for this completed task
            stopwatch.Restart();
            await completedTask; // Ensure the task is finished and results are available

            logSummary.AppendLine(
                $"{completedTask.GetType().Name} completed at {DateTime.UtcNow}, Elapsed: {stopwatch.ElapsedMilliseconds} ms");

            completedTasks.Add(completedTask); // Track that this task is completed
        }

        // After all tasks have completed, now perform the remaining logic
        var profile = profileTask.Result;
        if (profile == null)
            throw new Exception("User is not logged in");

        stopwatch.Restart();
        var lang = await ServiceLoader.Load<LanguageService>().GetLanguageJson(profile.Language);
        logSummary.AppendLine(
            $"Completed GetLanguageJson (LanguageService) at {DateTime.UtcNow}, Elapsed: {stopwatch.ElapsedMilliseconds} ms");

        stopwatch.Restart();
        var storageSavedMonth = new StatisticService().GetStorageSaved(Globals.STAT_STORAGE_SAVED_MONTH);
        var storageSavedTotal = new StatisticService().GetStorageSaved(Globals.STAT_STORAGE_SAVED);
        logSummary.AppendLine(
            $"Completed GetStorageSaved (StatisticService) at {DateTime.UtcNow}, Elapsed: {stopwatch.ElapsedMilliseconds} ms");

        stopwatch.Restart();
        var service = ServiceLoader.Load<SystemOverviewService>();
        var failed = service.GetFailedFiles();
        var successful = service.GetRecentlyFinishedFiles();
        var upcoming = service.GetUpcomingFiles();
        logSummary.AppendLine(
            $"Completed SystemOverviewService calls at {DateTime.UtcNow}, Elapsed: {stopwatch.ElapsedMilliseconds} ms");

        stopwatch.Restart();
        var savingsService = ServiceLoader.Load<SavingsService>();
        var savings31 = savingsService.GetLast31DaysData();
        var savingsAll = savingsService.GetOverallData();
        logSummary.AppendLine(
            $"Completed SavingsService calls at {DateTime.UtcNow}, Elapsed: {stopwatch.ElapsedMilliseconds} ms");

        var result = new InitialClientData
        {
            Profile = profile,
            LanguageJson = lang,
            CurrentSystemInfo = ServiceLoader.Load<SystemOverviewService>().GetSystemInfo(),
            CurrentFileOverData = ServiceLoader.Load<DashboardFileOverviewService>().GetData(),
            CurrentUpdatesInfo = ServiceLoader.Load<UpdateService>().Info,
            FlowList = flowsTask.Result.ToDictionary(x => x.Uid, x => x.Name),
            LibraryList = librariesTask.Result.ToDictionary(x => x.Uid, x => x.Name),
            CurrentExecutorInfoMinified = executorsTask.Result.Values.ToList(),
            NodeStatusSummaries = nodeStatusesTask.Result,
            StorageSavedTotalData = storageSavedTotal,
            StorageSavedMonthData = storageSavedMonth,
            FailedFiles = failed.Select(x => (LibraryFileMinimal)x).ToList(),
            RecentlyFinished = successful.Select(x => (LibraryFileMinimal)x).ToList(),
            UpcomingFiles = upcoming.Select(x => (LibraryFileMinimal)x).ToList(),
            TopSavingsAll = savings31,
            TopSavings31Days = savings31,
            Tags = (tagsTask.Result ?? Enumerable.Empty<Tag>())
                .OrderBy(x => x.Name.ToLowerInvariant())
                .ToList()
        };

        // Log summary to Logger.Instance
        Logger.Instance.ILog($"Task Execution Summary:\n{logSummary.ToString()}");

        return result;
    }
}