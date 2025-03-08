using FileFlows.NodeClient;
using FileFlows.RemoteServices;
using FileFlows.ServerShared.Services;
using FileFlows.ServerShared.Workers;
using FileFlows.Shared.Models;

namespace FileFlows.Node.Workers;

/// <summary>
/// Worker to clean up temporary files
/// </summary>
public class TempFileCleaner : Worker
{
    private string nodeAddress;
    
    /// <summary>
    /// Constructs a temp file cleaner
    /// <param name="nodeAddress">The name of the node</param>
    /// </summary>
    public TempFileCleaner(string nodeAddress) : base(ScheduleType.Daily, 5)
    {
        this.nodeAddress = nodeAddress;
        Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromMinutes(1));
            Trigger();
        });
    }

    /// <summary>
    /// Executes the cleaner
    /// </summary>
    protected sealed override void Execute()
    {
        var service = ServiceLoader.Load<INodeService>();
        ProcessingNode? node = null;
        try
        {
            node = string.IsNullOrWhiteSpace(nodeAddress)
                ? service.GetServerNodeAsync().Result
                : service.GetByAddressAsync(nodeAddress).Result;
        }
        catch (Exception ex)
        {
            Logger.Instance.WLog("Failed to fetch node: " + ex.Message);
        }
        
        if (string.IsNullOrWhiteSpace(node?.TempPath))
            return;
        var tempDir = new DirectoryInfo(node.TempPath);
        if (tempDir.Exists == false)
            return;

        var runnerService =  ServiceLoader.Load<RunnerManager>();
        var uids = runnerService.GetActiveRunnerUids();

        var executors = uids.Select(x => "Runner-" + x).ToList();
        
        Logger.Instance?.ILog("About to clean temporary directory: " + tempDir.FullName);
        foreach (var dir in tempDir.GetDirectories())
        {
            if (dir.Name.StartsWith("Runner-", StringComparison.InvariantCultureIgnoreCase) == false)
                continue;
            
            if (executors.Contains(dir.Name))
            {
                Logger.Instance?.ILog($"Skipping directory '{dir.Name}' as it still executing");
                continue; // still executing
            }

            if (dir.CreationTimeUtc < DateTime.UtcNow.AddDays(-1))
            {
                try
                {
                    dir.Delete(recursive: true);
                    Logger.Instance?.ILog($"Deleted directory '{dir.Name}' from temp directory");
                }
                catch (Exception ex)
                {
                    Logger.Instance?.WLog($"Failed to delete directory '{dir.Name}' from temp directory: " + ex.Message);
                }
            }
        }
    }
}