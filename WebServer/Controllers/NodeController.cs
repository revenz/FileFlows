using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;

namespace FileFlows.WebServer.Controllers;

/// <summary>
/// Processing node controller
/// </summary>
[Route("/api/node")]
[FileFlowsAuthorize(UserRole.Nodes)]
public class NodeController : BaseController
{
    /// <summary>
    /// Gets a list of all processing nodes in the system
    /// </summary>
    /// <returns>a list of processing node</returns>
    [HttpGet]
    public async Task<IEnumerable<ProcessingNode>> GetAll()
    {
        var service = ServiceLoader.Load<NodeService>();
        var nodes = (await service.GetAllAsync())
            .OrderBy(x => x.Address == CommonVariables.InternalNodeName ? 0 : 1)
            .ThenBy(x => x.Name)
            .ToList();
        
        return nodes.OrderBy(x => x.Name.ToLowerInvariant());
    }
    
    /// <summary>
    /// Gets a node icon
    /// </summary>
    /// <param name="uid">the UID of the node</param>
    /// <returns>the icon</returns>
    [HttpGet("{uid}/icon")]
    [AllowAnonymous]
    public async Task<IActionResult> NodeIcon(Guid uid)
    {
        var node = await ServiceLoader.Load<NodeService>().GetByUidAsync(uid);
        if (string.IsNullOrWhiteSpace(node?.Icon) || node.Icon.StartsWith("data:", StringComparison.InvariantCultureIgnoreCase) == false)
            return NotFound();
        try
        {
            // Remove the "data:" prefix and decode the base64 data
            var base64Data = node.Icon.Substring(node.Icon.IndexOf(',', StringComparison.Ordinal) + 1);
            var imageData = Convert.FromBase64String(base64Data);

            // Determine the MIME type from the data URL (e.g., "data:image/png;base64,")
            var mimeType = node.Icon[5..node.Icon.IndexOf(';', StringComparison.Ordinal)];
        
            // Return the image data as a file with the appropriate MIME type
            return File(imageData, mimeType);
        }
        catch (FormatException)
        {
            return BadRequest("Invalid base64 image data.");
        }
    }
    
    /// <summary>
    /// Gets a list of all processing nodes in the system
    /// </summary>
    /// <returns>a list of processing node</returns>
    [HttpGet("list")]
    public async Task<IEnumerable<ProcessingNode>> ListAll()
    {
        var service = ServiceLoader.Load<NodeService>();
        var nodes = (await service.GetAllAsync()).Select(x => new ProcessingNode()
        {
            Uid = x.Uid,
            Name = x.Name,
            OperatingSystem = x.OperatingSystem,
            Architecture = x.Architecture
        }).ToList();
        return nodes;
    }
    
    /// <summary>
    /// Gets an overview of the nodes
    /// </summary>
    /// <returns>the response</returns>
    [HttpGet("overview")]
    public async Task<IActionResult> Overview()
    {
        var data = (await GetAll())
            .OrderBy(x => x.Enabled ? 1 : 2)
            .ThenByDescending(x => x.Priority)
            .ThenBy(x => x.Uid == CommonVariables.InternalNodeUid ? 1 : 2)
            .ThenBy(x => x.Name)
            .Select(x => new
            {
                Name = x.Uid == CommonVariables.InternalNodeUid ? "Internal" : x.Name,
                Status = (int) x.Status,
                StatusText = $"Enums.{nameof(ProcessingNodeStatus)}.{x.Status}"
            });
        return Ok(data);
    }

    /// <summary>
    /// Get processing node
    /// </summary>
    /// <param name="uid">The UID of the processing node</param>
    /// <returns>The processing node instance</returns>
    [HttpGet("{uid}")]
    public Task<ProcessingNode?> Get(Guid uid) 
        => ServiceLoader.Load<NodeService>().GetByUidAsync(uid);

