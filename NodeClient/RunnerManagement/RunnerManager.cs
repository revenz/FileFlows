
using System.Collections.Concurrent;
using FileFlows.Common;
using FileFlows.FlowRunner;
using FileFlows.Plugin;
using FileFlows.RemoteServices;
using FileFlows.ServerShared;
using FileFlows.ServerShared.Models;
using FileFlows.ServerShared.Services;
using FileFlows.Shared;
using FileFlows.Shared.Models;
using NPoco.Expressions;

namespace FileFlows.NodeClient;

/// <summary>
/// Manages the execution of runners, ensuring concurrency limits are respected.
/// </summary>
public class RunnerManager
{
    private readonly ConcurrentDictionary<Guid, Runner> _activeRunners = new();
    private readonly object _lock = new();
    private readonly ConfigurationService _configService;

    /// <summary>
    /// Gets the active runnners
    /// </summary>
    public Dictionary<Guid, Runner> ActiveRunners => _activeRunners.ToDictionary();

    /// <summary>
    /// Action that is called when the runner is updated
    /// </summary>
    public event Action RunnerUpdated;
    
    private SemaphoreSlim _runnerSemaphore = new(1, 1);

    /// <summary>
    /// Constructs a new instance of the runner manager
    /// </summary>
    public RunnerManager()
    {
        _configService = ServiceLoader.Load<ConfigurationService>();
    }

    private ILogger _Logger;

    /// <summary>
    /// Gets or sets the logger
    /// </summary>
    public ILogger Logger
    {
        get => _Logger ?? FileFlows.Shared.Logger.Instance; 
        set => _Logger = value;
    }

    /// <summary>
    /// Attempts to start a new runner if the maximum allowed runners has not been reached.
    /// </summary>
    /// <param name="client">the client starting this run instance</param>
    /// <param name="args">The arguments for the runner execution.</param>
    /// <param name="node">the processing node</param>
    /// <param name="config">the current configuration</param>
    /// <returns>True if the runner was started, otherwise false.</returns>
    public async Task<bool> TryStartRunner(Client client, RunFileArguments args, ProcessingNode node, ConfigurationRevision config)
    {
        if (await _runnerSemaphore.WaitAsync(10_000) == false)
            return false;

        try
        {
            int maxRunners = args.MaxRunnersOnNode;

            if (_activeRunners.Count >= maxRunners)
            {
                Logger.ILog($"At maximum number of runners reached ({_activeRunners.Count} vs {maxRunners}).");
                return false;
            }

            if (args.CanRunPreExecuteCheck && PreExecuteScriptTest(node, _activeRunners.Count, config) == false)
            {
                Logger.ILog("Pre-execute check failed");
                return false;
            }

            var tempPath = GetTempPath(node);
            if (tempPath == null)
            {
                Logger.ILog("Failed to locate temp path.");
                return false;
            }

            // move the file as Processing
            var lf = args.LibraryFile;
            lf.Status = FileStatus.Processing;
            lf.ExecutedNodes = [];
            lf.Additional ??= new();
            lf.Additional.DisplayName = string.Empty;
            lf.Node = new()
            {
                Uid = node.Uid,
                Name = node.Name,
                Type = node.GetType().FullName!
            };

            var cfgService = ServiceLoader.Load<ConfigurationService>();
            var flow = cfgService.CurrentConfig?.Flows.FirstOrDefault(x => x.Uid == args.FlowUid);
            lf.Flow = new()
            {
                Uid = args.FlowUid,
                Name = flow?.Name,
                Type = typeof(Flow).FullName!
            };

            var runner = new Runner(client, args, node, tempPath, OnRunnerCompleted);
            if (_activeRunners.TryAdd(runner.Id, runner))
            {
                if (await client.FileStartProcessing(lf) == false)
                {
                    _activeRunners.TryRemove(runner.Id, out _);
                    Logger.ILog("FileStartProcessing failed: " + lf.Name);
                    // abort, could not start processing file
                    return false;
                }
                Logger.ILog("Starting runner: " + runner.Id + " : " + lf.Name);
                runner.Start(lf);
                EventManager.Broadcast(EventNames.RUNNERS_UPDATED, _activeRunners.Count);
                RunnerUpdated?.Invoke();
                return true;
            }

            Logger.ILog("Failed to add runner");
            return false;
        }
        finally
        {
            _runnerSemaphore.Release();
        }
    }


    private string? GetTempPath(ProcessingNode node)
    {
        string tempPath = node.TempPath?.EmptyAsNull() ?? string.Empty;
        if (string.IsNullOrEmpty(tempPath))
        {
            Logger?.ELog($"Temp Path not set on node '{node.Name}', cannot process");
            return null;
        }

        if (Directory.Exists(tempPath) == false)
        {
            try
            {
                Directory.CreateDirectory(tempPath);
            }
            catch (Exception)
            {
                Logger?.ELog(
                    $"Temp Path does not exist on on node '{node.Name}', and failed to create it: {tempPath}");
                return null;
            }
        }

        return tempPath;
    }
    
    /// <summary>
    /// Called when a runner completes its execution.
    /// </summary>
    /// <param name="runnerId">The ID of the completed runner.</param>
    private void OnRunnerCompleted(Guid runnerId)
    {
        _activeRunners.TryRemove(runnerId, out _);
        Logger?.ILog("Runner completed: " + runnerId + ", total runners remaining: " + _activeRunners.Count);
        EventManager.Broadcast(EventNames.RUNNERS_UPDATED,  _activeRunners.Count);
        RunnerUpdated?.Invoke();
    }

