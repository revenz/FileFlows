using System.Reflection;
using System.Text.Json;
using FileFlows.DataLayer.Converters;
using FileFlows.DataLayer.Helpers;
using FileFlows.DataLayer.Models;
using FileFlows.Plugin;
using FileFlows.ServerShared.Models;
using FileFlows.Shared;
using FileFlows.Shared.Attributes;
using FileFlows.Shared.Json;
using FileFlows.Shared.Models;
using FileFlows.Validators;

namespace FileFlows.DataLayer;

/// <summary>
/// Manager for FileFlowsObject that are stored in the DbObject table
/// </summary>
internal  class FileFlowsObjectManager
{
    private readonly DbObjectManager dbom;
    internal FileFlowsObjectManager(DbObjectManager dbom)
    {
        this.dbom = dbom;
    }
    
    /// <summary>
    /// Select a list of objects
    /// </summary>
    /// <typeparam name="T">the type of objects to select</typeparam>
    /// <returns>a list of objects</returns>
    public virtual async Task<List<T>> Select<T>() where T : FileFlowObject, new()
    {
        var dbObjects = await dbom.GetAll(typeof(T).FullName!);
        return ConvertFromDbObject<T>(dbObjects);
    }
    
    /// <summary>
    /// Select a single instance of a type
    /// </summary>
    /// <typeparam name="T">The type to select</typeparam>
    /// <returns>a single instance</returns>
    public async Task<Result<T?>> Single<T>() where T : FileFlowObject, new()
    {
        var fullName = typeof(T).FullName;
        if (fullName == null)
            return Result<T?>.Fail("Type FullName was null");
        
        DbObject dbObject = await dbom.Single(fullName);
        if (dbObject == null)
            return default;
        
        if (string.IsNullOrEmpty(dbObject?.Data))
            return Result<T?>.Fail($"Object found with no data");
        return Convert<T>(dbObject);
    }
    
    /// <summary>
    /// Selects a single instance
    /// </summary>
    /// <param name="uid">the UID of the item to select</param>
    /// <typeparam name="T">the type of item to select</typeparam>
    /// <returns>a single instance</returns>
    public virtual async Task<Result<T?>> Single<T>(Guid uid) where T : FileFlowObject, new()
    {
        var fullName = typeof(T).FullName;
        if (fullName == null)
            return Result<T?>.Fail("Type FullName was null");
        
        DbObject dbObject = await dbom.Single(uid);
        if (dbObject == null)
            return null;
        
        if (dbObject.Type != fullName)
            return Result<T?>.Fail($"Object found but was the wrong type '{dbObject.Type}' expected '{fullName}'");
        
        if (string.IsNullOrEmpty(dbObject?.Data))
            return Result<T?>.Fail($"Object found with no data");
        
        return Convert<T>(dbObject);
    }
    
    /// <summary>
    /// Gets an item by its name
    /// </summary>
    /// <param name="name">the name of the item</param>
    /// <param name="ignoreCase">if casing should be ignored</param>
    /// <typeparam name="T">The type to get</typeparam>
    /// <returns>the item</returns>
    public async Task<Result<T?>> GetByName<T>(string name, bool ignoreCase = false) where T : FileFlowObject, new()
    {
        var fullName = typeof(T).FullName;
        if (fullName == null)
            return Result<T?>.Fail("Type FullName was null");
        
        var dbObject = await dbom.GetByName(fullName, name, ignoreCase);
        if (dbObject == null)
            return Result<T?>.Fail("Not found.");
        
        if (dbObject.Type != fullName)
            return Result<T?>.Fail($"Object found but was the wrong type '{dbObject.Type}' expected '{fullName}'");
        
        if (string.IsNullOrEmpty(dbObject?.Data))
            return Result<T?>.Fail($"Object found with no data");
        
        return Convert<T>(dbObject);
    }

    /// <summary>
    /// Gets all names for a specific type
    /// </summary>
    /// <typeparam name="T">The type to get</typeparam>
    /// <returns>all names for a specific type</returns>
    public async Task<List<string>> GetNames<T>() where T : FileFlowObject, new()
    {
        var fullName = typeof(T).FullName;
        if (fullName == null)
            return new();

        return await dbom.GetNames(fullName);
    }

