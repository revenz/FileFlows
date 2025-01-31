using FileFlows.ServerModels;

namespace FileFlows.WebServer.Controllers;

/// <summary>
/// Library controller
/// </summary>
[Route("/api/library")]
[FileFlowsAuthorize(UserRole.Libraries)]
public class LibraryController : BaseController
{
    private static bool? _HasLibraries;
    /// <summary>
    /// Gets if there are any libraries
    /// </summary>
    internal static bool HasLibraries
    {
        get
        {
            if (_HasLibraries == null)
                UpdateHasLibraries().Wait();
            return _HasLibraries == true;
        }
    }
    private static async Task UpdateHasLibraries()
        => _HasLibraries = await ServiceLoader.Load<LibraryService>().HasAny();

    /// <summary>
    /// Gets all libraries in the system
    /// </summary>
    /// <returns>a list of all libraries</returns>
    [HttpGet]
    public async Task<IEnumerable<Library>> GetAll() 
        => (await ServiceLoader.Load<LibraryService>().GetAllAsync()).OrderBy(x => x.Name.ToLowerInvariant());


    /// <summary>
    /// Basic library list
    /// </summary>
    /// <returns>library list</returns>
    [HttpGet("basic-list")]
    [FileFlowsAuthorize(UserRole.Files | UserRole.Nodes | UserRole.Reports | UserRole.Flows)]
    public async Task<Dictionary<Guid, string>> GetLibraryList()
    {
        var items = await new LibraryService().GetAllAsync();
        return items.ToDictionary(x => x.Uid, x => x.Name);
    }

    /// <summary>
    /// Get a library
    /// </summary>
    /// <param name="uid">The UID of the library</param>
    /// <returns>the library instance</returns>
    [HttpGet("{uid}")]
    public Task<Library?> Get(Guid uid) =>
        ServiceLoader.Load<LibraryService>().GetByUidAsync(uid);

