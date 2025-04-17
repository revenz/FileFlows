using System.Text;
using FileFlows.Managers;
using FileFlows.Services.FileProcessing;
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
    /// <param name="mobile">if the client is a mobile device</param>
    [HttpGet]
    public async Task Get([FromQuery] bool initialData = false, [FromQuery] bool mobile = false)
    {
        var settings = await ServiceLoader.Load<ISettingsService>().Get();

        if (settings.InitialConfigDone == false)
        {
            Response.StatusCode = StatusCodes.Status451UnavailableForLegalReasons; // code browsers dont auto redirect on
            #if(DEBUG)
            Response.Headers.Location = "http://localhost:6868/initial-config"; // Set redirect URL
            #else
            Response.Headers.Location = "/initial-config"; // Set redirect URL
            #endif
            return;
        }

        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        var user = await HttpContext.GetLoggedInUser();

        var userRole = AuthenticationHelper.GetSecurityMode() == SecurityMode.Off
            ? UserRole.Admin
            : user?.Role ?? (UserRole)0;
        
        using var writer = new StreamWriter(Response.Body);
        
        var _broker = ServiceLoader.Load<SseEventBroker>();
        Guid? clientId = null;
        try
        {
            if (initialData)
            { 
                var data = await GetInitialData(HttpContext, userRole, mobile, settings);
                if (HttpContext.RequestAborted.IsCancellationRequested)
                    return; // in case it took too long and the request was closed while waiting

                data.PendingUpdate = _broker.PendingUpdate;
                var message = SseEventBroker.GetMessage("id", data);
                
                await writer.WriteLineAsync(message);
                await writer.FlushAsync();
                try
                {
                    await Task.Delay(5000, HttpContext.RequestAborted);
                }
                catch (TaskCanceledException)
                {
                    return; // Client disconnected before delay finished
                }
            }
            
            clientId = _broker.AddClient(writer, userRole, mobile);

            while (HttpContext.RequestAborted.IsCancellationRequested == false)
            {
                try
                {
                    await Task.Delay(5000, HttpContext.RequestAborted);
                }
                catch (TaskCanceledException)
                {
                    break; // Exit loop when client disconnects
                }
            }
        }
        catch (Exception ex)
        {
            // Client disconnected
            Logger.Instance.WLog($"SSE Controller Error: {ex}");
        }
        finally
        {
            if (clientId != null)
            {
                _broker.RemoveClient(clientId.Value); // <-- stop writing before disposing
                await writer.FlushAsync(); // optional safety flush
            }

            await writer.DisposeAsync(); // ensures writer is cleaned up
        }
    }

    /// <summary>
    /// Gets the initial data to send to the client
    /// </summary>
    private async Task<InitialClientData> GetInitialData(HttpContext context, UserRole userRole, bool mobile, Settings settings)
    {
        var logSummary = new StringBuilder();
        var swAll = new System.Diagnostics.Stopwatch();
        swAll.Start();
        var stopwatch = new System.Diagnostics.Stopwatch();

        // Prepare all tasks
        var flowsTask = ServiceLoader.Load<FlowService>().GetFlowsForBroker();
        var nodeStatusesTask = ServiceLoader.Load<NodeService>().GetStatusSummaries();
        var tagsTask = ServiceLoader.Load<TagService>().GetAllAsync();
        var flowElementsTask = ServiceLoader.Load<FlowElementService>().GetFlowElements(Guid.Empty, null);
        var profileTask = context.GetProfile();
        var variablesTask = ServiceLoader.Load<VariableService>().GetAllAsync();
        var pluginsTask = ServiceLoader.Load<PluginService>().GetForBroadcast();
        var scheduledReportsTask = ServiceLoader.Load<ScheduledReportService>().GetAll();
        var notificationsTask = ((NotificationService)ServiceLoader.Load<INotificationService>()).GetAll();

        var allTasks = new List<Task>
        {
            flowsTask,
            nodeStatusesTask,
            tagsTask,
            profileTask,
            flowElementsTask,
            variablesTask,
            pluginsTask,
            scheduledReportsTask,
            notificationsTask
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

        var fileSorter = ServiceLoader.Load<FileSorterService>();
            
        var successful = fileSorter.GetData(FileStatus.Processed);
        var successfulTotal = fileSorter.GetTotal(FileStatus.Processed);
        if (mobile ||(userRole & UserRole.Files) != UserRole.Files && successful.Count > 50)
            successful = successful.Take(50).ToList();
        
        var failed = fileSorter.GetData(FileStatus.ProcessingFailed);
        var failedTotal = fileSorter.GetTotal(FileStatus.ProcessingFailed);
        if (mobile || (userRole & UserRole.Files) != UserRole.Files && failed.Count > 50)
            failed = failed.Take(50).ToList();

        var upcoming = fileSorter.GetData(FileStatus.Unprocessed);
        if (mobile ||(userRole & UserRole.Files) != UserRole.Files && upcoming.Count > 50)
            upcoming = upcoming.Take(50).ToList();
        
        logSummary.AppendLine(
            $"Completed SystemOverviewService calls at {DateTime.UtcNow}, Elapsed: {stopwatch.ElapsedMilliseconds} ms");

        stopwatch.Restart();
        var savingsService = ServiceLoader.Load<SavingsService>();
        var savings31 = savingsService.GetLast31DaysData();
        var savingsAll = savingsService.GetOverallData();
        logSummary.AppendLine(
            $"Completed SavingsService calls at {DateTime.UtcNow}, Elapsed: {stopwatch.ElapsedMilliseconds} ms");

        var lfStatuses  = fileSorter.GetStatuses();
        
        var result = new InitialClientData
        {
            Profile = profile,
            PageSize = settings.MaxPageSize,
            LanguageJson = lang,
            CurrentSystemInfo = ServiceLoader.Load<SystemOverviewService>().GetSystemInfo(),
            CurrentFileOverData = ServiceLoader.Load<DashboardFileOverviewService>().GetData(),
            CurrentUpdatesInfo = ServiceLoader.Load<UpdateService>().Info,
            Flows = flowsTask.Result.OrderBy(x => x.Name.ToLowerInvariant()).ToList(),
            Plugins = pluginsTask.Result.OrderBy(x => x.Name.ToLowerInvariant()).ToList(),
            Libraries = ServiceLoader.Load<LibraryService>().GetListModels(),
            Processing = fileSorter.GetProcessing(),
            NodeStatusSummaries = nodeStatusesTask.Result,
            StorageSavedTotalData = storageSavedTotal,
            StorageSavedMonthData = storageSavedMonth,
            FailedFiles = failed.Select(x => (LibraryFileMinimal)x).ToList(),
            FailedFilesTotal = failedTotal,
            Successful = successful.Select(x => (LibraryFileMinimal)x).ToList(),
            SuccessfulTotal = successfulTotal,
            FileQueue = upcoming.Select(x => (LibraryFileMinimal)x).ToList(),
            TopSavingsAll = savingsAll,
            TopSavings31Days = savings31,
            LibraryFileCounts = lfStatuses,
            Notifications = notificationsTask.Result,
            FlowElements = flowElementsTask.Result.ToList(),
            Tags = (tagsTask.Result ?? Enumerable.Empty<Tag>())
                .OrderBy(x => x.Name.ToLowerInvariant())
                .ToList()
        };
        
        if ((userRole & UserRole.Variables) == UserRole.Variables)
            result.Variables = variablesTask.Result?.OrderBy(x => x.Name.ToLowerInvariant())?.ToList() ?? [];
        if ((userRole & UserRole.DockerMods) == UserRole.DockerMods)
            result.DockerMods = ServiceLoader.Load<DockerModService>().GetForBroadcast();
        if ((userRole & UserRole.Scripts) == UserRole.Scripts)
            result.Scripts = ServiceLoader.Load<ScriptService>().GetForBroadcast();
        if (LicenseService.IsLicensed(LicenseFlags.Reporting) && (userRole & UserRole.Reports) == UserRole.Reports)
        {
            result.ScheduledReports = scheduledReportsTask.Result.OrderBy(x => x.Name.ToLowerInvariant()).ToList();
            result.ReportDefinitions = new ReportManager().GetReports();
        }

        if ((userRole & UserRole.Files) == UserRole.Files)
        {
            result.OnHold = fileSorter.GetData(FileStatus.OnHold).Select(x => (LibraryFileMinimal)x)
                .ToList();
            result.DisabledFiles = fileSorter.GetData(FileStatus.Disabled).Select(x => (LibraryFileMinimal)x)
                .ToList();
            result.OutOfScheduleFiles = fileSorter.GetData(FileStatus.OutOfSchedule).Select(x => (LibraryFileMinimal)x)
                .ToList();
        }

        // Log summary to Logger.Instance
        swAll.Stop();
        Logger.Instance.ILog($"Task Execution Summary:\nTotal Time: {swAll.Elapsed}\n{logSummary}");

        return result;
    }
}