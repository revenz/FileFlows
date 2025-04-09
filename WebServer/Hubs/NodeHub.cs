using FileFlows.ServerModels;
using FileFlows.Services.FileProcessing;
using FileFlows.Shared.Models.SignalAre;
using Microsoft.AspNetCore.SignalR;
using ILogger = FileFlows.Common.ILogger;

namespace FileFlows.WebServer.Hubs;

/// <summary>
/// Represents a SignalR hub for managing node connections.
/// </summary>
public partial class NodeHub : Hub
{
    private readonly NodeService _nodeService;
    private readonly ISettingsService _settingsService;
    private readonly ILogger _logger;
    private readonly NodeLogger _nodeLogger = new();
    
    /// <summary>
    /// Special token just for internal use.
    /// </summary>
    internal static string InternalAccessToken = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Initializes a new instance of the <see cref="NodeHub"/> class.
    /// </summary>
    public NodeHub()
    {
        _nodeService = ServiceLoader.Load<NodeService>();
        _settingsService = ServiceLoader.Load<ISettingsService>();
        _logger = Logger.Instance;
    }
    
    /// <summary>
    /// Called when a client connects. Validates the access token.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var accessToken = string.Empty;

        if (httpContext?.Request.Headers.TryGetValue("Authorization", out var value) == true)
        {
            accessToken = value.FirstOrDefault()?.Replace("Bearer ", "") ?? string.Empty;
        }

        var remoteIp = httpContext?.Connection.RemoteIpAddress;
        
        if (ValidateAccessToken(accessToken) == false)
        {
            _logger.WLog($"Unauthorized connection attempt from {remoteIp}");
            Context.Abort(); // Reject the connection
            return;
        }
        
