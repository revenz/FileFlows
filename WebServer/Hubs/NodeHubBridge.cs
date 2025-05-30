

using FileFlows.ServerShared.Models;
using Microsoft.AspNetCore.SignalR;
using FileFlows.Services.Interfaces;
using FileFlows.WebServer.Hubs;


namespace FileFlows.WebServer.Hubs;

/// <summary>
/// Bridge service that allows calling SignalR NodeHub methods without directly referencing SignalR.
/// </summary>
public class NodeHubBridge : INodeHubService
{
    private readonly IHubContext<NodeHub> _hubContext;
    //private readonly NodeManagerService _nodeManager; 
    private readonly NodeService _nodeService; 
    private readonly SettingsService _settingsService;
    private bool KeepFailedFlowTempFiles;
    private bool LicensedForTasks;

    public NodeHubBridge(IHubContext<NodeHub> hubContext)
    {
        _hubContext = hubContext;
        //_nodeManager = nodeManager;
        _settingsService = (SettingsService) ServiceLoader.Load<ISettingsService>();
        _nodeService = ServiceLoader.Load<NodeService>();
        _nodeService.OnNodeUpdated += OnNodeUpdated;
        _settingsService.RevisionUpdated += (revision) =>
        {
            _ = SettingsServiceOnRevisionUpdated();
        };
        _settingsService.OnSettingsUpdated += (updatedSettings) =>
        {
            KeepFailedFlowTempFiles = updatedSettings.KeepFailedFlowTempFiles;
        };
        var licenseService = ServiceLoader.Load<LicenseService>();
        LicensedForTasks = licenseService.GetLicense()?.IsLicensed(LicenseFlags.Tasks) == true;
        licenseService.OnLicenseUpdated += (license) =>
        {
            LicensedForTasks = license?.IsLicensed(LicenseFlags.Tasks) == true;
        };
    }

    public async Task Initialize()
    {
        var settings = await _settingsService.Get();
        KeepFailedFlowTempFiles = settings.KeepFailedFlowTempFiles;
    }

    /// <inheritdoc/>
    public async Task<Result<FileCheckResult>> ProcessFile(Guid nodeUid, LibraryFile file, Guid flowUid, string connectionId, int maxNodeRunners)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            return await _hubContext.Clients.Client(connectionId)
                .InvokeAsync<FileCheckResult>("ClientProcessFile", new RunFileArguments()
                {
                    LibraryFile = file,
                    FlowUid = flowUid,
                    KeepFailedFiles = KeepFailedFlowTempFiles,
                    CanRunPreExecuteCheck = LicensedForTasks,
                    MaxRunnersOnNode = maxNodeRunners
                }, cts.Token);
        }
        catch (OperationCanceledException)
        {
            return Result<FileCheckResult>.Fail("Request timed out after 20 seconds.");
        }
        catch (Exception ex)
        {
            return Result<FileCheckResult>.Fail($"Failed to communicate with node: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task UpdateConfig(string connectionId, int UpdateConfig)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        try
        {
            await _hubContext.Clients.Client(connectionId)
                .SendAsync("ConfigUpdated", UpdateConfig, cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Timeout occurred, optionally log this if needed
        }
        catch (Exception)
        {
            // Ignored
        }
    }

    private async Task SettingsServiceOnRevisionUpdated()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        try
        {
            var cfg = await _settingsService.GetCurrentConfiguration();
            if(cfg != null)
                await _hubContext.Clients.All.SendAsync("ConfigUpdated", cfg, cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Timeout occurred, optionally log this if needed
        }
        catch (Exception)
        {
            // Ignored
        }
    }
    

    private void OnNodeUpdated(ProcessingNode obj)
    {
        var nodes = ServiceLoader.Load<NodeService>().GetOnlineNodes();
        
        var connectionId = nodes.FirstOrDefault(x => x.NodeUid == obj.Uid)?.ConnectionId;
        
        if (connectionId == null)
            return; // node isn't connected
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            _ = _hubContext.Clients.Client(connectionId)
                .SendAsync("NodeUpdated", obj, cts.Token);
        }
        catch (Exception)
        {
            // Ignored
        }
    }

    /// <inheritdoc/>
    public async Task<bool> AbortFile(Guid uid)
    {
        var nodes = ServiceLoader.Load<NodeService>().GetOnlineNodes();
        foreach (var node in nodes)
        {
            CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            bool canceled = false;

            try
            {
                var task = _hubContext.Clients.Client(node.ConnectionId)
                    .InvokeAsync<bool>("AbortFile", uid, cts.Token);

                // Wait for the task to complete or timeout after 30 seconds
                if (await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(30), CancellationToken.None)) == task)
                {
                    canceled = await task; // Task completed before timeout
                }
                else
                {
                    cts.Cancel(); // Cancel the task
                    canceled = false; // Timeout occurred
                }
            }
            catch (OperationCanceledException)
            {
                canceled = false; // Handle timeout
            }
            catch (Exception ex)
            {
                Logger.Instance.ELog($"Error invoking AbortFile: {ex.Message}");
            }

            if (canceled)
                return true;
        }
        

        return false;
    }

    /// <inheritdoc/>
    public async Task AbortAll()
    {
        var nodes = ServiceLoader.Load<NodeService>().GetOnlineNodes();
        foreach (var node in nodes)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                _ = _hubContext.Clients.All
                    .SendAsync("AbortAll", cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Ignore
            }
            catch (Exception ex)
            {
                Logger.Instance.ELog($"Error invoking AbortFile: {ex.Message}");
            }
        }
    }
}
