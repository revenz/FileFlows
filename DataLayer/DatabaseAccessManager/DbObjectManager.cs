using System.Globalization;
using System.Text;
using System.Text.Json;
using FileFlows.DataLayer.DatabaseConnectors;
using FileFlows.DataLayer.Converters;
using FileFlows.DataLayer.Helpers;
using FileFlows.DataLayer.Models;
using FileFlows.Plugin;
using FileFlows.ServerShared.Models;
using FileFlows.Shared;
using FileFlows.Shared.Models;

namespace FileFlows.DataLayer;

/// <summary>
/// Manages data access operations for the DbObject table
/// </summary>
internal  class DbObjectManager : BaseManager
{
    /// <summary>
    /// Initializes a new instance of the DbObject manager
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="dbType">the type of database</param>
    /// <param name="dbConnector">the database connector</param>
    public DbObjectManager(ILogger logger, DatabaseType dbType, IDatabaseConnector dbConnector) : base(logger, dbType, dbConnector)
    {
    }
    
    /// <summary>
    /// Adds or updates a DbObject directly in the database
    /// Note: NO revision will be saved
    /// </summary>
    /// <param name="dbObject">the object to add or update</param>
    internal async Task AddOrUpdate(DbObject dbObject)
    {
        using var db = await DbConnector.GetDb(write: true);
        bool updated = await db.Db.UpdateAsync(dbObject) > 0;
        if (updated)
            return;
        await db.Db.InsertAsync(dbObject);
    }

    /// <summary>
    /// Inserts a new DbObject
    /// </summary>
    /// <param name="dbObject">the new DbObject</param>
    internal async Task Insert(DbObject dbObject)
    {
        string sql = "insert into " + Wrap(nameof(DbObject)) + "(" +
                     Wrap(nameof(DbObject.Uid)) + ", " +
                     Wrap(nameof(DbObject.Name)) + ", " +
                     Wrap(nameof(DbObject.Type)) + ", " +
                     Wrap(nameof(DbObject.Data)) + ", " +
                     Wrap(nameof(DbObject.DateCreated)) + ", " +
                     Wrap(nameof(DbObject.DateModified)) + " " +
                     " ) values (@0, @1, @2, @3, " +
                     DbConnector.FormatDateQuoted(dbObject.DateCreated.Clamp()) + ", " +
                     DbConnector.FormatDateQuoted(dbObject.DateModified.Clamp()) + ")";
        try
        {
            using var db = await DbConnector.GetDb(write: true);
            await db.Db.ExecuteAsync(sql,
                dbObject.Uid,
                dbObject.Name ?? string.Empty,
                dbObject.Type ?? string.Empty,
                dbObject.Data ?? string.Empty
            );
        }
        catch (Exception ex)
        {
            Logger.ELog("Failed to insert DB Object: " + ex.Message + Environment.NewLine + sql);
        }
    }

    /// <summary>
    /// Updates a new DbObject
    /// </summary>
    /// <param name="dbObject">the new DbObject</param>
    internal async Task Update(DbObject dbObject)
    {
        using var db = await DbConnector.GetDb(write: true);
        await db.Db.ExecuteAsync("update " + Wrap(nameof(DbObject)) + " set " +
                                 Wrap(nameof(DbObject.Name)) + " = @1, " +
                                 Wrap(nameof(DbObject.Type)) + " = @2, " +
                                 Wrap(nameof(DbObject.Data)) + " = @3, " +
                                 Wrap(nameof(DbObject.DateModified)) + " = " + DbConnector.FormatDateQuoted(dbObject.DateModified.Clamp()) +
                                 " where " + Wrap(nameof(DbObject.Uid)) + " = @0 ",
            dbObject.Uid,
            dbObject.Name,
            dbObject.Type,
            dbObject.Data ?? string.Empty
        );
    }

    /// <summary>
    /// Fetches all items
    /// </summary>
    /// <returns>the items</returns>
    internal async Task<List<DbObject>> GetAll()
    {
        using var db = await DbConnector.GetDb();
        return await db.Db.FetchAsync<DbObject>();
    }