    /// <summary>
    /// Duplicates a library
    /// </summary>
    /// <param name="uid">The UID of the library</param>
    /// <returns>The duplicated library</returns>
    [HttpPost("duplicate/{uid}")]
    public async Task<IActionResult?> Duplicate([FromRoute] Guid uid)
    {
        if (uid == CommonVariables.ManualLibraryUid)
            return null;
        
        var service = ServiceLoader.Load<LibraryService>();
        var library = await service.GetByUidAsync(uid);
        if (library == null)
            return null;
        
        string json = JsonSerializer.Serialize(library, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        library = JsonSerializer.Deserialize<Library>(json);
        if (library == null)
            return null;
        library.Uid = Guid.Empty;
        library.Name = await service.GetNewUniqueName(library.Name);
        library.LastScanned = DateTime.MinValue;
        library.Enabled = false;
        return await Save(library);
    }
    
    /// <summary>
    /// Saves a library
    /// </summary>
    /// <param name="library">The library to save</param>
    /// <returns>the saved library instance</returns>
    [HttpPost]
    public async Task<IActionResult> Save([FromBody] Library library)
    {
        if(library == null)
            return BadRequest("No data provided");

        if (library.Uid != CommonVariables.ManualLibraryUid)
        {
            if (library.Flow == null)
                return BadRequest("ErrorMessages.NoFlowSpecified");
            if (library.Uid == Guid.Empty)
                library.LastScanned = DateTime.MinValue; // never scanned
            if (Regex.IsMatch(library.Schedule, "^[01]{672}$") == false)
                library.Schedule = new string('1', 672);
        }
        else
        {
            // ensure these are correctly set
            library.Schedule = new string('1', 672);
            library.Enabled = true;
            library.Name = CommonVariables.ManualLibrary;
            library.Folders = false;
        }

        var service = ServiceLoader.Load<LibraryService>();
        bool nameUpdated = false;
        if (library.Uid != Guid.Empty)
        {
            // existing, check for name change
            var existing = await service.GetByUidAsync(library.Uid);
            nameUpdated = existing != null && existing.Name != library.Name;
        }
        
        bool newLib = library.Uid == Guid.Empty;
        var result  = await service.Update(library, await GetAuditDetails());
        if (result.Failed(out string error))
            return BadRequest(error);

        library = result.Value;
        
        _ = Task.Run(async () =>
        {
            await Task.Delay(1);
            if (nameUpdated)
                await ServiceLoader.Load<IObjectReferenceUpdater>().RunUpdate();

            //RefreshCaches();

            if (newLib)
                Rescan(new() { Uids = new[] { library.Uid } });
        });
        
        return Ok(library);
    }
    
    // /// <summary>
    // /// Refresh the caches where libraries are stored in memory
    // /// </summary>
    // private void RefreshCaches()
    // {
    //     LibraryWorker.UpdateLibraries();
    // }

    /// <summary>
    /// Set the enable state for a library
    /// </summary>
    /// <param name="uid">The UID of the library</param>
    /// <param name="enable">true if enabled, otherwise false</param>
    /// <returns>the updated library instance</returns>
    [HttpPut("state/{uid}")]
    public async Task<Library> SetState([FromRoute] Guid uid, [FromQuery] bool enable)
    {
        var service = ServiceLoader.Load<LibraryService>();
        var library = await service.GetByUidAsync(uid);
        if (library == null)
            throw new Exception("Library not found.");
        
        if (library.Enabled != enable)
        {
            library.Enabled = enable;
            library = await service.Update(library, await GetAuditDetails());
            //RefreshCaches();
        }
        return library;
    }

    /// <summary>
    /// Delete libraries from the system
    /// </summary>
    /// <param name="model">A reference model containing UIDs to delete</param>
    /// <returns>an awaited task,</returns>
    [HttpDelete]
    public async Task Delete([FromBody] ReferenceModel<Guid> model)
    {
        if (model?.Uids?.Any() != true)
            return;
        // delete the files first
        await ServiceLoader.Load<LibraryFileService>().DeleteByLibrary(model.Uids);
        // then delete the libraries
        await ServiceLoader.Load<LibraryService>().Delete(model.Uids, await GetAuditDetails());

        await UpdateHasLibraries();
        //RefreshCaches();
    }

    /// <summary>
    /// Rescans libraries
    /// </summary>
    /// <param name="model">A reference model containing UIDs to rescan</param>
    /// <returns>an awaited task</returns>
    [HttpPut("rescan")]
    public void Rescan([FromBody] ReferenceModel<Guid> model)
    {
        var service = ServiceLoader.Load<LibraryService>();
        service.Rescan(model.Uids);
        // foreach(var uid in model.Uids)
        // {
        //     var item = await service.GetByUidAsync(uid);
        //     if (item == null)
        //         continue;
        //     item.LastScanned = DateTime.MinValue;
        //     await service.Update(item, await GetAuditDetails());
        // }
        //
        // _ = Task.Run(async () =>
        // {
        //     await Task.Delay(1);
        //     RefreshCaches();
        //     LibraryWorker.ScanNow();
        // });
    }

    /// <summary>
    /// Reprocess libraries.
    /// All library files will have their status updated to unprocessed.
    /// </summary>
    /// <param name="model">A reference model containing UIDs to reprocessing</param>
    /// <returns>an awaited task</returns>
    [HttpPut("reprocess")]
    public Task Reprocess([FromBody] ReferenceModel<Guid> model)
        => ServiceLoader.Load<LibraryFileService>().ReprocessByLibraryUid(model.Uids);

    /// <summary>
    /// Reset libraries.
    /// All library files will all be removed and the libraries will be rescanned.
    /// </summary>
    /// <param name="model">A reference model containing UIDs to reset</param>
    /// <returns>an awaited task</returns>
    [HttpPut("reset")]
    public async Task Reset([FromBody] ReferenceModel<Guid> model)
    {
        await ServiceLoader.Load<LibraryFileService>().DeleteByLibrary(model.Uids);
        Rescan(model);
        var service = ServiceLoader.Load<IClientService>();
        await service.UpdateFileStatus();
    }

    private FileInfo[] GetTemplateFiles() 
        => new DirectoryInfo(DirectoryHelper.TemplateDirectoryLibrary).GetFiles("*.json", SearchOption.AllDirectories);

    /// <summary>
    /// Gets a list of library templates
    /// </summary>
    /// <returns>a list of library templates</returns>
    [HttpGet("templates")]
    public Dictionary<string, List<Library>> GetTemplates()
    {
        SortedDictionary<string, List<Library>> templates = new(StringComparer.OrdinalIgnoreCase);
        var lstGeneral = new List<Library>();
        Logger.Instance.ILog("Library Templates Directory: " + DirectoryHelper.TemplateDirectoryLibrary);
        string generalGroup = Translater.Instant("Templates.Libraries.Groups.General");
        foreach (var tf in GetTemplateFiles())
        {
            try
            {
                string json = System.IO.File.ReadAllText(tf.FullName);
                if (json.StartsWith("//"))
                    json = string.Join("\n", json.Split('\n').Skip(1)); // remove the //path comment
                json = TemplateHelper.ReplaceMediaWithHomeDirectory(json);
                // json = TemplateHelper.ReplaceWindowsPathIfWindows(json);
                var jst = JsonSerializer.Deserialize<LibraryTemplate>(json, new JsonSerializerOptions
                {
                    AllowTrailingCommas = true,
                    PropertyNameCaseInsensitive = true
                });
                string group = jst?.Group ?? string.Empty;
                group = Translater.Instant("Templates.Groups." + CleanForJsonKey(group));
                if (string.IsNullOrWhiteSpace(group) || group == CleanForJsonKey(jst!.Group))
                    group = jst?.Group ?? string.Empty;

                string name = jst!.Name;
                string prefix = "Templates.Libraries." + CleanForJsonKey(jst.Name) + ".";
                string translateName = Translater.Instant(prefix + "Name");
                if (string.IsNullOrWhiteSpace(translateName) == false && translateName != "Name")
                    name = translateName;
                string description = jst.Description;
                string translateDescription = Translater.Instant(prefix + "Description");
                if (string.IsNullOrWhiteSpace(translateDescription) == false && translateDescription != "Description")
                    description = translateDescription;

                var library = new Library
                {
                    Enabled = true,
                    FileSizeDetectionInterval = jst.FileSizeDetectionInterval,
                    Filters = jst.Filters?.Any() == true ? jst.Filters : string.IsNullOrWhiteSpace(jst.Filter) == false ? [jst.Filter] : [],
                    ExclusionFilters = jst.ExclusionFilters?.Any() == true ? jst.ExclusionFilters : string.IsNullOrWhiteSpace(jst.ExclusionFilter) == false ? [jst.ExclusionFilter] : [],
                    Extensions = jst.Extensions?.OrderBy(x => x.ToLowerInvariant())?.ToList() ?? [],
                    //UseFingerprinting = jst.UseFingerprint,
                    Name = name,
                    Description = description,
                    Path = jst.Path,
                    Priority = jst.Priority,
                    ScanInterval = jst.ScanInterval,
                    //ReprocessRecreatedFiles = jst.ReprocessRecreatedFiles,
                    Folders = jst.Folders,
                    //UpdateMovedFiles = jst.DontUpdateMovedFiles == false
                };
                if (group == generalGroup)
                    lstGeneral.Add(library);
                else
                {
                    if (templates.ContainsKey(group) == false)
                        templates.Add(group, new List<Library>());
                    templates[group].Add(library);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.WLog($"Error parsing library template '{tf.FullName}': {ex.Message}");
            }
        }

        var dict = new Dictionary<string, List<Library>>();
        if(lstGeneral.Any())
            dict.Add(generalGroup, lstGeneral.OrderBy(x => x.Name.ToLowerInvariant()).ToList());
        foreach (var kv in templates)
        {
            if(kv.Value.Any())
                dict.Add(kv.Key, kv.Value.OrderBy(x => x.Name.ToLowerInvariant()).ToList());
        }

        return dict;
    }

    /// <summary>
    /// Cleans a value for a json key
    /// </summary>
    /// <param name="value">the text value</param>
    /// <returns>the cleaned json key</returns>
    private string CleanForJsonKey(string value)
        => Regex.Replace(value, @"[\s/\\]", string.Empty);

    /// <summary>
    /// Rescans enabled libraries and waits for them to be scanned
    /// </summary>
    [HttpPost("rescan-enabled")]
    public async Task RescanEnabled()
    {
        var service = ServiceLoader.Load<LibraryService>();
        var libs = (await service.GetAllAsync()).Where(x => x.Enabled).Select(x => x.Uid).ToArray();
        Rescan(new ReferenceModel<Guid> { Uids = libs });
    }
}