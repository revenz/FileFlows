
using System.Collections.Concurrent;
using FileFlows.Common;
using FileFlows.Plugin;
using FileFlows.RemoteServices;
using FileFlows.ServerShared;
using FileFlows.ServerShared.Models;
using FileFlows.ServerShared.Services;
using FileFlows.Shared;
using FileFlows.Shared.Models;

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
    /// Contructs a new instance of the runner manager
    /// </summary>
    public RunnerManager()
    {
        _configService = ServiceLoader.Load<ConfigurationService>();
    }

    /// <summary>
    /// Attempts to start a new runner if the maximum allowed runners has not been reached.
    /// </summary>
    /// <param name="args">The arguments for the runner execution.</param>
    /// <param name="node">the processing node</param>
    /// <param name="config">the current configuration</param>
    /// <returns>True if the runner was started, otherwise false.</returns>
    public bool TryStartRunner(RunFileArguments args, ProcessingNode node, ConfigurationRevision config)
    {
        int maxRunners = node.FlowRunners;

        lock (_lock)
        {
            if (_activeRunners.Count >= maxRunners)
                return false;

            if (args.CanRunPreExecuteCheck && PreExecuteScriptTest(node, _activeRunners.Count, config) == false)
                return false;

            var tempPath = GetTempPath(node);
            if (tempPath == null)
                return false;

            var runner = new Runner(args, node, tempPath, OnRunnerCompleted);
            if (_activeRunners.TryAdd(runner.Id, runner))
            {
                runner.Start();
                return true;
            }
            return false;
        }
    }


    private string? GetTempPath(ProcessingNode node)
    {
        string tempPath = node.TempPath?.EmptyAsNull() ?? string.Empty;
        if (string.IsNullOrEmpty(tempPath))
        {
            Logger.Instance?.ELog($"Temp Path not set on node '{node.Name}', cannot process");
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
                Logger.Instance?.ELog(
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
                Logger.Instance.WLog("Failed running pre-execute script: " + error + "\n" + logger);
                return false;
            }
            bool preExecuteResult = seResult.Value == 1;
            
            if(preExecuteResult == false)
                Logger.Instance.ILog("Pre-Execute Script Returned False:\n" + logger);
            else
                Logger.Instance.ILog("Pre-Execute Script Successful:\n" + logger);
            
            return preExecuteResult;
        }
        string scriptDir = Path.Combine(_configService.GetConfigurationDirectory(config.Revision), "Scripts");
        string sharedDir = Path.Combine(scriptDir, "Shared");
        string jsFile = Path.Combine(scriptDir, "System", node.PreExecuteScript + ".js");
        if (File.Exists(jsFile) == false)
        {
            Logger.Instance.ELog("Failed to locate pre-execute script: " + node.PreExecuteScript);
            return false;
        }

        Logger.Instance.ILog("Loading Pre-Execute Script: " + jsFile);
        string code = File.ReadAllText(jsFile);
        if (string.IsNullOrWhiteSpace(code))
        {
            Logger.Instance.ELog("Failed to load pre-execute script code");
            return false;
        }

        var variableService = ServiceLoader.Load<IVariableService>();
        var variables = variableService.GetAllAsync().Result?.ToDictionary(x => x.Name, x => (object)x.Value) ?? new ();
        variables["FileFlows.Url"] = RemoteService.ServiceBaseUrl;
        var result = ScriptExecutor.Execute(code, variables, sharedDirectory: sharedDir);
        if (result.Success == false)
        {
            Logger.Instance.ELog("Pre-execute script failed: " + result.ReturnValue + "\n" + result.Log);
            return false;
        }
        Logger.Instance.ILog("Pre-Execute script returned: " + (result.ReturnValue == null ? "" : System.Text.Json.JsonSerializer.Serialize(result.ReturnValue)));
        

        if (result.ReturnValue?.ToString()?.ToLowerInvariant() == "exit")
        {
            Logger.Instance.ILog("Exiting");
            return false;
        }

        if (result.ReturnValue?.ToString()?.ToLowerInvariant() == "restart")
        {
            if (Globals.IsDocker == false)
            {
                Logger.Instance.WLog("Requested restart but not running inside docker");
                return false;
            }

            if (currentRunners > 0)
            {
                Logger.Instance.WLog("Requested restart, but there are executing runners");
                return false;
            }
            
            Logger.Instance.ILog("Restarting...");
            Thread.Sleep(5_000);
            Environment.Exit(0);
            return false;
        }
        
        if (result.ReturnValue as bool? == false)
        {
            Logger.Instance.ELog("Output from pre-execute script failed: " + result.ReturnValue + "\n" + result.Log);
            return false;
        }
        Logger.Instance.ILog("Pre-execute script passed: \n"+ result.Log.Replace("\\n", "\n"));
        return true;
    }

    /// <summary>
    /// Gets the active runner UIDs
    /// </summary>
    /// <returns>the active runner UIDs</returns>
    public List<Guid> GetActiveRunnerUids()
        => _activeRunners.Select(x => x.Key).ToList();
}