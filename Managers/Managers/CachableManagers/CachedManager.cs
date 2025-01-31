using FileFlows.ServerShared.Models;
using FileFlows.Shared.Helpers;

namespace FileFlows.Managers;

/// <summary>
/// Services that caches its objects
/// </summary>
public abstract class CachedManager<T> where T : FileFlowObject, new()
{
    /// <summary>
    /// Gets if this service increments the system configuration revision number when changes to the data happens
    /// </summary>
    public virtual bool IncrementsConfiguration => true;

    /// <summary>
    /// Gets if the cache should be used
    /// </summary>
    protected virtual bool UseCache => SettingsManager.UseCache;

    /// <summary>
    /// Gets if the revisions should be saved
    /// </summary>
    protected virtual bool SaveRevisions => false;

    private FairSemaphore GetDataSemaphore = new(1);
    
    protected static List<T>? _Data;
    
    /// <summary>
    /// Gets the data
    /// </summary>
    protected async Task<List<T>> GetData()
    {
        if (UseCache == false)
            return await LoadDataFromDatabase();
        GetDataSemaphore.WaitAsync().Wait();
        try
        {
            if (_Data == null)
                await Refresh();
            return _Data ?? new ();
        }
        finally
        {
            GetDataSemaphore.Release();
        }
    }


    /// <summary>
    /// Sets the data
    /// </summary>
    /// <param name="data">the data to set</param>
    internal static void SetData(List<T> data)
    {
        _Data = data ?? new List<T>();
    }

    /// <summary>
    /// Gets the data
    /// </summary>
    /// <returns>the data</returns>
    public virtual Task<List<T>> GetAll()
        => GetData();

    /// <summary>
    /// Gets an item by its UID
    /// </summary>
    /// <param name="uid">the UID of the item</param>
    /// <returns>the item</returns>
    public virtual async Task<T?> GetByUid(Guid uid)
    {
        try
        {
            if (UseCache)
                return (await GetData()).FirstOrDefault(x => x.Uid == uid);
            return await DatabaseAccessManager.Instance.FileFlowsObjectManager.Single<T>(uid);
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog($"Failed getting '{typeof(T).Name}': " + ex.Message);
            return null;
        }
    }
    
    /// <summary>
    /// Gets a item by it's name
    /// This is virtual so plugins can override it and use the package name
    /// </summary>
    /// <param name="item">the item to get by name</param>
    /// <param name="ignoreCase">if case should be ignored</param>
    /// <returns>the item</returns>
    protected virtual Task<T?> GetByName(T item, bool ignoreCase = true)
        => GetByName(item.Name, ignoreCase);
    
    /// <summary>
    /// Gets a item by it's name
    /// </summary>
    /// <param name="name">the name of the item</param>
    /// <param name="ignoreCase">if case should be ignored</param>
    /// <returns>the item</returns>
    public virtual async Task<T?> GetByName(string name, bool ignoreCase = true)
    {
        if (UseCache)
        {
            if (ignoreCase)
            {
                name = name.ToLowerInvariant();
                return (await GetData()).FirstOrDefault(x => x.Name.ToLowerInvariant() == name);
            }

            return (await GetData()).FirstOrDefault(x => x.Name == name);
        }

        return await DatabaseAccessManager.Instance.FileFlowsObjectManager.GetByName<T>(name, ignoreCase);
    }

    /// <summary>
    /// Updates an item
    /// </summary>
    /// <param name="item">the item being updated</param>
    /// <param name="auditDetails">the audit details</param>
    /// <param name="dontIncrementConfigRevision">if this is a revision object, if the revision should be updated</param>
    /// <returns>the result of the update, if successful the updated item</returns>
    public async Task<Result<T>> Update(T item, AuditDetails? auditDetails, bool dontIncrementConfigRevision = false)
    {
        if (item == null)
            return Result<T>.Fail("No model");

        if (string.IsNullOrWhiteSpace(item.Name))
            return Result<T>.Fail("ErrorMessages.NameRequired");

        var existingName = await GetByName(item);
        if (existingName != null && existingName.Uid != item.Uid)
            return Result<T>.Fail("ErrorMessages.NameInUse");

        var customValid = await CustomUpdateValidate(item);
        if (customValid.IsFailed)
            return customValid;
        
        Logger.Instance.ILog($"Updating {item.GetType().Name}: '{item.Name}'");
        var result = await DatabaseAccessManager.Instance.FileFlowsObjectManager
            .AddOrUpdateObject(item, auditDetails, saveRevision: SaveRevisions);
        if (result.Failed(out var error))
        {
            Logger.Instance.ELog($"Failed updating {item.GetType().Name}: '{item.Name}' : {error}");
            return Result<T>.Fail(error);
        }
        
        if (result is { IsFailed: false, Value.changed: true } && dontIncrementConfigRevision == false)
            await IncrementConfigurationRevision();
        
        if(UseCache)
            await Refresh();

        return (await GetByUid(item.Uid))!;
    }