    /// <summary>
    /// Saves a processing node
    /// </summary>
    /// <param name="node">The node to save</param>
    /// <returns>The saved instance</returns>
    [HttpPost]
    public async Task<IActionResult> Save([FromBody] ProcessingNode node)
    {
        // see if we are updating the internal node
        var service = ServiceLoader.Load<NodeService>();
        if (node.PreExecuteScript == Guid.Empty)
            node.PreExecuteScript = null; // null it out
        if (node.ProcessingOrder != null && (int)node.ProcessingOrder == 1000)
            node.ProcessingOrder = null;
        
        if(node.Libraries?.Any() == true)
        {
            // remove any removed libraries and update any names
            var libraries = (await ServiceLoader.Load<LibraryService>().GetAllAsync()).ToDictionary(x => x.Uid, x => x.Name);
            node.Libraries = node.Libraries.Where(x => libraries.ContainsKey(x.Uid)).Select(x => new ObjectReference
            {
                Uid = x.Uid,
                Name = libraries[x.Uid],
                Type = typeof(Library).FullName!
            }).DistinctBy(x => x.Uid).ToList();
        }
        
        var nodeService = ServiceLoader.Load<NodeService>();

        if(node.Uid == CommonVariables.InternalNodeUid)
        {
            Logger.Instance.ILog("Updating internal processing node");
            var internalNode = (await GetAll()).FirstOrDefault(x => x.Uid == CommonVariables.InternalNodeUid);
            if(internalNode != null)
            {
                internalNode.Schedule = node.Schedule;
                internalNode.DisableSchedule = node.DisableSchedule;
                internalNode.FlowRunners = node.FlowRunners;
                internalNode.Enabled = node.Enabled;
                internalNode.Priority = node.Priority;
                internalNode.TempPath = node.TempPath;
                internalNode.Icon = node.Icon;
                internalNode.DontChangeOwner = node.DontChangeOwner;
                internalNode.DontSetPermissions = node.DontSetPermissions;
                internalNode.PermissionsFiles = node.PermissionsFiles;
                internalNode.PermissionsFolders = node.PermissionsFolders;
                internalNode.AllLibraries = node.AllLibraries;
                internalNode.MaxFileSizeMb = node.MaxFileSizeMb;
                internalNode.Variables = node.Variables ?? new();
                internalNode.PreExecuteScript = node.PreExecuteScript;
                internalNode.ProcessingOrder = node.ProcessingOrder;
                
                internalNode.Libraries = node.Libraries ?? [];
                internalNode = await service.Update(internalNode, await GetAuditDetails());
                await CheckLicensedNodes(internalNode.Uid, internalNode.Enabled);
                _ = nodeService?.UpdateNodeStatusSummaries();
                await RevisionIncrement();
                return Ok(internalNode);
            }
            
            // internal but doesnt exist
            Logger.Instance.ILog("Internal processing node does not exist, creating.");
            node.Address = CommonVariables.InternalNodeName;
            node.Name = CommonVariables.InternalNodeName;
            node.AllLibraries = ProcessingLibraries.All;
            node.Mappings = []; // no mappings for internal
            node.Variables ??= new();
            node = await service.Update(node, await GetAuditDetails());
            await CheckLicensedNodes(node.Uid, node.Enabled);
            await RevisionIncrement();
            _ = nodeService?.UpdateNodeStatusSummaries();
            return Ok(node);
        }
        else
        {
            Logger.Instance.ILog("Updating external processing node: " + node.Name);
            var existing = await service.GetByUidAsync(node.Uid);
            if (existing == null)
                return BadRequest("Node not found");
            node.Variables ??= new();
            node = await service.Update(node, await GetAuditDetails());
            Logger.Instance.ILog("Updated external processing node: " + node.Name);
            await CheckLicensedNodes(node.Uid, node.Enabled);
            await RevisionIncrement();
            _ = nodeService.UpdateNodeStatusSummaries();
            return Ok(node);
        }
    }
    
    /// <summary>
    /// Increments the configuration revision
    /// </summary>
    /// <returns>an awaited task</returns>
    private Task RevisionIncrement()
        => ((SettingsService)ServiceLoader.Load<ISettingsService>()).RevisionIncrement();

    /// <summary>
    /// Delete processing nodes from the system
    /// </summary>
    /// <param name="model">A reference model containing UIDs to delete</param>
    /// <returns>an awaited task</returns>
    [HttpDelete]
    public async Task Delete([FromBody] ReferenceModel<Guid> model)
    {
        var internalNode =  (await GetAll())
            .FirstOrDefault(x => x.Address == CommonVariables.InternalNodeName)?.Uid ?? Guid.Empty;
        if (model.Uids.Contains(internalNode))
            throw new Exception("ErrorMessages.CannotDeleteInternalNode");
        var nodeService = ServiceLoader.Load<NodeService>();
        await nodeService.Delete(model.Uids, await GetAuditDetails());
        _ = nodeService?.UpdateNodeStatusSummaries();
    }