    /// <summary>
    /// Gets the current number of active runners.
    /// </summary>
    public int ActiveRunnerCount => _activeRunners.Count;
    
    /// <summary>
    /// Gets if there are any active runners
    /// </summary>
    public bool HasActiveRunners => _activeRunners.Count > 0;
    
    
    /// <summary>
    /// Tests if there is a pre-execute script and if so executes it
    /// </summary>
    /// <param name="node">the processing node</param>
    /// <param name="currentRunners">the number of the current runners</param>
    /// <param name="config">the current configuration</param>
    /// <returns>if the pre-execute script passes</returns>
    private bool PreExecuteScriptTest(ProcessingNode node, int currentRunners, ConfigurationRevision config)
    {
        if (node.PreExecuteScript == null || node.PreExecuteScript.Value == Guid.Empty)
            return true; // no script
        var script = config.SystemScripts?.FirstOrDefault(x => node.PreExecuteScript!.Value == x.Uid);
        if (script == null)
            return true; // no script
        
        if (script.Language != ScriptLanguage.JavaScript)
        {
            StringLogger logger = new();
            var seResult = new ScriptExecutor().Execute(new()
            {
                Code = script.Code,
                TempPath = Path.GetTempPath(),
                Language = script.Language,
                Logger = logger,
                ScriptType = ScriptType.System,
                NotificationCallback = (severity, title, message) =>
                {
                    _ = ServiceLoader.Load<INotificationService>()
                        .Record((NotificationSeverity)severity, title, message);
                } 
            });
            if (seResult.Failed(out var error))
            {
                _ = ServiceLoader.Load<INotificationService>().Record(NotificationSeverity.Warning,
                    $"Failed executing pre-execute script '{script.Name}'",
                    $"Failed to execute on node '{node.Name}': {error}");
                Logger.WLog("Failed running pre-execute script: " + error + "\n" + logger);
                return false;
            }
            bool preExecuteResult = seResult.Value == 1;
            
            if(preExecuteResult == false)
                Logger.ILog("Pre-Execute Script Returned False:\n" + logger);
            else
                Logger.ILog("Pre-Execute Script Successful:\n" + logger);
            
            return preExecuteResult;
        }
        string scriptDir = Path.Combine(_configService.GetConfigurationDirectory(config.Revision), "Scripts");
        string sharedDir = Path.Combine(scriptDir, "Shared");
        string jsFile = Path.Combine(scriptDir, "System", node.PreExecuteScript + ".js");
        if (File.Exists(jsFile) == false)
        {
            Logger.ELog("Failed to locate pre-execute script: " + node.PreExecuteScript);
            return false;
        }

        Logger.ILog("Loading Pre-Execute Script: " + jsFile);
        string code = File.ReadAllText(jsFile);
        if (string.IsNullOrWhiteSpace(code))
        {
            Logger.ELog("Failed to load pre-execute script code");
            return false;
        }

        var variables = config.Variables.ToDictionary(x => x.Key, x => (object)x.Value);
        variables["FileFlows.Url"] = RemoteService.ServiceBaseUrl;
        var result = ScriptExecutor.Execute(code, variables, sharedDirectory: sharedDir);
        if (result.Success == false)
        {
            Logger.ELog("Pre-execute script failed: " + result.ReturnValue + "\n" + result.Log);
            return false;
        }
        Logger.ILog("Pre-Execute script returned: " + (result.ReturnValue == null ? "" : System.Text.Json.JsonSerializer.Serialize(result.ReturnValue)));
        

        if (result.ReturnValue?.ToString()?.ToLowerInvariant() == "exit")
        {
            Logger.ILog("Exiting");
            return false;
        }

        if (result.ReturnValue?.ToString()?.ToLowerInvariant() == "restart")
        {
            if (Globals.IsDocker == false)
            {
                Logger.WLog("Requested restart but not running inside docker");
                return false;
            }

            if (currentRunners > 0)
            {
                Logger.WLog("Requested restart, but there are executing runners");
                return false;
            }
            
            Logger.ILog("Restarting...");
            Thread.Sleep(5_000);
            Environment.Exit(0);
            return false;
        }
        
        if (result.ReturnValue as bool? == false)
        {
            Logger.ELog("Output from pre-execute script failed: " + result.ReturnValue + "\n" + result.Log);
            return false;
        }
        Logger.ILog("Pre-execute script passed: \n"+ result.Log.Replace("\\n", "\n"));
        return true;
    }

    /// <summary>
    /// Gets the active runner UIDs
    /// </summary>
    /// <returns>the active runner UIDs</returns>
    public List<Guid> GetActiveRunnerUids()
        => _activeRunners.Select(x => x.Key).ToList();

    /// <summary>
    /// Aborts a runner if it is running
    /// </summary>
    /// <param name="uid">the UID of the runner</param>
    /// <returns>true if the runner was requested to cancel</returns>
    public async Task<bool> AbortRunner(Guid uid)
    {
        if (_activeRunners.TryGetValue(uid, out var runner) == false)
            return false;
        runner.Info.Aborted = true;
        RunnerUpdated?.Invoke();
        await runner.Abort();
        return true;
    }

    /// <summary>
    /// Update the runner info
    /// </summary>
    /// <param name="info">the runner info</param>
    public void UpdateRunner(FlowExecutorInfo info)
    {
        if (_activeRunners.TryGetValue(info.LibraryFile.Uid, out var runner) == false)
            return;
        // started at isnt tracked in the runner it self
        info.StartedAt = runner.Info.StartedAt;
        info.Aborted = runner.Info.Aborted;
        info.TotalParts = runner.Info.TotalParts;
        
        runner.Info = info;
        RunnerUpdated?.Invoke();
    }
}