using FileFlows.ServerShared.Models;

namespace FileFlows.WebServer.Controllers.RemoteControllers;

/// <summary>
/// Controller used by a processing node to communicate with the server
/// </summary>
[Route("/remote/node")]
[FileFlowsApiAuthorize]
[ApiExplorerSettings(IgnoreApi = true)]
public class NodeController : BaseController
{
    /// <summary>
    /// Get processing node by address
    /// </summary>
    /// <param name="address">The address</param>
    /// <param name="version">The version of the node</param>
    /// <returns>If found, the processing node</returns>
    [HttpGet("by-address/{address}")]
    public async Task<ProcessingNode?> GetByAddress([FromRoute] string address, [FromQuery] string version)
    {
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentNullException(nameof(address));

        var service = ServiceLoader.Load<NodeService>();
        var node = await service.GetByAddressAsync(address);
        if (node == null)
            return node;

        if (string.IsNullOrEmpty(version) == false && node.Version != version)
        {
            node.Version = version;
            node = await service.Update(node, await GetAuditDetails());
        }
        else
        {
            // this updates the "LastSeen"
            await service.UpdateLastSeen(node.Uid);
        }

        node.SignalrUrl = "flow";
        return node;
    }

    /// <summary>
    /// Basic flow list
    /// </summary>
    /// <returns>flow list</returns>
    [HttpGet("basic-list")]
    public async Task<Dictionary<Guid, string>> GetNodeList()
    {
        var items = await new NodeService().GetAllAsync();
        return items.Where(x => x.Enabled)
            .OrderBy(x => (x.Name == CommonVariables.InternalNodeName ? "Internal Processing Node" : x.Name).ToLowerInvariant())
            .ToDictionary(x => x.Uid, x => x.Name == CommonVariables.InternalNodeName ? "Internal Processing Node" : x.Name);
    }
    
    /// <summary>
    /// Get processing node
    /// </summary>
    /// <param name="uid">The UID of the processing node</param>
    /// <param name="version">The version of the node</param>
    /// <returns>The processing node instance</returns>
    [HttpGet("{uid}")]
    public async Task<ProcessingNode?> Get(Guid uid, [FromQuery] string version)
    {
        var service = ServiceLoader.Load<NodeService>();
        var node = await service.GetByUidAsync(uid);
        if (node == null)
            return node;

        if (string.IsNullOrEmpty(version) == false && node.Version != version)
        {
            node.Version = version;
            node = await service.Update(node, await GetAuditDetails());
        }
        else
        {
            // this updates the "LastSeen"
            await service.UpdateLastSeen(node.Uid);
        }

        node.SignalrUrl = "flow";
        return node;
    }

    /// <summary>
    /// Sets the status of a node
    /// </summary>
    /// <param name="uid">the UID of the node</param>
    /// <param name="status">the status</param>
    [HttpPost("{uid}/status/{status?}")]
    public void SetStatus(Guid uid, [FromRoute] ProcessingNodeStatus? status)
    {
        var service = ServiceLoader.Load<NodeService>();
        service.UpdateStatus(uid, status);
    }

    /// <summary>
    /// Gets the version an node update available
    /// </summary>
    /// <returns>the version an node update available</returns>
    [HttpGet("update-version")]
    public string GetNodeUpdateVersion()
    {
        if (LicenseService.IsLicensed(LicenseFlags.AutoUpdates) == false)
            return string.Empty;
        return Globals.Version;
    }
    
    /// <summary>
    /// Gets the node updater
    /// </summary>
    /// <param name="windows">if the update is for a windows system</param>
    /// <returns>the node updater</returns>
    [HttpGet("updater")]
    public IActionResult GetNodeUpdater([FromQuery] bool windows)
    {
        if (LicenseService.IsLicensed(LicenseFlags.AutoUpdates) == false)
            return new ContentResult();
        
        string updateFile = Path.Combine(DirectoryHelper.BaseDirectory, "Server", "Nodes",
            $"FileFlows-Node-{Globals.Version}.zip");
        if (System.IO.File.Exists(updateFile) == false)
            return new ContentResult();

        return File(System.IO.File.ReadAllBytes(updateFile), "application/zip");
    }

    /// <summary>
    /// Gets an node update available
    /// </summary>
    /// <param name="version">the current version of the node</param>
    /// <param name="windows">if the update is for a windows system</param>
    /// <returns>if there is a node update available, returns the update</returns>
    [HttpGet("updater-available")]
    public IActionResult GetNodeUpdater([FromQuery]string version, [FromQuery] bool windows)
    {
        if (LicenseService.IsLicensed(LicenseFlags.AutoUpdates) == false)
            return new ContentResult();
        if (string.IsNullOrWhiteSpace(version))
            return new ContentResult();
        var current = new Version(Globals.Version);
        var node =  new Version(version);
        if (node >= current)
            return new ContentResult();

        return GetNodeUpdater(windows);
    }
    //
    // /// <summary>
    // /// Records the node system statistics to the server
    // /// </summary>
    // /// <param name="args">the node system statistics</param>
    // [HttpPost("system-statistics")]
    // public async Task RecordNodeSystemStatistics([FromBody] NodeSystemStatistics args)
    // {
    //     await ServiceLoader.Load<NodeService>()?.UpdateLastSeen(args.Uid);
    //     SystemMonitor.Instance?.Record(args);
    // }
    //
    /// <summary>
    /// Gets if nodes should auto update
    /// </summary>
    /// <returns>if nodes should auto update</returns>
    [HttpGet("auto-update-nodes")]
    public async Task<bool> GetAutoUpdateNodes()
    {
        if (LicenseService.IsLicensed(LicenseFlags.AutoUpdates) == false)
            return false;
        var settings = await ServiceLoader.Load<ISettingsService>().Get();
        return settings.AutoUpdateNodes;

    }
    