    /// <summary>
    /// Fetches all items of a type
    /// </summary>
    /// <param name="typeName">the name of the type</param>
    /// <returns>the items</returns>
    internal async Task<List<DbObject>> GetAll(string typeName)
    {
        using var db = await DbConnector.GetDb();
        return await db.Db.FetchAsync<DbObject>($"where {Wrap(nameof(DbObject.Type))} = @0", typeName);
    }
    
    /// <summary>
    /// Select a single instance of a type
    /// </summary>
    /// <param name="typeName">the name of the type</param>
    /// <returns>a single instance</returns>
    internal async Task<DbObject> Single(string typeName)
    {
        using var db = await DbConnector.GetDb();
        return await db.Db.FirstOrDefaultAsync<DbObject>($@"where {Wrap(nameof(DbObject.Type))}=@0", typeName);
    }

    /// <summary>
    /// Gets a DbObject by its name
    /// </summary>
    /// <param name="typeName">the name of the type</param>
    /// <param name="name">the name of the item</param>
    /// <param name="ignoreCase">if casing should be ignored</param>
    /// <returns>the item if found</returns>
    internal async Task<DbObject?> GetByName(string typeName, string name, bool ignoreCase)
    {
        using var db = await DbConnector.GetDb();
        if(ignoreCase)
            return await db.Db.FirstOrDefaultAsync<DbObject>($"where {Wrap(nameof(DbObject.Type))}=@0 and LOWER({Wrap(nameof(DbObject.Name))})=@1", typeName, name.ToLowerInvariant());
        return await db.Db.FirstOrDefaultAsync<DbObject>($"where {Wrap(nameof(DbObject.Type))}=@0 and {Wrap(nameof(DbObject.Name))}=@1", typeName, name);
    }
    

    /// <summary>
    /// Gets all names for a specific type
    /// </summary>
    /// <param name="typeName">the name of the type</param>
    /// <returns>all names for a specific type</returns>
    internal async Task<List<string>> GetNames(string typeName)
    {
        using var db = await DbConnector.GetDb();
        return await db.Db.FetchAsync<string>(
            $"select {Wrap(nameof(DbObject.Name))} " +
            $" from {Wrap(nameof(DbObject))} " +
            $" where {Wrap(nameof(DbObject.Type))} = @0", typeName);
    }
    
    /// <summary>
    /// Select a single instance of a type
    /// </summary>
    /// <param name="uid">the UID of the item</param>
    /// <returns>a single instance</returns>
    internal async Task<DbObject> Single(Guid uid)
    {
        using var db = await DbConnector.GetDb();
        return await db.Db.FirstOrDefaultAsync<DbObject>($"where {DbConnector.WrapFieldName("Uid")}='{uid}'");
    }
    
    /// <summary>
    /// Updates the last modified date of an object
    /// </summary>
    /// <param name="uid">the UID of the object to update</param>
    internal async Task UpdateLastModified(Guid uid)
    {
        using var db = await DbConnector.GetDb(write: true);
        await db.Db.ExecuteAsync($"update {DbConnector.WrapFieldName(nameof(DbObject))} " +
                                 $" set {Wrap(nameof(DbObject.DateModified))} = " + DbConnector.FormatDateQuoted(DateTime.UtcNow) +
                                 $" where {Wrap(nameof(DbObject.Uid))} = '{uid}'");
    }

    /// <summary>
    /// Sets a value in the Data object of an item
    /// </summary>
    /// <param name="uid">The UID of the item being updated</param>
    /// <param name="typeName">The type name of the item being updated</param>
    /// <param name="field">The field in the Data object to update</param>
    /// <param name="value">The value to set</param>
    public async Task SetDataValue(Guid uid, string? typeName, string field, object value)
    {
        using var db = await DbConnector.GetDb(write: true);
        string sql = $"update {Wrap(nameof(DbObject))} set ";
        string dataColumnName = Wrap(nameof(DbObject.Data));
        if (DbType == DatabaseType.SqlServer)
            sql += $" {dataColumnName} = json_modify({dataColumnName}, '$.{field}', @0)";
        else if (DbType == DatabaseType.Postgres)
        {
            if (value is bool b)
                value = b ? "'1'" : "'0'";
            else
                value = SqlHelper.Escape(JsonSerializer.Serialize(value));
            
            sql +=
                $" {dataColumnName} = jsonb_set({dataColumnName}::jsonb, '{{{field}}}', {value}::jsonb)::text"; 
        }
        else // mysql and sqlite are the same
            sql += $" {dataColumnName} = json_set({dataColumnName}, '$.{field}', @0)";

        if (value is DateTime dt)
            value = dt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
        if (value is bool bValue)
            value = bValue ? 1 : 0;
        
        sql += $" where {Wrap(nameof(DbObject.Uid))} = '{uid}'" +
               $" and {Wrap(nameof(DbObject.Type))} = {SqlHelper.Escape(typeName!)}";
        

        await db.Db.ExecuteAsync(sql, value, typeName);
    }