    /// <summary>
    /// Adds or updates an object in the database
    /// </summary>
    /// <param name="obj">The object being added or updated</param>
    /// <param name="auditDetails">The audit details</param>
    /// <param name="saveRevision">if the revision should be saved</param>
    /// <typeparam name="T">The type of object being added or updated</typeparam>
    /// <returns>The updated object</returns>
    public async Task<Result<(DbObject dbo, bool changed)>> AddOrUpdateObject<T>(T obj, AuditDetails? auditDetails, bool saveRevision = false) where T : FileFlowObject, new()
    {
        try
        {
            var dbo = ConvertToDbObject(obj);
            DbObject? dbObject = null;
            T? original = null;
            bool newObject = false;
            if (obj.Uid != Guid.Empty)
            {
                try
                {
                    dbObject = await dbom.Single(obj.Uid);
                    if(dbObject != null)
                        original = Convert<T>(dbObject);
                }
                catch (Exception)
                {
                    // Ignored
                }
            }

            bool changed = false;
            if (dbObject == null)
            {
                if (obj.Uid == Guid.Empty)
                    obj.Uid = Guid.NewGuid();
                if (obj.DateCreated < new DateTime(2020, 1, 1))
                    obj.DateCreated = DateTime.UtcNow;
                obj.DateModified = DateTime.UtcNow;
                // create new 
                dbObject = new DbObject
                {
                    Uid = obj.Uid,
                    Name = obj.Name,
                    DateCreated = obj.DateCreated,
                    DateModified = obj.DateModified,
                    Type = dbo.Type,
                    Data = dbo.Data
                };
                await dbom.Insert(dbObject);
                newObject = true;
                changed = true;
            }
            else if (dbo.Name != dbObject.Name || dbo.Data != dbObject.Data)
            {
                obj.DateModified = DateTime.UtcNow;
                dbObject.Name = obj.Name;
                dbObject.DateModified = obj.DateModified;
                if (obj.DateCreated != dbObject.DateCreated && obj.DateCreated > new DateTime(2020, 1, 1))
                {
                    dbObject.DateCreated = obj.DateCreated; // OnHeld moving to process now can change this date
                }

                changed = true;
                dbObject.Data = dbo.Data;
                await dbom.Update(dbObject);
            }

            if (changed)
            {
                Guid? revisionUid = null;
                if (saveRevision)
                {
                    revisionUid = Guid.NewGuid();
                    await DatabaseAccessManager.Instance.RevisionManager.Insert(new()
                    {
                        Uid = revisionUid.Value,
                        RevisionDate = DateTime.UtcNow,
                        RevisionUid = dbo.Uid,
                        RevisionCreated = dbo.DateCreated,
                        RevisionName = dbo.Name,
                        RevisionType = dbo.Type,
                        RevisionData = dbo.Data
                    });
                }

                if (auditDetails != null)
                {
                    var changes = AuditValueHelper.Audit(typeof(T), obj, original);

                    await DatabaseAccessManager.Instance.AuditManager.Insert(new()
                    {
                        Action = newObject ? AuditAction.Added : AuditAction.Updated,
                        RevisionUid = revisionUid,
                        LogDate = DateTime.UtcNow,
                        ObjectType = dbo.Type,
                        ObjectUid = dbo.Uid,
                        OperatorName = auditDetails.UserName,
                        OperatorUid = auditDetails.UserUid,
                        OperatorType = auditDetails.OperatorType,
                        IPAddress = auditDetails.IPAddress,
                        Parameters = new ()
                        {
                            { nameof(obj.Name), obj.Name }
                        },
                        Changes = changes.ToDictionary(x => x.Key, x => (object)x.Value)
                    });
                }
            }

            return (dbObject, changed);
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog("Failed FFObjectManager.AddOrUpdateObject: " + ex.Message + Environment.NewLine + ex.StackTrace);
            return Result<(DbObject dbo, bool changed)>.Fail("Error: " + ex.Message);
        }
    }

    
    // /// <summary>
    // /// Updates a FileFlowObject
    // /// </summary>
    // /// <param name="auditDetails">The audit details</param>
    // /// <param name="item">the item to update</param>
    // public async Task Update(FileFlowObject item, AuditDetails auditDetails)
    // {
    //     item.DateModified = DateTime.UtcNow;
    //     if (item.Uid == Guid.Empty)
    //         item.Uid = Guid.NewGuid();
    //     var dbo = ConvertToDbObject(item);
    //     await dbom.Update(dbo);
    // }