    /// <summary>
    /// Pauses the system
    /// </summary>
    /// <param name="minutes">The minutes to pause the system for</param>
    [HttpPost("pause")]
    public async Task Pause([FromQuery] int minutes)
        => await ServiceLoader.Load<PausedService>().Pause(minutes);
    
    /// <summary>
    /// Gets if the system is paused
    /// </summary>
    /// <returns>true if the system is paused,otherwise false</returns>
    [HttpGet("system-is-paused")]
    public async Task<bool> SystemIsPaused()
    {
        var service = ServiceLoader.Load<ISettingsService>();
        var settings = await service.Get();
        return settings.IsPaused;
    }


    /// <summary>
    /// Register a processing node.  If already registered will return existing instance
    /// </summary>
    /// <param name="model">The register model containing information about the processing node being registered</param>
    /// <returns>The processing node instance</returns>
    [HttpPost("register")]
    [FileFlowsApiAuthorize(node: false)]
    public async Task<ProcessingNode?> RegisterPost([FromBody] RegisterModel model)
    {
        if (string.IsNullOrWhiteSpace(model?.Address))
            throw new ArgumentNullException(nameof(model.Address));
        if (string.IsNullOrWhiteSpace(model?.TempPath))
            throw new ArgumentNullException(nameof(model.TempPath));

        Logger.Instance.ILog("Registering Node: " + model.Address);
        if (model.HardwareInfo != null)
            Logger.Instance.ILog($"Node {model.Address} Hardware Info: " + Environment.NewLine + model.HardwareInfo);
        else
            Logger.Instance.ILog($"Node {model.Address} provided no Hardware Info");

        var address = model.Address.ToLowerInvariant().Trim();
        var service = ServiceLoader.Load<NodeService>();
        var data = await service.GetAllAsync();
        var existing = data.FirstOrDefault(x => x.Address.ToLowerInvariant() == address);
        if (existing != null)
        {
            if(existing.Version != model.Version) // existing.TempPath != model.TempPath)
            {
                //existing.FlowRunners = model.FlowRunners;
                //existing.Enabled = model.Enabled;
                //existing.TempPath = model.TempPath;
                //existing.OperatingSystem = model.OperatingSystem;
                existing.Architecture = model.Architecture;
                existing.OperatingSystem = model.OperatingSystem;
                existing.Version = model.Version;
                existing.HardwareInfo = model.HardwareInfo;
                existing = await service.Update(existing, await GetAuditDetails());
            }
            existing.SignalrUrl = "flow";
            return existing;
        }
        // doesnt exist, register a new node.
        var variables = await ServiceLoader.Load<VariableService>().GetAllAsync();

        if(model.Mappings?.Any() == true)
        {
            var ffmpegTool = variables?.FirstOrDefault(x => x.Name.Equals("ffmpeg", StringComparison.CurrentCultureIgnoreCase));
            if (ffmpegTool != null)
            {
                // update ffmpeg with actual location
                var mapping = model.Mappings.FirstOrDefault(x => x.Server.ToLower() == "ffmpeg");
                if(mapping != null)
                {
                    mapping.Server = ffmpegTool.Value;
                }
            }
        }

        var node = new ProcessingNode
        {
            Name = address,
            Address = address,
            //Enabled = model.Enabled,
            //FlowRunners = model.FlowRunners,
            Enabled = false,
            FlowRunners = 1,
            TempPath = model.TempPath,
            OperatingSystem = model.OperatingSystem,
            Architecture = model.Architecture,
            Version = model.Version,
            Schedule = new string('1', 672),
            AllLibraries = ProcessingLibraries.All,
            Mappings = model.Mappings?.Select(x => new KeyValuePair<string, string>(x.Server, x.Local))?.ToList() ??
                       variables?.Select(x => new
                           KeyValuePair<string, string>(x.Value, "")
                       )?.ToList() ?? new(),
            HardwareInfo = model.HardwareInfo
        };
        var result = await service.Update(node, await GetAuditDetails());
        if (result.Failed(out string error))
        {
            Logger.Instance.ELog("Failed registering node: " + error);
            throw new Exception(error);
        }

        node = result.Value;
        node.SignalrUrl = "flow";
        return node;
    }
}