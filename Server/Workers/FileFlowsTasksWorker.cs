using FileFlows.Interfaces;
using FileFlows.Server.Helpers;
using FileFlows.Services;
using FileFlows.ServerShared.Models;
using FileFlows.Shared.Models;
using Logger = FileFlows.Shared.Logger;

namespace FileFlows.Server.Workers;

/// <summary>
/// A worker that runs FileFlows Tasks
/// </summary>
public class FileFlowsTasksWorker: ServerWorker, ITaskService
{
    /// <summary>
    /// Gets the instance of the tasks worker
    /// </summary>
    internal static FileFlowsTasksWorker Instance { get;private set; } = null!;

    /// <summary>
    /// A list of tasks and the quarter they last ran in
    /// </summary>
    private Dictionary<Guid, int> TaskLastRun = new ();

    /// <summary>
    /// The logger used for tasks
    /// </summary>
    private Logger Logger;
    
    /// <summary>
    /// Creates a new instance of the Scheduled Task Worker
    /// </summary>
    public FileFlowsTasksWorker() : base(ScheduleType.Minute, 1, quiet: true)
    {
        ServiceLoader.AddSpecialCase<ITaskService>(this);
        Instance = this;
        Logger = new Logger();
        Logger.RegisterWriter(new FileLogger(DirectoryHelper.LoggingDirectory, "FileFlowsTasks", false));

        var service = ServiceLoader.Load<ISystemEventsService>();
        service.OnLibraryFileAdd += SystemEventsOnOnLibraryFileAdd;
        service.OnLibraryFileProcessed += SystemEventsOnOnLibraryFileProcessed;
        service.OnLibraryFileProcessedFailed += SystemEventsOnOnLibraryFileProcessedFailed;
        service.OnLibraryFileProcessedSuceess += SystemEventsOnOnLibraryFileProcessedSuceess;
        service.OnLibraryFileProcessingStarted += SystemEventsOnOnLibraryFileProcessingStarted;
        service.OnServerUpdating += SystemEventsOnOnServerUpdating;
        service.OnServerUpdateAvailable += SystemEventsOnOnServerUpdateAvailable;
    }
    
    /// <summary>
    /// Gets the variables in a dictionary
    /// </summary>
    /// <returns>a dictionary of variables</returns>
    public static Dictionary<string, object> GetVariables()
    {
        var list = ServiceLoader.Load<VariableService>().GetAllAsync().Result ?? new ();
        var dict = new Dictionary<string, object>();
        foreach (var var in list)
        {
            dict.Add(var.Name, var.Value);
        }
        
        dict.TryAdd("FileFlows.Url", Globals.ServerUrl);
        dict["FileFlows.AccessToken"] = ServiceLoader.Load<ISettingsService>().Get()?.Result?.AccessToken;
        return dict;
    }

    /// <summary>
    /// Executes any tasks
    /// </summary>
    protected override void ExecuteActual(Settings settings)
    {
        if (LicenseService.IsLicensed(LicenseFlags.Tasks) == false)
            return;
        
        int quarter = TimeHelper.GetCurrentQuarter();
        var tasks = ServiceLoader.Load<TaskService>().GetAllAsync().Result;
        // 0, 1, 2, 3, 4
        foreach (var task in tasks)
        {
            if (task.Enabled == false)
                continue;
            
            if (task.Type != TaskType.Schedule)
                continue;
            if (task.Schedule[quarter] != '1')
                continue;
            if (TaskLastRun.ContainsKey(task.Uid) && TaskLastRun[task.Uid] == quarter)
                continue;
            _ = RunTask(task);
            TaskLastRun[task.Uid] = quarter;
        }
    }

    /// <summary>
    /// Runs a task by its UID
    /// </summary>
    /// <param name="uid">The UID of the task to run</param>
    /// <returns>the result of the executed task</returns>
    public async Task<FileFlowsTaskRun> RunByUid(Guid uid)
    {
        if (LicenseService.IsLicensed(LicenseFlags.Tasks) == false) 
            return new() { Success = false, Log = "Not licensed" };
        var task = await ServiceLoader.Load<TaskService>().GetByUidAsync(uid);
        if (task == null)
            return new() { Success = false, Log = "Task not found" };
        return await RunTask(task);
    } 