    /// <summary>
    /// Set state of a processing node
    /// </summary>
    /// <param name="uid">The UID of the processing node</param>
    /// <param name="enable">Whether or not this node is enabled and will process files</param>
    /// <returns>an awaited task</returns>
    [HttpPut("state/{uid}")]
    public async Task<IActionResult> SetState([FromRoute] Guid uid, [FromQuery] bool? enable)
    {
        var service = ServiceLoader.Load<NodeService>();
        var node = await service.GetByUidAsync(uid);
        if (node == null)
            return BadRequest("Node not found.");
        if (enable != null && node.Enabled != enable.Value)
        {
            node.Enabled = enable.Value;
            node = await service.Update(node, await GetAuditDetails());
        }
        await CheckLicensedNodes(uid, enable == true);
        _ = service.UpdateNodeStatusSummaries();
        return Ok(node);
    }

    /// <summary>
    /// Register a processing node.  If already registered will return existing instance
    /// </summary>
    /// <param name="address">The address of the processing node</param>
    /// <returns>The processing node instance</returns>
    [HttpGet("register")]
    public async Task<ProcessingNode> Register([FromQuery]string address)
    {
        if(string.IsNullOrWhiteSpace(address))
            throw new ArgumentNullException(nameof(address));

        address = address.Trim();
        var service = ServiceLoader.Load<NodeService>();
        var data = await service.GetAllAsync();
        var existing = data.FirstOrDefault(x => x.Address.ToLowerInvariant() == address.ToLowerInvariant());
        if (existing != null)
        {
            existing.SignalrUrl = "flow";
            _ = service.UpdateNodeStatusSummaries();
            return existing;
        }

        // doesnt exist, register a new node.
        var variables = await ServiceLoader.Load<VariableService>().GetAllAsync();
        bool isSystem = address == CommonVariables.InternalNodeName;
        var node = new ProcessingNode
        {
            Name = address,
            Address = address,
            Enabled = isSystem, // default to disabled so they have to configure it first
            FlowRunners = 1,
            AllLibraries = ProcessingLibraries.All,
            Schedule = new string('1', 672),
            Mappings = isSystem
                ? []
                : variables?.Select(x => new
                    KeyValuePair<string, string>(x.Value, string.Empty)
                ).ToList() ?? []
        };
        node = await service.Update(node, await GetAuditDetails());
        node.SignalrUrl = "flow";
        await CheckLicensedNodes(Guid.Empty, false);
        _ = service.UpdateNodeStatusSummaries();
        return node;
    }

    /// <summary>
    /// Ensure the user does not exceed their licensed node count
    /// </summary>
    /// <param name="nodeUid">optional UID of a node that should be checked first</param>
    /// <param name="enabled">optional status of the node state</param>
    private async Task CheckLicensedNodes(Guid nodeUid, bool enabled)
    {
        var licenseService = ServiceLoader.Load<LicenseService>();
        var licensedNodes = licenseService.GetLicensedProcessingNodes();
        var service = ServiceLoader.Load<NodeService>();
        var nodes = await service.GetAllAsync();
        int current = 0;
        foreach (var node in nodes.OrderBy(x => x.Uid == nodeUid ? 1 : 2).ThenBy(x => x.Name))
        {
            if (node.Uid == nodeUid && enabled != node.Enabled)
            {
                Logger.Instance.ILog($"Changing processing node '{node.Name}' state from '{node.Enabled}' to '{enabled}'");
                node.Enabled = enabled;
                var result = await service.Update(node, await GetAuditDetails());
                if (result.Failed(out string error))
                    Logger.Instance.ELog($"Failed updating node '{node.Name}': {error}");
            }

            if (node.Enabled)
            {
                if (current >= licensedNodes)
                {
                    node.Enabled = false;
                    await service.Update(node, await GetAuditDetails());
                    Logger.Instance.ILog($"Disabled processing node '{node.Name}' due to license restriction");
                }
                else
                {
                    ++current;
                }
            }
        }
    }
}