        _logger.ILog($"Node connected from {remoteIp}");
        await base.OnConnectedAsync();
    }
    
    /// <summary>
    /// Updates the status of a node.
    /// </summary>
    /// <param name="info">The node status information.</param>
    public async Task<NodeStatusUpdateResult> UpdateNodeStatus(OnlineNodeInfo info)
    {
        try
        {
            if (info == null)
                return NodeStatusUpdateResult.InvalidModel;
            _ = _nodeService.UpdateNodeStatusFromNode(info);
            var configRevision = await ServiceLoader.Load<ISettingsService>().GetCurrentConfigurationRevision();
            if (info.ConfigRevision != configRevision)
            {
                Logger.Instance.ILog($"Configuration out of date for node {(info.Node?.Name?.EmptyAsNull() ?? info.Name?.EmptyAsNull() ?? info.NodeUid.ToString() ?? "unknown")}, has {info.ConfigRevision}, expected {configRevision}");
                return NodeStatusUpdateResult.UpdateConfiguration;
            }

            return NodeStatusUpdateResult.Success;
        }
        catch (Exception ex)
        {
            _logger.ELog($"Failed updating node status: {ex}");
            return NodeStatusUpdateResult.Exception;
        }
    }
    
    
    /// <summary>
    /// Validates the access token.
    /// </summary>
    private bool ValidateAccessToken(string accessToken)
    {
        if (accessToken == InternalAccessToken)
            return true;

        if (!LicenseService.IsLicensed(LicenseFlags.UserSecurity))
            return true; // If security isn't enabled, allow connection

        var appSettings = ServiceLoader.Load<AppSettingsService>().Settings;
        if (appSettings.Security == SecurityMode.Off)
            return true; // If security is off, allow connection

        var settings = _settingsService.Get().Result;
        return settings.AccessToken?.Equals(accessToken, StringComparison.InvariantCultureIgnoreCase) == true;
    }
    
    /// <summary>
    /// Registers a node when it connects.
    /// </summary>
    /// <param name="parameters">The register parameters.</param>
    /// <returns>the result of the registration</returns>
    public async Task<NodeRegisterResult> RegisterNode(NodeRegisterParameters parameters)
    {
        _logger.ILog($"Registering node: {parameters.Hostname}");
        var node = await _nodeService.GetByAddressAsync(parameters.Hostname);
        if (node != null)
        {
            var sorterService = ServiceLoader.Load<FileSorterService>();
            sorterService.NodeReconnected(node.Uid, parameters.ActiveRunners);
            // already exists
            if(node.Version != parameters.Version) 
            {
                node.Architecture = parameters.Architecture;
                node.OperatingSystem = parameters.OperatingSystem;
                node.Version = parameters.Version;
                node.HardwareInfo = parameters.HardwareInfo;
                node = await _nodeService.Update(node, null);
            }
        }
        else
        {
            _logger.ILog($"Node {parameters.Hostname} does not exist, registering new node");

            // doesnt exist, register a new node.
            var variables = await ServiceLoader.Load<VariableService>().GetAllAsync();
            bool isSystem = parameters.Hostname == CommonVariables.InternalNodeName;
            node = new ProcessingNode
            {
                Name = parameters.Hostname,
                Address = parameters.Hostname,
                Architecture = parameters.Architecture,
                OperatingSystem = parameters.OperatingSystem,
                Version = parameters.Version,
                HardwareInfo = parameters.HardwareInfo,
                TempPath = parameters.TempPath,
                Enabled = isSystem, // default to disabled so they have to configure it first
                FlowRunners = 1,
                AllLibraries = ProcessingLibraries.All,
                Schedule = new string('1', 672)
            };
            if(parameters.Mappings?.Any() == true)
            {
                var ffmpegTool = variables?.FirstOrDefault(x => x.Name.Equals("ffmpeg", StringComparison.CurrentCultureIgnoreCase));
                if (ffmpegTool != null)
                {
                    // Find and replace the existing ffmpeg mapping if it exists
                    var index = parameters.Mappings.FindIndex(x => x.Key.Equals("ffmpeg", StringComparison.InvariantCultureIgnoreCase));
                    if (index >= 0)
                    {
                        parameters.Mappings[index] = new KeyValuePair<string, string>(ffmpegTool.Value, parameters.Mappings[index].Value);
                    }
                }

                node.Mappings = parameters.Mappings;
            }
            else
            {
                node.Mappings = variables?.Select(x => new
                    KeyValuePair<string, string>(x.Value, "")
                )?.ToList() ?? new();
            }
            node = await _nodeService.Update(node, null);
        }

        _nodeService.SetConnectionId(Context.ConnectionId, parameters.ConfigRevision, node, parameters.ActiveRunners);
        _logger.ILog($"Node registered: {node.Name} ({node.Uid})");
        await ServiceLoader.Load<NodeService>().UpdateNodeStatusSummaries();
        return new NodeRegisterResult()
        {
            Success = true,
            Node = node,
            ServerVersion = Globals.Version,
            ConnectionId = Context.ConnectionId,
            CurrentConfigRevision = await _settingsService.GetCurrentConfigurationRevision()
        };
    }
    
    /// <summary>
    /// Retrieves the current configuration.
    /// </summary>
    public async Task<ConfigurationRevision?> GetConfiguration()
        => await _settingsService.GetCurrentConfiguration();
    
    /// <summary>
    /// Unregisters a node when it disconnects.
    /// </summary>
    /// <param name="nodeUid">The unique identifier of the disconnecting node.</param>
    public async Task UnregisterNode(Guid nodeUid)
    {
        _logger.ILog($"Unregistering node: {nodeUid}");
        _nodeService.RemoveOnlineNode(nodeUid);
    }
    
    /// <summary>
    /// Detects when a client disconnects and removes it from tracking.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception exception)
    {
        _logger.ILog($"Node disconnected: {Context.ConnectionId}");
        _nodeService.SetPendingDisconnect(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
        await ServiceLoader.Load<NodeService>().UpdateNodeStatusSummaries();
    }

    /// <summary>
    /// Logs message from a node.
    /// </summary>
    /// <param name="nodeName">The name of the node.</param>
    /// <param name="messages">The log messages.</param>
    public async Task Log(string nodeName, string[] messages)
        => await _nodeLogger.Log(nodeName, messages);
}
