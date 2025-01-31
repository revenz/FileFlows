// using FileFlows.Server.Helpers;
// using FileFlows.Services;
// using FileFlows.Shared.Models;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.AspNetCore.Mvc.Routing;
//
// namespace FileFlows.WebServer.Controllers;
//
//
// /// <summary>
// /// A base controller that can store data if the database type requires
// /// </summary>
// /// <typeparam name="T">the type of data this controller stores</typeparam>
// public abstract class ControllerStore<T>:Controller where T : FileFlowObject, new()
// {
//     protected static Dictionary<Guid, T> _Data;
//     protected static SemaphoreSlim _mutex = new SemaphoreSlim(1);
//
//     /// <summary>
//     /// Gets if add/updates/deletes for this controller should automatically update the configuration revision
//     /// </summary>
//     protected virtual bool AutoIncrementRevision => false; 
//     
//     protected async Task<IEnumerable<string>> GetNames(Guid? uid = null)
//     {
//         if (DbHelper.UseMemoryCache)
//         {
//             var data = await GetData();
//             if (uid == null)
//                 return data.Select(x => x.Value.Name);
//             return data.Where(x => x.Key != uid.Value).Select(x => x.Value.Name);
//         }
//         else
//         {
//             var data = await DbHelper.GetIndexedNames<T>();
//             if (uid == null)
//                 return data.Select(x => x.Value);
//             return data.Where(x => x.Key != uid.Value).Select(x => x.Value);
//         }
//     }
//
//
//     /// <summary>
//     /// Checks to see if a name is in use
//     /// </summary>
//     /// <param name="uid">the Uid of the item</param>
//     /// <param name="name">the name of the item</param>
//     /// <returns>true if name is in use</returns>
//     protected async Task<bool> NameInUse(Guid uid, string name)
//     {
//         name = name.ToLower().Trim();
//         if(DbHelper.UseMemoryCache)
//             return (await GetData()).Any(x => uid != x.Key && x.Value.Name.ToLower() == name);
//         
//         return await DbHelper.NameInUse<T>(uid, name);
//     }
//
//     protected async Task<string> GetNewUniqueName(string name)
//     {
//         List<string> names;
//         if (DbHelper.UseMemoryCache)
//             names = (await GetData()).Select(x => x.Value.Name.ToLower()).ToList();
//         else
//             names = (await DbHelper.GetNames<T>()).Select(x => x.ToLower()).ToList();
//         return UniqueNameHelper.GetUnique(name, names);
//     }
//
//     /// <summary>
//     /// Clears the cached data 
//     /// </summary>
//     internal void ClearData() => _Data = null;
//
//     /// <summary>
//     /// Gets all the data indexed by the items UID
//     /// </summary>
//     /// <returns>all the data indexed by the items UID</returns>
//     internal async Task<Dictionary<Guid, T>> GetData(bool? useCache = null)
//     {
//         if (useCache == null)
//             useCache = DbHelper.UseMemoryCache;
//         if(useCache == false)
//             return (await DbHelper.Select<T>()).ToDictionary(x => x.Uid, x => x);
//         
//         if (_Data == null)
//         {
//             await _mutex.WaitAsync();
//             try
//             {
//                 if (_Data == null) // make sure its still null after getting mutex
//                 {
//                     _Data = (await DbHelper.Select<T>())
//                                          .ToDictionary(x => x.Uid, x => x);
//                 }
//             }
//             finally
//             {
//                 _mutex?.Release(); 
//             }
//         }
//         return _Data;
//     }
//
//     internal virtual async Task<IEnumerable<T>> GetDataList(bool? useCache = null)
//     {
//         if (useCache == null)
//             useCache = DbHelper.UseMemoryCache;
//         if(useCache == true)
//             return (await GetData(useCache: true)).Values.ToList();
//         return await DbHelper.Select<T>();
//     } 
//
//     protected async Task<T> GetByUid(Guid uid, bool? useCache = null)
//     {
//         if (useCache == null)
//             useCache = DbHelper.UseMemoryCache;
//         if(useCache == true)
//         {
//             var data = await GetData(useCache: true);
//             if (data.ContainsKey(uid))
//                 return data[uid];
//             return default;
//         }
//
//         return await DbHelper.Single<T>(uid);
//     }
//
//     protected async Task DeleteAll(ReferenceModel<Guid> model)
//     {
//         if (model?.Uids?.Any() != true)
//             return;
//         await DeleteAll(model.Uids);
//     }
//
//     internal async Task DeleteAll(params Guid[] uids)
//     {
//         if (_Data?.Any() == true)
//         {
//             var data = await GetData(useCache: true);
//             foreach (var uid in uids)
//             {
//                 await _mutex.WaitAsync();
//                 try
//                 {
//                     if (data.ContainsKey(uid))
//                         data.Remove(uid);
//                 }
//                 finally
//                 {
//                     _mutex.Release();
//                 }
//             }
//         }
//
//         await DbHelper.Delete(uids);
//         if(AutoIncrementRevision)
//             IncrementConfigurationRevision();
//     }
//
//     internal async Task<T> Update(T model, bool checkDuplicateName = false, bool? useCache = null, bool? dontIncrementConfigRevision = null)
//     {
//         if (checkDuplicateName)
//         {
//             if(await NameInUse(model.Uid, model.Name))
//                 throw new Exception("ErrorMessages.NameInUse");
//         }
//         
//         var updated = await DbHelper.Update(model);
//         
//         if (useCache == null)
//             useCache = DbHelper.UseMemoryCache;
//         if(useCache == true)
//         {
//             if(_Data == null)
//                 await GetData();
//             await _mutex.WaitAsync();
//             try
//             {
//                 if (_Data.ContainsKey(updated.Uid))
//                     _Data[updated.Uid] = updated;
//                 else
//                     _Data.Add(updated.Uid, updated);
//             }
//             finally
//             {
//                 _mutex.Release();
//             }
//         }
//         if(AutoIncrementRevision && dontIncrementConfigRevision != true)
//             IncrementConfigurationRevision();
//         return updated;
//     }
//     
//     internal async Task UpdateDateModified(Guid uid, bool? useCache = null)
//     {
//         if (useCache == null)
//             useCache = DbHelper.UseMemoryCache;
//         if(useCache == true)
//         {
//             var item = await GetByUid(uid);
//             if (item == null)
//                 return;
//             item.DateModified = DateTime.Now;
//         }
//
//         await DbHelper.UpdateLastModified(uid);
//     }
//     
//     
//
//     /// <summary>
//     /// Refreshes a object from the DbObject
//     /// Called by the revision controller when a revision is restored
//     /// </summary>
//     /// <param name="dbo">the DbObject</param>
//     /// <param name="useCache">if the cache should be used</param>
//     internal async Task Refresh(DbObject dbo, bool? useCache = null)
//     {
//         if (useCache == null)
//             useCache = DbHelper.UseMemoryCache;
//         if(useCache == false)
//             return;
//         var to = DbHelper.GetDbManager().ConvertFromDbObject<T>(dbo);
//         
//         await _mutex.WaitAsync();
//
//         try
//         {
//             if (_Data.ContainsKey(dbo.Uid))
//             {
//                 // update it
//                 _Data[dbo.Uid] = to;
//             }
//             else
//             {
//                 // insert it
//                 _Data.Add(dbo.Uid, to);
//             }
//         }
//         finally
//         {
//             _mutex.Release();
//         }
//     }
//
//     /// <summary>
//     /// Increments the revision of the configuration
//     /// </summary>
//     protected void IncrementConfigurationRevision()
//     {
//         var service = ServiceLoader.Load<ISettingsService>();
//         _ = service.RevisionIncrement();
//     }
// }
