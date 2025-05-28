using System.Dynamic;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace FileFlows.WebServer.Controllers;
/// <summary>
/// Controller for Flows
/// </summary>
[Route("/api/flow")]
[FileFlowsAuthorize(UserRole.Flows)]
public class FlowController : BaseController
{
    const int DEFAULT_XPOS = 450;
    const int DEFAULT_YPOS = 50;
    
    private static bool? _HasFlows;
    /// <summary>
    /// Gets if there are any flows
    /// </summary>
    internal static bool HasFlows
    {
        get
        {
            if (_HasFlows == null)
                UpdateHasFlows().Wait();
            return _HasFlows == true;
        }
    }
    
    /// <summary>
    /// Get all flows in the system
    /// </summary>
    /// <returns>all flows in the system</returns>
    [HttpGet]
    public async Task<IEnumerable<Flow>> GetAll() => 
        (await ServiceLoader.Load<FlowService>().GetAllAsync()).OrderBy(x => x.Name.ToLowerInvariant());


    [HttpGet("list-all")]
    public async Task<IEnumerable<FlowListModel>> ListAll()
    {
        var flows = await ServiceLoader.Load<FlowService>().GetAllAsync();
        List<FlowListModel> list = new List<FlowListModel>();

        foreach(var item in flows)
        {
            list.Add(new FlowListModel
            {
                Default = item.Default,
                Name = item.Name,
                Type = item.Type,
                Uid = item.Uid,
                ReadOnly = item.ReadOnly,
                Icon = item.Icon,
                Description = item.Description
            });
        }
        var dictFlows  = list.ToDictionary(x => x.Uid, x => x);
        
        string flowTypeName = typeof(Flow).FullName ?? string.Empty;
        foreach (var flow in flows)
        {
            if (flow?.Parts?.Any() != true)
                continue;
            foreach (var p in flow.Parts)
            {
                if (p.Model == null || p.FlowElementUid != "FileFlows.BasicNodes.Functions.GotoFlow")
                    continue;
                try
                {
                    var gotoModel = JsonSerializer.Deserialize<GotoFlowModel>(JsonSerializer.Serialize(p.Model), new JsonSerializerOptions()
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (gotoModel?.Flow == null || dictFlows.ContainsKey(gotoModel.Flow.Uid) == false)
                        continue;
                    var dictFlow = dictFlows[gotoModel.Flow.Uid];
                    dictFlow.UsedBy ??= new();
                    if (dictFlow.UsedBy.Any(x => x.Uid == flow.Uid))
                        continue;
                    dictFlow.UsedBy.Add(new()
                    {
                        Name = flow.Name,
                        Type = flowTypeName,
                        Uid = flow.Uid
                    });
                }
                catch (Exception)
                {
                    // Ignored
                }
            }
        }

        string libTypeName = typeof(Library).FullName ?? string.Empty;
        var libraries = await ServiceLoader.Load<LibraryService>().GetAllAsync();
        foreach (var lib in libraries)
        {
            if (lib.Flow == null)
                continue;
            if (dictFlows.ContainsKey(lib.Flow.Uid) == false)
                continue;
            var dictFlow = dictFlows[lib.Flow.Uid];
            if (dictFlow.UsedBy != null && dictFlow.UsedBy.Any(x => x.Uid == lib.Uid))
                continue;
            dictFlow.UsedBy ??= new();
            dictFlow.UsedBy.Add(new()
            {
                Name = lib.Name,
                Type = libTypeName,
                Uid = lib.Uid
            });
        }
        
        return list.OrderBy(x => x.Name.ToLowerInvariant());
    }

    private class GotoFlowModel
    {
        public ObjectReference Flow { get; set; } = null!;
    }

    /// <summary>
    /// Gets the failure flow for a particular library
    /// </summary>
    /// <param name="libraryUid">the UID of the library</param>
    /// <returns>the failure flow</returns>
    [HttpGet("failure-flow/by-library/{libraryUid}")]
    public Task<Flow?> GetFailureFlow([FromRoute] Guid libraryUid)
        => ServiceLoader.Load<FlowService>().GetFailureFlow(libraryUid);

    /// <summary>
    /// Exports a flows
    /// </summary>
    /// <param name="uids">The Flow UIDs</param>
    /// <returns>A download response of the flow(s)</returns>
    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery(Name = "uid")] Guid[] uids)
    {
        var service = ServiceLoader.Load<FlowService>();
        var allFlows = await service.GetAllAsync();
        var flows = allFlows.Where(flow => uids.Contains(flow.Uid)).ToList();
        
        if (flows.Any() == false)
            return NotFound();

        var subFlows = allFlows.Where(x => x.Type == FlowType.SubFlow).ToList();
            
        if (flows.Count() == 1)
        {
            var flow = flows[0];
            string json = CreateExportJson(flow, subFlows);
            byte[] data = System.Text.Encoding.UTF8.GetBytes(json);
            return File(data, "application/octet-stream", flow.Name + ".json");
        }
        
        // multiple, send a zip
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true)) // Dispose explicitly
        {
            foreach (var flow in flows)
            {
                string json = CreateExportJson(flow, subFlows);
                var fe = zip.CreateEntry(flow.Name + ".json");

                await using var entryStream = fe.Open();
                await using var streamWriter = new StreamWriter(entryStream);
                await streamWriter.WriteAsync(json);
                await streamWriter.FlushAsync(); // Ensure all data is written
            }
        }

        // Reset stream position AFTER disposing ZipArchive
        ms.Seek(0, SeekOrigin.Begin);
        return File(ms.ToArray(), "application/octet-stream", "Flows.zip");

    }

    private string CreateExportJson(Flow flow, List<Flow> subFlows)
    {
        var dependencies = new List<Guid>();
        LoadSubFlows(dependencies, flow, subFlows);
        string json = JsonSerializer.Serialize(new
        {
            flow.Name,
            Uid = flow.Type == FlowType.SubFlow ? (object)flow.Uid : null,
            flow.Type,
            Revision = Math.Max(1, flow.Revision),
            flow.Description,
            flow.Icon,
            flow.Fields,
            flow.FileDropOptions,
            Properties = new
            {
                // flow.Properties.Tags,
                // Author = flow.Properties.Author?.EmptyAsNull(),
                // MinimumVersion = flow.Properties.MinimumVersion?.EmptyAsNull(),
                flow.Properties.Fields,
                flow.Properties.Variables,
                flow.Properties.Outputs
            },
            SubFlows = dependencies.Any() ? dependencies : null,
            flow.Parts
        }, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true,
        });
        return json;
    }

    private void LoadSubFlows(List<Guid> list, Flow flow, List<Flow> subflows)
    {
        var sfParts = flow.Parts.Where(x => x.FlowElementUid.StartsWith("SubFlow:"))
            .Select(x => Guid.Parse(x.FlowElementUid.Split(':')[1])).ToArray();

        foreach (var uid in sfParts)
        {
            if (list.Contains(uid))
                continue;
            var sf = subflows.FirstOrDefault(x => x.Uid == uid);
            if (sf == null)
                continue;
            
            list.Add(uid);
            LoadSubFlows(list, sf, subflows);
        }

    }

    /// <summary>
    /// Imports a flow
    /// </summary>
    /// <param name="json">The json data to import</param>
    /// <param name="asFileDropFlow">If the flow is duplicated as a FileDrop flow</param>
    /// <returns>The newly import flow</returns>
    [HttpPost("import")]
    public async Task<Flow> Import([FromBody] string json, [FromQuery] bool asFileDropFlow = false)
    {
        Flow? flow = JsonSerializer.Deserialize<Flow>(json);
        if (flow == null)
            throw new ArgumentNullException(nameof(flow));
        if (flow.Parts == null || flow.Parts.Count == 0)
            throw new ArgumentException(nameof(flow.Parts));

        // generate new UIDs for each part
        foreach (var part in flow.Parts)
        {
            Guid newGuid = Guid.NewGuid();
            json = json.Replace(part.Uid.ToString(), newGuid.ToString());
        }

        // reparse with new UIDs
        var service = ServiceLoader.Load<FlowService>();
        flow = JsonSerializer.Deserialize<Flow>(json)!;
        if(flow.Type != FlowType.SubFlow || await service.UidInUse(flow.Uid))
            flow.Uid = Guid.Empty;
        
        if (asFileDropFlow && LicenseService.IsLicensed(LicenseFlags.FileDrop) && flow.Type == FlowType.Standard)
            flow.Type = FlowType.FileDrop;
        
        flow.ReadOnly = false;
        flow.Default = false;
        flow.DateModified = DateTime.UtcNow;
        flow.DateCreated = DateTime.UtcNow;
        flow.Name = await service.GetNewUniqueName(flow.Name);
        return await service.Update(flow, await GetAuditDetails());
    }


    /// <summary>
    /// Duplicates a flow
    /// </summary>
    /// <param name="uid">The UID of the flow</param>
    /// <param name="asFileDropFlow">If the flow is duplicated as a FileDrop flow</param>
    /// <returns>The duplicated flow</returns>
    [HttpGet("duplicate/{uid}")]
    public async Task<Flow?> Duplicate([FromRoute] Guid uid, [FromQuery] bool asFileDropFlow = false)
    { 
        var flow = await ServiceLoader.Load<FlowService>().GetByUidAsync(uid);
        if (flow == null)
            return null;
        
        string json = JsonSerializer.Serialize(flow, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        
        return await Import(json, asFileDropFlow);
    }

    /// <summary>
    /// Sets the enabled state of a flow
    /// </summary>
    /// <param name="uid">The flow UID</param>
    /// <param name="enable">Whether or not the flow should be enabled</param>
    /// <returns>The updated flow</returns>
    [HttpPut("state/{uid}")]
    public async Task<Flow> SetState([FromRoute] Guid uid, [FromQuery] bool? enable)
    {
        var service = ServiceLoader.Load<FlowService>();
        var flow = await service.GetByUidAsync(uid);
        if (flow == null)
            throw new Exception("Flow not found.");
        if (enable != null)
        {
            flow.Enabled = enable.Value;
            flow = await service.Update(flow, await GetAuditDetails());
        }

        return flow;
    }

    /// <summary>
    /// Sets the default state of a flow
    /// </summary>
    /// <param name="uid">The flow UID</param>
    /// <param name="isDefault">Whether or not the flow should be the default</param>
    [HttpPut("set-default/{uid}")]
    public async Task SetDefault([FromRoute] Guid uid, [FromQuery(Name = "default")] bool isDefault = true)
    {
        var service = ServiceLoader.Load<FlowService>();
        var flow = await service.GetByUidAsync(uid);
        if (flow == null)
            throw new Exception("Flow not found.");
        if(flow.Type != FlowType.Failure)
            throw new Exception("Flow not a failure flow.");

        if (isDefault)
        {
            // make sure no others are defaults
            var others = (await service.GetAllAsync()).Where(x => x.Type == FlowType.Failure && x.Default && x.Uid != uid).ToList();
            foreach (var other in others)
            {
                other.Default = false;
                await service.Update(other, await GetAuditDetails());
            }
        }

        if (isDefault == flow.Default)
            return;

        flow.Default = isDefault;
        await service.Update(flow, await GetAuditDetails());
    }
    /// <summary>
    /// Delete flows from the system
    /// </summary>
    /// <param name="model">A reference model containing UIDs to delete</param>
    /// <returns>an awaited task</returns>
    [HttpDelete]
    public async Task Delete([FromBody] ReferenceModel<Guid> model)
    {
        if (model?.Uids?.Any() != true)
            return; // nothing to delete
        await ServiceLoader.Load<FlowService>().Delete(model.Uids, await GetAuditDetails());
        await UpdateHasFlows();
    }

    private static async Task UpdateHasFlows()
        => _HasFlows = await ServiceLoader.Load<FlowService>().HasAny();


    /// <summary>
    /// Get a flow
    /// </summary>
    /// <param name="uid">The Flow UID</param>
    /// <returns>The flow instance</returns>
    [HttpGet("{uid}")]
    public async Task<Flow?> Get(Guid uid)
    {
        if (uid != Guid.Empty)
        {
            var flow = await ServiceLoader.Load<FlowService>().GetByUidAsync(uid);
            if (flow == null)
                return flow;

            var elements = await GetElements(uid);

            var scripts = (await ServiceLoader.Load<ScriptService>().GetAll()).ToDictionary(x => x.Uid, x => x.Name);
            var flows = (await ServiceLoader.Load<FlowService>().GetAllAsync()).ToDictionary(x => x.Uid.ToString(), x => x.Name);
            foreach (var p in flow.Parts)
            {
                if (p.Type == FlowElementType.Script && string.IsNullOrWhiteSpace(p.Name))
                {
                    string sScriptUid = p.FlowElementUid[7..];
                    // set the name to the script name
                    if (Guid.TryParse(sScriptUid, out var scriptUid) &&
                        scripts.TryGetValue(scriptUid, out var scriptName) &&
                        string.IsNullOrWhiteSpace(scriptName) == false)
                        p.Name = scriptName;
                    else
                        p.Name = "Missing Script";
                }
                else if (p.Type == FlowElementType.SubFlow && string.IsNullOrWhiteSpace(p.Name))
                {
                    string feName = p.FlowElementUid[8..]; // remove SubFlow:
                    if (flows.TryGetValue(feName, out string? subflow) && string.IsNullOrWhiteSpace(subflow) == false)
                        p.Name = subflow;
                    else
                        p.Name = "Missing Sub Flow";
                }
                
                LoadFlowPartValues(p, elements);
            }

            return flow;
        }
        else
        {
            // create default flow
            var flowNames = (await ServiceLoader.Load<FlowService>().GetAllAsync()).Select(x => x.Name).ToList();
            Flow flow = new Flow();
            flow.Parts = new();
            flow.Name = "New Flow";
            flow.Enabled = true;
            int count = 0;
            while (flowNames.Contains(flow.Name))
            {
                flow.Name = "New Flow " + (++count);
            }

            // try find basic node
            var elements = await GetElements(uid);
            var info = elements.FirstOrDefault(x => x.Uid == "FileFlows.BasicNodes.File.InputFile");
            if (info != null && string.IsNullOrEmpty(info.Name) == false)
            {
                flow.Parts.Add(new FlowPart
                {
                    Name = "InputFile",
                    xPos = DEFAULT_XPOS,
                    yPos = DEFAULT_YPOS,
                    Uid = Guid.NewGuid(),
                    Type = FlowElementType.Input,
                    Outputs = 1,
                    FlowElementUid = info.Name,
                    Icon = "far fa-file"
                });
            }

            return flow;
        }
    }


    private void LoadFlowPartValues(FlowPart p, FlowElement[] elements)
    {
        if (p.FlowElementUid.EndsWith("." + p.Name))
            p.Name = string.Empty;
        p.CustomColor = null; // clear it
        p.ReadOnly = false;

        p.Label = Translater.TranslateIfHasTranslation(
            $"Flow.Parts.{p.FlowElementUid[(p.FlowElementUid.LastIndexOf(".", StringComparison.Ordinal) + 1)..]}.Label",
            string.Empty);
        
        var element = elements?.FirstOrDefault(x => x.Uid == p.FlowElementUid);
        if (element == null)
            return;
        if (string.IsNullOrEmpty(element.Icon) == false)
            p.Icon = element.Icon;
        if (string.IsNullOrEmpty(element.CustomColor) == false)
            p.CustomColor = element.CustomColor;
        p.ReadOnly = element.ReadOnly;
        if (p.ReadOnly == false)
        {
            if (element.Uid == "SubFlowOutput" && p.Name.StartsWith("Output "))
                p.ReadOnly = true;
            else if (element.Uid == "SubFlowInput")
                p.ReadOnly = true;
        }
        p.Icon = element.Icon;
        p.Inputs = element.Inputs;
        if((element.Uid.EndsWith(".Random") || element.Uid.EndsWith(".IfString") || element.Uid.EndsWith(".Function") 
            || element.Uid.StartsWith("FileFlows.BasicNodes.Scripting.")   ) == false)
            p.Outputs = element.Outputs;
        
    }

    /// <summary>
    /// Gets all nodes in the system
    /// </summary>
    /// <param name="flowUid">the UID of the flow to get elements for</param>
    /// <param name="type">the type of flow to get flow elements for</param>
    /// <returns>Returns a list of all the nodes in the system</returns>
    [HttpGet("elements")]
    public Task<FlowElement[]> GetElements([FromQuery] Guid flowUid, [FromQuery]FlowType? type = null)
        => ServiceLoader.Load<FlowElementService>().GetFlowElements(flowUid, type);

    
    /// <summary>
    /// Saves a flow
    /// </summary>
    /// <param name="model">The flow being saved</param>
    /// <param name="uniqueName">Whether or not a new unique name should be generated if the name already exists</param>
    /// <returns>The saved flow</returns>
    [HttpPut]
    public async Task<Flow> Save([FromBody] Flow model, [FromQuery] bool uniqueName = false)
    {
        if (model == null)
            throw new Exception("No model");

        if (string.IsNullOrWhiteSpace(model.Name))
            throw new Exception("ErrorMessages.NameRequired");

        if (model.Type == FlowType.FileDrop)
        {
            if(model.Name.StartsWith("FileDrop: ") == false)
                model.Name = "FileDrop: " + model.Name;
        }
        
        var service = ServiceLoader.Load<FlowService>();
        model.Name = model.Name.Trim();
        model.Revision++;
        if (uniqueName == false)
        {
            bool inUse = await service.NameInUse(model.Uid, model.Name);
            if (inUse)
                throw new Exception("ErrorMessages.NameInUse");
        }
        else
        {
            model.Name = await service.GetNewUniqueName(model.Name);
        }

        if (model.Parts?.Any() != true)
            throw new Exception("Flow.ErrorMessages.NoParts");

        foreach (var p in model.Parts)
        {
            if (Guid.TryParse(p.Name, out _))
                p.Name = string.Empty; // fixes issue with Scripts being saved as the Guids
            if (string.IsNullOrEmpty(p.Name))
                continue;
            if (p.FlowElementUid.ToLower().EndsWith("." + p.Name.Replace(" ", "").ToLower()))
                p.Name = string.Empty; // fixes issue with flow part being named after the display
        }

        int inputNodes = model.Parts.Count(x => x.Type == FlowElementType.Input || x.Type == FlowElementType.Failure);
        if (inputNodes == 0)
            throw new Exception("Flow.ErrorMessages.NoInput");
        if (inputNodes > 1)
            throw new Exception("Flow.ErrorMessages.TooManyInputNodes");

        if (model.Uid == Guid.Empty && model.Type == FlowType.Failure)
        {
            // if first failure flow make it default
            var others = (await service.GetAllAsync()).Count(x => x.Type == FlowType.Failure);
            if (others == 0)
                model.Default = true;
        }

        bool nameChanged = false;
        if (model.Uid != Guid.Empty)
        {
            // existing, check for name change
            var existing = await service.GetByUidAsync(model.Uid);
            nameChanged = existing != null && existing.Name != model.Name;
        }
        
        Logger.Instance.ILog($"Saving Flow '{model.Name}'");

        model = await service.Update(model, await GetAuditDetails());
        if(nameChanged)
            _ = ServiceLoader.Load<IObjectReferenceUpdater>().RunUpdate();

        await SaveToDisk(model);

        return (await Get(model.Uid))!;
    }
    
    /// <summary>
    /// Saves the flow to disk
    /// </summary>
    /// <param name="flow">the flow to save</param>
    private async Task SaveToDisk(Flow flow)
    {
        try
        {
            var service = ServiceLoader.Load<FlowService>();
            var allFlows = await service.GetAllAsync();
            var subFlows = allFlows.Where(x => x.Type == FlowType.SubFlow).ToList();
            string json = CreateExportJson(flow, subFlows);
            var dir = Path.Combine(DirectoryHelper.ConfigDirectory, "Flows");
            if (Directory.Exists(dir) == false)
                Directory.CreateDirectory(dir);

            var file = Path.Combine(dir, SanitizeFileName(flow.Name) + ".json");
            await System.IO.File.WriteAllTextAsync(file, json);
        }
        catch (Exception ex)
        {
            Logger.Instance.WLog("Failed to save flow to disk: " + ex.Message);
        }
    }
    
    /// <summary>
    /// Saves the flow to disk.
    /// </summary>
    private string SanitizeFileName(string fileName)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(c.ToString(), string.Empty);
        }
        return fileName.Replace("..", string.Empty);
    }
    
    /// <summary>
    /// Rename a flow
    /// </summary>
    /// <param name="uid">The Flow UID</param>
    /// <param name="name">The new name</param>
    /// <returns>an awaited task</returns>
    [HttpPut("{uid}/rename")]
    public async Task Rename([FromRoute] Guid uid, [FromQuery] string name)
    {
        if (uid == Guid.Empty)
            return; // renaming a new flow

        var service = ServiceLoader.Load<FlowService>();
        var flow = await service.GetByUidAsync(uid);
        if (flow == null)
            throw new Exception("Flow not found");
        if (flow.Name == name)
            return; // name already is the requested name

        flow.Name = name;
        flow = await service.Update(flow, await GetAuditDetails());

        // update any object references
        var lfService = ServiceLoader.Load<LibraryFileService>();
        await lfService.UpdateFlowName(flow.Uid, flow.Name);
        await lfService.UpdateFlowName(flow.Uid, flow.Name);
    }

    /// <summary>
    /// Get variables for flow parts
    /// </summary>
    /// <param name="flowParts">The flow parts</param>
    /// <param name="partUid">The specific part UID</param>
    /// <param name="isNew">If the flow part is a new part</param>
    /// <returns>The available variables for the flow part</returns>
    [HttpPost("{uid}/variables")]
    public async Task<Dictionary<string, object>> GetVariables([FromBody] List<FlowPart> flowParts,
        [FromRoute(Name = "uid")] Guid partUid, [FromQuery] bool isNew = false)
    {
        var variables = new Dictionary<string, object>();
        bool windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        bool dir = flowParts?.Any(x => x.FlowElementUid.EndsWith("InputDirectory")) == true;
        variables.Add("FlowName", "My Flow");

        if (dir)
        {
            variables.Add("folder.Name", "FolderName");
            variables["folder.OriginalName"] = "/original/file/on/server.txt";
            variables.Add("folder.FullName", windows ? @"C:\Folder\SubFolder" : "/folder/subfolder");
            variables.Add("folder.Date", DateTime.UtcNow);
            variables.Add("folder.Date.Day", DateTime.UtcNow.Day);
            variables.Add("folder.Date.Month", DateTime.UtcNow.Month);
            variables.Add("folder.Date.Year", DateTime.UtcNow.Year);
            variables.Add("folder.OrigName", "FolderOriginalName");
            variables.Add("folder.OrigFullName",
                windows ? @"C:\OriginalFolder\SubFolder" : "/originalFolder/subfolder");
        }
        else
        {
            variables.Add("ext", ".mkv");
            variables["file.OriginalName"] = "/original/file/on/server.txt";
            variables.Add("file.Name", "Filename.ext");
            variables.Add("file.NameNoExtension", "Filename");
            variables.Add("file.Extension", ".mkv");
            variables.Add("file.Size", 1000);
            variables.Add("file.FullName",
                windows ? @"C:\Folder\temp\randomfile.ext" : "/media/temp/randomfile.ext");
            variables.Add("file.Orig.Extension", ".mkv");
            variables.Add("file.Orig.FileName", "OriginalFile.ext");
            variables.Add("file.Orig.RelativeName", "files/filename.ext");
            variables.Add("file.Orig.FileNameNoExtension", "OriginalFile");
            variables.Add("file.Orig.FullName",
                windows ? @"C:\Folder\files\filename.ext" : "/media/files/filename.ext");
            variables.Add("file.Orig.Size", 1000);

            variables.Add("file.Create", DateTime.UtcNow);
            variables.Add("file.Create.Day", DateTime.UtcNow.Day);
            variables.Add("file.Create.Month", DateTime.UtcNow.Month);
            variables.Add("file.Create.Year", DateTime.UtcNow.Year);
            variables.Add("file.Modified", DateTime.UtcNow);
            variables.Add("file.Modified.Day", DateTime.UtcNow.Day);
            variables.Add("file.Modified.Month", DateTime.UtcNow.Month);
            variables.Add("file.Modified.Year", DateTime.UtcNow.Year);

            variables.Add("folder.Name", "FolderName");
            variables.Add("folder.FullName", windows ? @"C:\Folder\SubFolder" : "/folder/subfolder");
            variables.Add("folder.Orig.Name", "FolderOriginalName");
            variables.Add("folder.Orig.FullName",
                windows ? @"C:\OriginalFolder\SubFolder" : "/originalFolder/subfolder");
        }
        variables.Add("folder.Size", 10000);
        variables.Add("folder.Orig.Size", 10000);
        variables["library.Name"] = "My Library";
        variables["library.Path"] = "/library/path";
        variables["temp"] = "/node-temp";
        variables["time.processing"] = new TimeSpan(1, 2, 3).ToString();
        variables["time.now"] = DateTime.Now.ToShortTimeString();

        var resources = await ServiceLoader.Load<ResourceService>().GetAllAsync();
        foreach (var resource in resources)
        {
            variables[$"resource.{resource.Name}"] = resource.Name;
        }

        //p.FlowElementUid == FileFlows.VideoNodes.DetectBlackBars
        var flowElements = await GetElements(Guid.Empty, (FlowType)(-1));
        flowElements ??= new FlowElement[] { };
        var dictFlowElements = flowElements.ToDictionary(x => x.Uid, x => x);

        if (isNew)
        {
            // we add all variables on new, so they can hook up a connection easily
            foreach (var p in flowParts ?? new List<FlowPart>())
            {
                if (dictFlowElements.ContainsKey(p.FlowElementUid) == false)
                    continue;
                var partVariables = dictFlowElements[p.FlowElementUid].Variables ??
                                    new Dictionary<string, object>();
                foreach (var pv in partVariables)
                {
                    if (variables.ContainsKey(pv.Key) == false)
                        variables.Add(pv.Key, pv.Value);
                }
            }

            return variables;
        }

        // get the connected nodes to this part
        var part = flowParts?.Where(x => x.Uid == partUid)?.FirstOrDefault();
        if (part == null)
            return variables;

        List<FlowPart> checkedParts = new List<FlowPart>();

        var parentParts = FindParts(part, 0);
        if (parentParts.Any() == false)
            return variables;

        foreach (var p in parentParts)
        {
            if (dictFlowElements.ContainsKey(p.FlowElementUid) == false)
                continue;

            var partVariables = dictFlowElements[p.FlowElementUid].Variables ?? new Dictionary<string, object>();
            foreach (var pv in partVariables)
            {
                if (variables.ContainsKey(pv.Key) == false)
                    variables.Add(pv.Key, pv.Value);
            }
        }

        return variables;

        List<FlowPart> FindParts(FlowPart partInner, int depth)
        {
            List<FlowPart> results = new List<FlowPart>();
            if (depth > 30)
                return results; // prevent infinite recursion

            foreach (var p in flowParts ?? new List<FlowPart>())
            {
                if (checkedParts.Contains(p) || p == partInner)
                    continue;

                if (p.OutputConnections?.Any() != true)
                {
                    checkedParts.Add(p);
                    continue;
                }

                if (p.OutputConnections.Any(x => x.InputNode == partInner.Uid))
                {
                    results.Add(p);
                    if (checkedParts.Contains(p))
                        continue;
                    checkedParts.Add(p);
                    results.AddRange(FindParts(p, ++depth));
                }
            }

            return results;
        }
    }
}