    /// <summary>
    /// Custom update validate that runs after the name validation
    /// </summary>
    /// <returns>the result</returns>
    protected virtual Task<Result<T>> CustomUpdateValidate(T item)
        => Task.FromResult(Result<T>.Success(item));


    /// <summary>
    /// Refreshes the data
    /// </summary>
    public async Task Refresh()
    {
        if (UseCache == false)
            return;
        
        Logger.Instance.ILog($"Refreshing Data for '{typeof(T).Name}'");
        var newData = await LoadDataFromDatabase();
        if (_Data?.Any() != true)
        {
            _Data = newData;
            return;
        }
        
        List<T> updatedData = new List<T>();

        foreach (var newItem in newData)
        {
            var existingItem = _Data.FirstOrDefault(item => item.Uid == newItem.Uid);
            if (existingItem != null)
            {
                // Update properties of existing item with values from newItem
                var properties = typeof(T).GetProperties();
                foreach (var property in properties)
                {
                    if (property.CanRead && property.CanWrite)
                    {
                        var value = property.GetValue(newItem);
                        property.SetValue(existingItem, value);
                    }
                }
                updatedData.Add(existingItem);
            }
            else
            {
                updatedData.Add(newItem);
            }
        }

        _Data = updatedData;
    }
    
    /// <summary>
    /// Loads the data from the database
    /// </summary>
    /// <returns>the data</returns>
    protected async Task<List<T>> LoadDataFromDatabase()
     => (await DatabaseAccessManager.Instance.FileFlowsObjectManager.Select<T>()).ToList();

    /// <summary>
    /// Deletes items matching the UIDs
    /// </summary>
    /// <param name="uids">the UIDs of the items to delete</param>
    /// <param name="auditDetails">the audit details</param>
    public async Task Delete(Guid[] uids, AuditDetails? auditDetails)
    {
        if (uids?.Any() != true)
            return;
        
        await DatabaseAccessManager.Instance.FileFlowsObjectManager.Delete(uids, auditDetails);
        await IncrementConfigurationRevision();
        
        if(UseCache)
            await Refresh();
    }

    
    /// <summary>
    /// Increments the revision of the configuration
    /// </summary>
    /// <param name="force">If we are forcing a configuration revision increment</param>
    public async Task IncrementConfigurationRevision(bool force = false)
    {
        if (force == false && IncrementsConfiguration == false)
            return;
        var service = new SettingsManager();
        await service.RevisionIncrement();
    }
    
    
    /// <summary>
    /// Gets a unique name
    /// </summary>
    /// <param name="name">the name to make unique</param>
    /// <returns>the unique name</returns>
    public virtual async Task<string> GetNewUniqueName(string name)
    {
        List<string> names;
        if (UseCache)
        {
             names = (await GetData()).Select(x => x.Name.ToLowerInvariant()).ToList();
        }
        else
        {
            names = (await DatabaseAccessManager.Instance.FileFlowsObjectManager.GetNames<T>())
                .Select(x => x.ToLowerInvariant()).ToList();
        }

        return UniqueNameHelper.GetUnique(name, names);
    }

    /// <summary>
    /// Checks to see if a name is in use
    /// </summary>
    /// <param name="uid">the Uid of the item</param>
    /// <param name="name">the name of the item</param>
    /// <returns>true if name is in use</returns>
    public virtual async Task<bool> NameInUse(Guid uid, string name)
    {
        if (UseCache)
        {
            name = name.ToLowerInvariant().Trim();
            return (await GetData()).Any(x => uid != x.Uid && x.Name.ToLowerInvariant() == name);
        }
        else
        {
            var existing =
                await DatabaseAccessManager.Instance.FileFlowsObjectManager.GetByName<T>(name, ignoreCase: true);
            return existing.IsFailed == false && existing.ValueOrDefault != null;
        }
    }

    /// <summary>
    /// Gets if a UID is in use
    /// </summary>
    /// <param name="uid">the UID to check</param>
    /// <returns>true if in use</returns>
    public virtual Task<bool> UidInUse(Guid uid)
        => DatabaseAccessManager.Instance.ObjectManager.UidInUse(uid);
}