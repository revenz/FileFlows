// namespace FileFlows.Shared.Helpers;
//
// /// <summary>
// /// Cache store of objects
// /// </summary>
// public class CacheStore
// {
//     private readonly Dictionary<string, CachedObject> Cache = new();
//
//     /// <summary>
//     /// Gets an item from the cache store
//     /// </summary>
//     /// <param name="uid">The UID of the object to get</param>
//     /// <typeparam name="T">The type of object to get</typeparam>
//     /// <returns>The object, or default if not found</returns>
//     public T Get<T>(Guid uid) => Get<T>(uid.ToString());
//     
//     /// <summary>
//     /// Gets an item from the cache store
//     /// </summary>
//     /// <param name="uid">The UID of the object to get</param>
//     /// <typeparam name="T">The type of object to get</typeparam>
//     /// <returns>The object, or default if not found</returns>
//     public T Get<T>(string uid)
//     {
//         if (Cache.ContainsKey(uid) == false)
//             return default;
//         if (Cache[uid].Expiry < DateTime.UtcNow)
//         {
//             lock (Cache)
//             {
//                 Cache.Remove(uid);
//             }
//
//             return default;
//         }
//
//         return (T)Cache[uid].Value;
//     }
//
//     /// <summary>
//     /// Stores an item in the cache store
//     /// </summary>
//     /// <param name="uid">The UID of the item to store</param>
//     /// <param name="value">The item to store</param>
//     /// <param name="expirySeconds">How long to keep the item in the cache store</param>
//     /// <typeparam name="T">The type of item being stored</typeparam>
//     public void Store<T>(Guid uid, T value, int expirySeconds = 60) => Store(uid.ToString(), value, expirySeconds);
//     
//     /// <summary>
//     /// Stores an item in the cache store
//     /// </summary>
//     /// <param name="uid">The UID of the item to store</param>
//     /// <param name="value">The item to store</param>
//     /// <param name="expirySeconds">How long to keep the item in the cache store</param>
//     /// <typeparam name="T">The type of item being stored</typeparam>
//     public void Store<T>(string uid, T value, int expirySeconds = 60)
//     {
//         var co = new CachedObject()
//         {
//             Expiry = DateTime.UtcNow.AddSeconds(expirySeconds),
//             Value = value
//         };
//         lock(Cache)
//         {
//             if (Cache.ContainsKey(uid))
//                 Cache[uid] = co;
//             else
//                 Cache.Add(uid, co);
//         }
//     }
//
//     /// <summary>
//     /// Removes an item from the cache store if exists in it
//     /// If it does not exist, it will just return, no exception
//     /// </summary>
//     /// <param name="uid">the UID of the item to remove</param>
//     public void Remove(Guid uid) => Remove(uid.ToString());
//     
//     /// <summary>
//     /// Removes an item from the cache store if exists in it
//     /// If it does not exist, it will just return, no exception
//     /// </summary>
//     /// <param name="uid">the UID of the item to remove</param>
//     public void Remove(string uid)
//     {
//         lock (Cache)
//         {
//             if(Cache.ContainsKey(uid))
//                 Cache.Remove(uid);
//         }
//     }
//
//     private class CachedObject
//     {
//         public object Value { get; set; }
//         public DateTime Expiry { get; set; }
//     }
// }