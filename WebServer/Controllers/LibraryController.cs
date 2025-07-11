using System.Text;
using FileFlows.ServerModels;
using FileFlows.Services.FileProcessing;

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
            if (LicenseService.IsLicensed(LicenseFlags.ProcessingOrder) == false)
            {
                library.MaxRunners = 0;
                library.ProcessingOrder = ProcessingOrder.AsFound;
            }
        }

        var service = ServiceLoader.Load<LibraryService>();
        bool nameUpdated = false;
        if (library.Uid != Guid.Empty)
        {
            // existing, check for name change
            var existing = await service.GetByUidAsync(library.Uid);
            if (existing != null)
            {
                nameUpdated = existing.Name != library.Name;
            }
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
                await Rescan(new ReferenceModel<Guid> { Uids = [library.Uid] });
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
        LogTimer log = new();
        try
        {
            var service = ServiceLoader.Load<LibraryService>();
            log.Log("Getting library");
            var library = await service.GetByUidAsync(uid);
            log.Log("Get Library complete");
            if (library == null)
                throw new Exception("Library not found.");

            if (library.Enabled != enable)
            {
                library.Enabled = enable;
                log.Log("Updating library");
                library = await service.Update(library, await GetAuditDetails(), log);
                log.Log("Updated library");
            }

            return library;
        }
        finally
        {
            Logger.Instance.ILog("Library Set State Log:\n" + log);
        }
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
    public async Task<IActionResult> Rescan([FromBody] ReferenceModel<Guid> model)
    {
        var sorter = ServiceLoader.Load<FileSorterService>();
        // bool atCapacity = sorter.AtCapacity();
        // if(atCapacity)
        //     return BadRequest("ErrorMessages.LibraryScanAtCapacity");
        
        var service = ServiceLoader.Load<LibraryService>();
        await service.Rescan(model.Uids);
        return Ok();
    }

    /// <summary>
    /// Reset libraries.
    /// All library files will all be removed and the libraries will be rescanned.
    /// </summary>
    /// <param name="model">A reference model containing UIDs to reset</param>
    /// <returns>an awaited task</returns>
    [HttpPut("reset")]
    public async Task Reset([FromBody] ReferenceModel<Guid> model)
    {
        await ServiceLoader.Load<LibraryFileService>().ResetLibraries(model.Uids);
        _ = Rescan(model);
    }

    /// <summary>
    /// Rescans enabled libraries and waits for them to be scanned
    /// </summary>
    [HttpPost("rescan-enabled")]
    public async Task<IActionResult> RescanEnabled()
    {
        var service = ServiceLoader.Load<LibraryService>();
        var libs = (await service.GetAllAsync()).Where(x => x.Enabled).Select(x => x.Uid).ToArray();
        return await Rescan(new ReferenceModel<Guid> { Uids = libs });
    }
}