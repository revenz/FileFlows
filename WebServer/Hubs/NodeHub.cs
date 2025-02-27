using FileFlows.Shared.Models.SignalAre;
using Microsoft.AspNetCore.SignalR;
using ILogger = FileFlows.Common.ILogger;

namespace FileFlows.WebServer.Hubs;

/// <summary>
/// Represents a SignalR hub for managing node connections.
/// </summary>
public class NodeHub : Hub
{
    private readonly NodeService _nodeService;
    private readonly ISettingsService _settingsService;
    private readonly ILogger _logger;
    
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
        var accessToken = httpContext?.Request.Headers["Authorization"].ToString()?.Replace("Bearer ", "");
        var remoteIp = httpContext?.Connection.RemoteIpAddress;
        
        if (string.IsNullOrEmpty(accessToken) || !ValidateAccessToken(accessToken))
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
    public void UpdateNodeStatus(NodeService.OnlineNodeInfo info)
    {
        _logger.DLog($"Updating node status: {info?.NodeUid}");
        _nodeService.UpdateNodeStatus(info);
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
    /// <param name="hostname">The hostname of the node.</param>
    /// <param name="currentConfig">the nodes current config</param>
    public async Task<NodeRegisterResult> RegisterNode(string hostname, int currentConfig)
    {
        _logger.ILog($"Registering node: {hostname}");
        var node = await GetNode(hostname);
        if (node == null)
        {
            _logger.WLog($"Failed to register node: {hostname}");
            return new NodeRegisterResult() { Success = false };
        }
        
        _nodeService.SetConnectionId(node.Uid, Context.ConnectionId, currentConfig);
        _logger.ILog($"Node registered: {node.Name} ({node.Uid})");
        return new NodeRegisterResult()
        {
            Success = true,
            Node = node,
            ConnectionId = Context.ConnectionId,
            CurrentConfigRevision = await _settingsService.GetCurrentConfigurationRevision()
        };
    }
    
    /// <summary>
    /// Retrieves the current configuration.
    /// </summary>
    public async Task<ConfigurationRevision?> GetConfiguration()
        => await _settingsService.GetCurrentConfiguration();
    
    private async Task<ProcessingNode> GetNode(string hostname)
    {
        hostname = hostname?.Trim() ?? string.Empty;
        _logger.DLog($"Fetching node: {hostname}");
        var node = await _nodeService.GetByAddressAsync(hostname);
        if (node != null)
            return node;
        
        _logger.ILog($"Node {hostname} does not exist, registering new node");
        var variables = await ServiceLoader.Load<VariableService>().GetAllAsync();
        bool isSystem = hostname == CommonVariables.InternalNodeName;
        node = new ProcessingNode
        {
            Name = hostname,
            Address = hostname,
            Enabled = isSystem,
            FlowRunners = 1,
            AllLibraries = ProcessingLibraries.All,
            Schedule = new string('1', 672),
            Mappings = isSystem
                ? []
                : variables?.Select(x => new KeyValuePair<string, string>(x.Value, string.Empty)).ToList() ?? []
        };
        return await _nodeService.Update(node, null);
    }
    
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
        _nodeService.RemoveOnlineNodeByConnection(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
    
    /// <summary>
    /// Logs a message from a node.
    /// </summary>
    /// <param name="nodeUid">The node's unique identifier.</param>
    /// <param name="message">The log message.</param>
    public Task Log(Guid nodeUid, string message)
    {
        _logger.ILog($"[{nodeUid}] {message}"); // Store in centralized logging
        return Task.CompletedTask;
    }
}
