using FileFlows.WebServer.Authentication;
using FileFlows.Server.Helpers;
using FileFlows.ServerModels;
using FileFlows.Services;
using FileFlows.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using NPoco;

namespace FileFlows.WebServer.Controllers;

/// <summary>
/// Revisioned Object controller
/// </summary>
[Route("/api/revision")]
[FileFlowsAuthorize(UserRole.Revisions)]
public class RevisionController:Controller
{
    /// <summary>
    /// Get all revisions for an object
    /// </summary>
    /// <param name="uid">The UID of the object</param>
    /// <returns>A list of revisions for an object</returns>
    [HttpGet("{uid}")]
    public Task<List<RevisionedObject>> GetAll([FromRoute] Guid uid)
    {
        if (LicenseService.IsLicensed() == false)
            return Task.FromResult(new List<RevisionedObject>());

        var service = ServiceLoader.Load<RevisionService>();
        return service.GetAllAsync(uid);
    }
    
    /// <summary>
    /// Get latest revisions for all objects
    /// </summary>
    /// <returns>A list of latest revisions for all objects</returns>
    [HttpGet("list")]
    public async Task<IEnumerable<RevisionedObject>> ListAll()
    {
        if (LicenseService.IsLicensed() == false)
            return new RevisionedObject[] { };
        
        var service = ServiceLoader.Load<RevisionService>();
        return await service.ListAllAsync();
    }

    /// <summary>
    /// Gets a specific revision
    /// </summary>
    /// <param name="uid">The UID of the revision object</param>
    /// <param name="dboUid">the UID of the DbObject</param>
    /// <returns>The specific revision</returns>
    [HttpGet("{dboUid}/revision/{uid}")]
    public async Task<RevisionedObject?> GetRevision([FromRoute] Guid uid, [FromRoute] Guid dboUid)
    {
        if (LicenseService.IsLicensed() == false)
            return null;
        return await ServiceLoader.Load<RevisionService>().Get(uid, dboUid);
    }

    /// <summary>
    /// Restores a revision
    /// </summary>
    /// <param name="uid">The UID of the object</param>
    /// <param name="revisionUid">the UID of the revision</param>
    [HttpPut("{revisionUid}/restore/{uid}")]
    public async Task Restore([FromRoute] Guid revisionUid, [FromRoute] Guid uid)
    {
        if (LicenseService.IsLicensed() == false)
            return;
        await ServiceLoader.Load<RevisionService>().Restore(revisionUid, uid);
    }
    
    
    /// <summary>
    /// Creates an object revision
    /// </summary>
    /// <param name="dbo">the source to revision</param>
    /// <returns>the revisioned object reference</returns>
    internal static RevisionedObject From(DbObject dbo)
    {
        var ro = new RevisionedObject();
        ro.Uid = Guid.NewGuid();
        ro.RevisionDate = dbo.DateModified;
        ro.RevisionCreated = dbo.DateCreated;
        ro.RevisionData = dbo.Data;
        ro.RevisionType = dbo.Type;
        ro.RevisionUid = dbo.Uid;
        ro.RevisionName = dbo.Name;
        return ro;
    }

    /// <summary>
    /// Saves an DbObject revision
    /// </summary>
    /// <param name="dbo">the DbObject to save a revision of</param>
    internal static async Task SaveRevision(DbObject dbo)
    {
        if (LicenseService.IsLicensed() == false)
            return;
        
        var ro = From(dbo);
        await Save(ro);
    }
    
    /// <summary>
    /// Saves the revision to the database
    /// </summary>
    /// <param name="ro">the revisioned object to save</param>
    private static async Task Save(RevisionedObject ro)
    {
        // this is a premium feature
        if (LicenseService.IsLicensed() == false)
            return;
        
        if (ro == null)
            return;
        if (ro.Uid == Guid.Empty)
            ro.Uid = Guid.NewGuid();
        var service = ServiceLoader.Load<RevisionService>();
        await service.Insert(ro);
    }
}