    /// <summary>
    /// This will batch insert many objects into thee database
    /// </summary>
    /// <param name="items">Items to insert</param>
    internal virtual async Task AddMany(FileFlowObject[] items)
    {
        if (items?.Any() != true)
            return;
        int max = 500;
        int count = items.Length;

        var serializerOptions = new JsonSerializerOptions
        {
            Converters = { new DataConverter(), new BoolConverter() }
        };
        for (int i = 0; i < count; i += max)
        {
            StringBuilder sql = new StringBuilder();
            for (int j = i; j < i + max && j < count; j++)
            {
                var obj = items[j];
                // need to case obj to (ViObject) here so the DataConverter is used
                string json = JsonSerializer.Serialize(obj, serializerOptions);

                var type = obj.GetType();
                obj.Name = obj.Name?.EmptyAsNull() ?? type.Name;
                obj.Uid = Guid.NewGuid();
                obj.DateCreated = DateTime.UtcNow;
                obj.DateModified = obj.DateCreated;

                sql.AppendLine($"insert into {DbConnector.WrapFieldName(nameof(DbObject))} " +
                               $"({Wrap(nameof(DbObject.Uid))}, {Wrap(nameof(DbObject.Name))}," +
                               $"({Wrap(nameof(DbObject.DateCreated))}, {Wrap(nameof(DbObject.DateModified))}," +
                               $" {Wrap(nameof(DbObject.Type))}, {Wrap(nameof(DbObject.Data))})" +
                               $" values (" +
                               obj.Uid.ToString().SqlEscape() + "," +
                               obj.Name.SqlEscape() + "," +
                               DbConnector.FormatDateQuoted(obj.DateCreated) + ", " + 
                               DbConnector.FormatDateQuoted(obj.DateModified) + ", " + 
                               (type?.FullName ?? string.Empty).SqlEscape() + "," +
                               json.SqlEscape() +
                               ");");
            }

            if (sql.Length > 0)
            {
                using var db = await DbConnector.GetDb(write: true);
                await db.Db.ExecuteAsync(sql.ToString());
            }
        }
    }

    
    /// <summary>
    /// Delete items from a database
    /// </summary>
    /// <param name="uids">the UIDs of the items to delete</param>
    /// <param name="auditDetails">The audit details</param>
    public async Task Delete(Guid[] uids, AuditDetails? auditDetails)
    {
        if (uids?.Any() != true)
            return; // nothing to delete

        string strUids = string.Join(",", uids.Select(x => "'" + x.ToString() + "'"));
        List<DbObject>? deleted = null;

        using (var db = await DbConnector.GetDb(write: true))
        {
            deleted = db.Db.Fetch<DbObject>($"select * from {Wrap(nameof(DbObject))} " +
                                                $" where {Wrap(nameof(DbObject.Uid))} in ({strUids})");
            if (deleted?.Any() != true)
                return; // nothing to delete

            await db.Db.ExecuteAsync($"delete from {Wrap(nameof(DbObject))}" +
                                     $" where {Wrap(nameof(DbObject.Uid))} in ({strUids})");
        }

        if (auditDetails == null)
            return; // nothing to audit
        foreach (var item in deleted)
        {
            await DatabaseAccessManager.Instance.AuditManager.Insert(new()
            {
                Action = AuditAction.Deleted,
                ObjectUid = item.Uid,
                OperatorName = auditDetails.UserName,
                OperatorUid = auditDetails.UserUid,
                OperatorType = auditDetails.OperatorType,
                IPAddress = auditDetails.IPAddress,
                ObjectType = item.Type,
                LogDate = DateTime.UtcNow,
                Parameters = new()
                {
                    { "Name", item.Name }
                }
            });
        }
    }
    