    /// <summary>
    /// Runs a task
    /// </summary>
    /// <param name="task">the task to run</param>
    /// <param name="additionalVariables">any additional variables</param>
    private async Task<FileFlowsTaskRun> RunTask(FileFlowsTask task, Dictionary<string, object>? additionalVariables = null)
    {
        var code = (await ServiceLoader.Load<ScriptService>().Get(task.Script))?.Code;
        if (string.IsNullOrWhiteSpace(code))
        {
            var msg = $"No code found for Task '{task.Name}' using script: {task.Script}";
            Logger.WLog(msg);
            return new() { Success = false, Log = msg };
        }
        Logger.ILog("Executing task: " + task.Name);
        DateTime dtStart = DateTime.UtcNow;

        var variables = GetVariables();
        if (additionalVariables?.Any() == true)
        {
            foreach (var variable in additionalVariables)
            {
                variables[variable.Key] = variable.Value;
            }
        }

        var scriptService = ServiceLoader.Load<ScriptService>();
        string sharedDirectory = await scriptService.GetSharedDirectory();

        var result = ScriptExecutor.Execute(code, variables, sharedDirectory: sharedDirectory, dontLogCode: true);
        if (result.Success)
        {
            Logger.ILog($"Task '{task.Name}' completed in: " + (DateTime.UtcNow.Subtract(dtStart)) + "\n" +
                                 result.Log);

            _ = ServiceLoader.Load<INotificationService>().Record(NotificationSeverity.Information,
                $"Task executed successfully '{task.Name}'");
        }
        else
        {
            Logger.ELog($"Error executing task '{task.Name}': " + result.ReturnValue + "\n" + result.Log);
            
            _ = ServiceLoader.Load<INotificationService>().Record(NotificationSeverity.Warning,
                $"Error executing task '{task.Name}': " + result.ReturnValue, result.Log);
        }

        task.LastRun = DateTime.UtcNow;
        task.RunHistory ??= new Queue<FileFlowsTaskRun>(10);
        lock (task.RunHistory)
        {
            task.RunHistory.Enqueue(result);
            while (task.RunHistory.Count > 10 && task.RunHistory.TryDequeue(out _));
        }

        await ServiceLoader.Load<TaskService>().Update(task, auditDetails: AuditDetails.ForServer());
        return result;
    }
    
    /// <summary>
    /// Triggers all tasks of a certain type to run
    /// </summary>
    /// <param name="type">the type of task</param>
    /// <param name="variables">the variables to pass into the task</param>
    private void TriggerTaskType(TaskType type, Dictionary<string, object> variables)
    {
        if (LicenseService.IsLicensed(LicenseFlags.Tasks) == false)
            return;
        var tasks = ServiceLoader.Load<TaskService>().GetAllAsync().Result.Where(x => x.Type == type && x.Enabled).ToArray();
        foreach (var task in tasks)
        {
            _ = RunTask(task, variables);
        }
    }

    private void UpdateEventTriggered(TaskType type, UpdateEventArgs args)
    {
        if (LicenseService.IsLicensed(LicenseFlags.Tasks) == false)
            return;
        TriggerTaskType(type, new Dictionary<string, object>
        {
            { nameof(args.Version), args.Version },
            { nameof(args.CurrentVersion), args.CurrentVersion },
        });
    }

    private void SystemEventsOnOnServerUpdateAvailable(UpdateEventArgs args)
        => UpdateEventTriggered(TaskType.FileFlowsServerUpdateAvailable, args);
    private void SystemEventsOnOnServerUpdating(UpdateEventArgs args)
        => UpdateEventTriggered(TaskType.FileFlowsServerUpdating, args);

    private void LibraryFileEventTriggered(TaskType type, LibraryFileEventArgs args)
    {
        if (LicenseService.IsLicensed(LicenseFlags.Tasks) == false)
            return;
        TriggerTaskType(type, new Dictionary<string, object>
        {
            { "FileName", args.File.Name },
            { "LibraryFile", args.File },
            { "Library", args.Library }
        });
    }

    private void SystemEventsOnOnLibraryFileAdd(LibraryFileEventArgs args) =>
        LibraryFileEventTriggered(TaskType.FileAdded, args);
    private void SystemEventsOnOnLibraryFileProcessingStarted(LibraryFileEventArgs args)
        => LibraryFileEventTriggered(TaskType.FileProcessing, args);
    private void SystemEventsOnOnLibraryFileProcessed(LibraryFileEventArgs args)
        => LibraryFileEventTriggered(TaskType.FileProcessed, args);
    private void SystemEventsOnOnLibraryFileProcessedSuceess(LibraryFileEventArgs args)
        => LibraryFileEventTriggered(TaskType.FileProcessSuccess, args);
    private void SystemEventsOnOnLibraryFileProcessedFailed(LibraryFileEventArgs args)
        => LibraryFileEventTriggered(TaskType.FileProcessFailed, args);

}