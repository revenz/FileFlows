

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
    

    public NodeHubBridge(IHubContext<NodeHub> hubContext)
    {
        _hubContext = hubContext;
        //_nodeManager = nodeManager;
        _settingsService = (SettingsService) ServiceLoader.Load<ISettingsService>();
        _nodeService = ServiceLoader.Load<NodeService>();
        _settingsService.RevisionUpdated += (revision) =>
        {
            _ = SettingsServiceOnRevisionUpdated();
        };
    }

    /// <inheritdoc/>
    public async Task<Result<bool>> ProcessFile(Guid nodeUid, LibraryFile file, string connectionId)
    {
        try
        {
            var settings = await _settingsService.Get();
            return await _hubContext.Clients.Client(connectionId)
                .InvokeAsync<bool>("ClientProcessFile", new RunFileArguments()
                {
                    LibraryFile = file,
                    KeepFailedFiles = settings.KeepFailedFlowTempFiles,
                    CanRunPreExecuteCheck = LicenseService.IsLicensed(LicenseFlags.Tasks)
                }, CancellationToken.None);
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail($"Failed to communicate with node: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task UpdateConfig(string connectionId, int UpdateConfig)
    {
        try
        {
            await _hubContext.Clients.Client(connectionId).SendAsync("ConfigUpdated", UpdateConfig);
        }
        catch (Exception)
        {
            // Ignored
        }
    }

    private async Task SettingsServiceOnRevisionUpdated()
    {
        try
        {
            var cfg = await _settingsService.GetCurrentConfiguration();
            if(cfg != null)
                await _hubContext.Clients.All.SendAsync("ConfigUpdated", cfg);
        }
        catch (Exception)
        {
            // Ignored
        }
    }
    
    /// <inheritdoc/>
    public async Task NodeUpdate(ProcessingNode node)
    {
        // var connectionId = _nodeManager.GetNodeConnection(node.Uid);
        // if (connectionId == null)
        //     return; // node isn't connected
        // try
        // {
        //     await _hubContext.Clients.Client(connectionId)
        //         .SendAsync("NodeUpdated", node);
        // }
        // catch (Exception)
        // {
        //     // Ignored
        // }
    }
}
