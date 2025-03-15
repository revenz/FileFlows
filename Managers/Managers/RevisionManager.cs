using FileFlows.DataLayer.Models;
using FileFlows.Plugin;

namespace FileFlows.Managers;

/// <summary>
/// Manager for object revisions
/// </summary>
public class RevisionManager 
{
    /// <summary>
    /// Inserts a new revision
    /// </summary>
    /// <param name="revision">the new revision</param>
    public Task Insert(RevisionedObject revision)
        => DatabaseAccessManager.Instance.RevisionManager.Insert(revision);
    
    /// <summary>
    /// Gets all revisions in the database
    /// </summary>
    /// <returns>the revisions</returns>
    public Task<List<RevisionedObject>> GetAll()
        => DatabaseAccessManager.Instance.RevisionManager.GetAll();
        
    /// <summary>
    /// Gets all revisions for a given object
    /// </summary>
    /// <param name="uid">the UID to get all revisions for</param>
    /// <returns>the items</returns>
    public Task<List<RevisionedObject>> GetAll(Guid uid)
        => DatabaseAccessManager.Instance.RevisionManager.GetAll(uid);

    /// <summary>
    /// Get latest revisions for all objects
    /// </summary>
    /// <returns>A list of latest revisions for all objects</returns>
    public Task<List<RevisionedObject>> ListAll()
        => DatabaseAccessManager.Instance.RevisionManager.ListAll();

    /// <summary>
    /// Deletes all the revisions
    /// </summary>
    /// <param name="uids">the UIDs of the revisions to delete</param>
    /// <returns>a task to await</returns>
    public Task Delete(Guid[] uids)
        => DatabaseAccessManager.Instance.RevisionManager.Delete(uids);

    /// <summary>
    /// Gets a specific revision
    /// </summary>
    /// <param name="uid">The UID of the revision object</param>
    /// <param name="dboUid">the UID of the DbObject</param>
    /// <returns>The specific revision</returns>
    public Task<RevisionedObject?> Get(Guid uid, Guid dboUid)
        => DatabaseAccessManager.Instance.RevisionManager.Get(uid, dboUid);

    /// <summary>
    /// Restores a revision
    /// </summary>
    /// <param name="revisionUid">the UID of the revision</param>
    /// <param name="uid">The UID of the object</param>
    public async Task<Result<bool>> Restore(Guid revisionUid, Guid uid)
    {
        var revision = await Get(uid, revisionUid);
        if (revision == null)
            return Result<bool>.Fail("Revision not found");

        var dbo = new DbObject();
        dbo.Data = revision.RevisionData;
        dbo.Name = revision.RevisionName;
        dbo.DateCreated = revision.RevisionCreated;
        dbo.DateModified = revision.RevisionDate;
        dbo.Uid = revision.RevisionUid;
        dbo.Type = revision.RevisionType;

        await DatabaseAccessManager.Instance.ObjectManager.Update(dbo);

        // have to update any in memory objects
        if (dbo.Type == typeof(Library).FullName)
            await new LibraryManager().Refresh();
        else if (dbo.Type == typeof(Flow).FullName)
            await new FlowManager().Refresh();

        return true;
    }
}