    /// <summary>
    /// Delete items from a database
    /// </summary>
    /// <param name="uids">the UIDs of the items to delete</param>
    /// <param name="auditDetails">The audit details</param>
    public Task Delete(Guid[] uids, AuditDetails? auditDetails) => dbom.Delete(uids, auditDetails);







    /// <summary>
    /// Converts a FileFlowObject to a DbObject
    /// </summary>
    /// <param name="item">the item to convert</param>
    /// <returns>the converted item</returns>
    internal static DbObject ConvertToDbObject(FileFlowObject item)
    {
        var serializerOptions = new JsonSerializerOptions
        {
            Converters = { new DataConverter(), new DataConverter<FlowPart>(), new BoolConverter(), new ValidatorConverter() }
        };
        // need to case obj to (ViObject) here so the DataConverter is used
        string json = JsonSerializer.Serialize(item, serializerOptions);

        var type = item.GetType();
        return new DbObject()
        {
            Uid = item.Uid,
            Name = item.Name?.EmptyAsNull() ?? type.Name,
            Type = type.FullName!,
            DateCreated = item.DateCreated,
            DateModified = item.DateModified,
            Data = json,
        };
    }
    
    
    
    
    /// <summary>
    /// Converts DbObjects into strong types
    /// </summary>
    /// <param name="dbObjects">a collection of DbObjects</param>
    /// <typeparam name="T">The type to convert to</typeparam>
    /// <returns>A converted list of objects</returns>
    internal List<T> ConvertFromDbObject<T>(IEnumerable<DbObject> dbObjects) where T : FileFlowObject, new()
    {
        var list = dbObjects.ToList();
        T[] results = new T [list.Count];
        Parallel.ForEach(list, (x, state, index) =>
        {
            var converted = Convert<T>(x);
            if (converted != null)
                results[index] = converted;
        });
        return results.Where(x => x != null).ToList();
    }
    
    
    /// <summary>
    /// Converts a DbObject type to a strong type
    /// </summary>
    /// <param name="dbObject">the DbObject instance to convert</param>
    /// <typeparam name="T">the type to convert to</typeparam>
    /// <returns>the converetd object</returns>
    internal static T? Convert<T>(DbObject dbObject) where T : FileFlowObject, new()
    {
        if (dbObject == null)
            return default;

        var serializerOptions = new JsonSerializerOptions
        {
            Converters =
            {
                new BoolConverter(), 
                new ValidatorConverter(), 
                new DataConverter()
            },
            PropertyNameCaseInsensitive = true
        };
        
        // need to case obj to (ViObject) here so the DataConverter is used
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
        T result = JsonSerializer.Deserialize<T>(dbObject.Data, serializerOptions);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
        if (result == null)
            return default;
        foreach (var prop in result.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            var dbencrypted = prop.GetCustomAttribute<EncryptedAttribute>();
            if (dbencrypted != null)
            {
                var value = prop.GetValue(result) as string;
                if (string.IsNullOrEmpty(value) == false)
                {
                    string decrypted = Decrypter.Decrypt(value);
                    if (decrypted != value)
                        prop.SetValue(result, decrypted);
                }
            }
        }

        //result.Uid = Guid.Parse(dbObject.Uid);
        result.Uid = dbObject.Uid;
        result.Name = dbObject.Name;
        result.DateModified = dbObject.DateModified;
        result.DateCreated = dbObject.DateCreated;
        return result;
    }

    /// <summary>
    /// Checks if a object exists with the given UID
    /// </summary>
    /// <param name="uid">the objects UID</param>
    /// <returns>true if exists, otherwise false</returns>
    public async Task<bool> Exists(Guid uid)
        => await dbom.Exists(uid);
}