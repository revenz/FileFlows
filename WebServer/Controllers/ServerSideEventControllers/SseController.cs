using System.Text;
using FileFlows.Services.FileProcessing;
using FileFlows.Services.SystemOverview;
using FileFlows.WebServer.Helpers;

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
        
        var user = await HttpContext.GetLoggedInUser();


        var userRole = AuthenticationHelper.GetSecurityMode() == SecurityMode.Off
            ? UserRole.Admin
            : user?.Role ?? (UserRole)0;
        
        using var writer = new StreamWriter(Response.Body);
        var _broker = ServiceLoader.Load<SseEventBroker>();
        var clientId = _broker.AddClient(writer, userRole);
        
        try
        {
            if (initialData)
            { 
                var data = await GetInitialData(HttpContext, userRole);
                if (HttpContext.RequestAborted.IsCancellationRequested)
                    return; // in case it took too long and the request was closed while waiting

                var message = SseEventBroker.GetMessage("id", data);
                
                await writer.WriteLineAsync(message);
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
    private async Task<InitialClientData> GetInitialData(HttpContext context, UserRole userRole)
    {
        var logSummary = new StringBuilder();
        var swAll = new System.Diagnostics.Stopwatch();
        swAll.Start();
        var stopwatch = new System.Diagnostics.Stopwatch();

        // Prepare all tasks
        var flowsTask = ServiceLoader.Load<FlowService>().GetFlowsForBroker();
        var executorsTask = ServiceLoader.Load<FlowRunnerService>().GetExecutors();
        var nodeStatusesTask = ServiceLoader.Load<NodeService>().GetStatusSummaries();
        var tagsTask = ServiceLoader.Load<TagService>().GetAllAsync();
        var flowElementsTask = FlowController.GetFlowElements(Guid.Empty, null);
        var profileTask = context.GetProfile();
        var variablesTask = ServiceLoader.Load<VariableService>().GetAllAsync();
        var pluginsTask = ServiceLoader.Load<PluginService>().GetForBroadcast();

        var allTasks = new List<Task>
        {
            flowsTask,
            executorsTask,
            nodeStatusesTask,
            tagsTask,
            profileTask,
            flowElementsTask,
            variablesTask,
            pluginsTask
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

        var lfStatuses  = ServiceLoader.Load<LibraryFileStatusOverviewService>().GetStatuses();

        var result = new InitialClientData
        {
            Profile = profile,
            LanguageJson = lang,
            CurrentSystemInfo = ServiceLoader.Load<SystemOverviewService>().GetSystemInfo(),
            CurrentFileOverData = ServiceLoader.Load<DashboardFileOverviewService>().GetData(),
            CurrentUpdatesInfo = ServiceLoader.Load<UpdateService>().Info,
            Flows = flowsTask.Result,
            Plugins = pluginsTask.Result,
            Libraries = ServiceLoader.Load<LibraryService>().GetListModels(),
            CurrentExecutorInfoMinified = executorsTask.Result.Values.ToList(),
            NodeStatusSummaries = nodeStatusesTask.Result,
            StorageSavedTotalData = storageSavedTotal,
            StorageSavedMonthData = storageSavedMonth,
            FailedFiles = failed.Select(x => (LibraryFileMinimal)x).ToList(),
            RecentlyFinished = successful.Select(x => (LibraryFileMinimal)x).ToList(),
            UpcomingFiles = upcoming.Select(x => (LibraryFileMinimal)x).ToList(),
            TopSavingsAll = savingsAll,
            TopSavings31Days = savings31,
            LibraryFileCounts = lfStatuses,
            FlowElements = flowElementsTask.Result.ToList(),
            Tags = (tagsTask.Result ?? Enumerable.Empty<Tag>())
                .OrderBy(x => x.Name.ToLowerInvariant())
                .ToList()
        };
        
        if ((userRole & UserRole.Variables) == UserRole.Variables)
            result.Variables = variablesTask.Result ?? [];
        if ((userRole & UserRole.DockerMods) == UserRole.DockerMods)
            result.DockerMods = ServiceLoader.Load<DockerModService>().GetForBroadcast();
        if ((userRole & UserRole.Scripts) == UserRole.Scripts)
            result.Scripts = ServiceLoader.Load<ScriptService>().GetForBroadcast();

        // Log summary to Logger.Instance
        swAll.Stop();
        Logger.Instance.ILog($"Task Execution Summary:\nTotal Time: {swAll.Elapsed}\n{logSummary}");

        return result;
    }
}