    /// <summary>
    /// Delete items from a database
    /// </summary>
    /// <param name="typeName">the name of the type</param>
    /// <param name="name">the name of the item being deleted</param>
    /// <returns>true if any records were deleted</returns>
    public async Task<bool> DeleteByTypeAndName(string typeName, string name)
    {
        string sql = $"delete from {Wrap(nameof(DbObject))} where " +
                     $"{Wrap(nameof(DbObject.Type))} = @0 and " +
                     $"{Wrap(nameof(DbObject.Name))} = @1";
        using var db = await DbConnector.GetDb(write: true);
        return await db.Db.ExecuteAsync(sql, typeName, name) > 0;
    }

    /// <summary>
    /// Gets if there are any of the specified type in the system
    /// </summary>
    /// <returns>true if there are some, otherwise false</returns>
    public async Task<bool> Any(string fullName)
    {
        using var db = await DbConnector.GetDb();
        return (await db.Db.ExecuteScalarAsync<int>("select count(*) from " + 
                Wrap(nameof(DbObject)) + " where " + Wrap(nameof(DbObject.Type)) + " = @0", fullName)) > 0;
    }

    /// <summary>
    /// Gets if a UID is in use
    /// </summary>
    /// <param name="uid">the UID to check</param>
    /// <returns>true if in use</returns>
    public async Task<bool> UidInUse(Guid uid)
    {
        using var db = await DbConnector.GetDb();
        return await db.Db.ExecuteScalarAsync<int>("select count(*) from " + Wrap(nameof(DbObject)) + " where " +
                                               Wrap(nameof(DbObject.Uid)) + $" = '{uid}'") > 0;
    }

    /// <summary>
    /// Updates all object references
    /// </summary>
    /// <param name="property">object reference name/property</param>
    /// <param name="uid">the UID in the object reference to match again</param>
    /// <param name="name">the new name to update</param>
    /// <returns>true if any objects were updated, otherwise false</returns>
    public async Task<bool> UpdateAllObjectReferences(string property, Guid uid, string name)
    {
        string sql = $"update {Wrap(nameof(DbObject))} set ";

        string dataColumnName = Wrap(nameof(DbObject.Data));
        
        if (DbType == DatabaseType.SqlServer)
            sql += $" {dataColumnName} = json_modify({dataColumnName}, '$.{property}.Name', @0) " + 
                   $" where JSON_VALUE({dataColumnName}, '$.{property}.Uid') = '{uid}";
        else if (DbType == DatabaseType.Postgres)
            sql += $" {dataColumnName} = jsonb_set({dataColumnName}::jsonb, '{{{property},Name}}', to_jsonb(@0)::jsonb)::text " +
                   $" where {dataColumnName}::jsonb->'{property}'->>'Uid' = '{uid}'";
        else // mysql and sqlite are the same
            sql += $" {dataColumnName} = json_set({dataColumnName}, '$.{property}.Name', @0) " + 
                    $" where JSON_EXTRACT({dataColumnName},'$.{property}.Uid') = '{uid}'";
        
        using var db = await DbConnector.GetDb();
        try
        {
            bool result = await db.Db.ExecuteAsync(sql, name) > 0;
            return result;
        }
        catch (Exception ex)
        {
            Logger.ELog($"{ex.Message}: SQL: {sql}");
            return false;
        }
    }

    /// <summary>
    /// Checks if a object exists with the given UID
    /// </summary>
    /// <param name="uid">the objects UID</param>
    /// <returns>true if exists, otherwise false</returns>
    public async Task<bool> Exists(Guid uid)
    {
        using var db = await DbConnector.GetDb();
        try
        {
            bool result = db.Db.Exists<DbObject>(uid);
            return result;
        }
        catch (Exception)
        {
            return false;
        }
    }
}