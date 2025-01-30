// using Jint.Native;
//
// namespace FileFlows.ScriptExecution;
//
// /// <summary>
// /// The cache store that is static across the entire process
// /// </summary>
// public class CacheStore
// {
//     /// <summary>
//     /// The dictionary of values
//     /// </summary>
//     private static readonly Dictionary<string, object> _Values = new();
//     /// <summary>
//     /// The instance
//     /// </summary>
//     private static readonly CacheStore _Instance = new CacheStore();
//     /// <summary>
//     /// Gets the instance
//     /// </summary>
//     public static CacheStore Instance => _Instance;
//
//     /// <summary>
//     /// Gets a value from the store
//     /// </summary>
//     /// <param name="key">the key to get</param>
//     /// <returns>the value if in the store</returns>
//     private object? GetValue(string key)
//     {
//         object? value;
//         if(_Values.TryGetValue(key, out value))
//             return value;
//         return null;
//     }
//
//
//     /// <summary>
//     /// Clears the cache store
//     /// </summary>
//     public void Clear()
//         => _Values.Clear();
//
//     /// <summary>
//     /// Gets a object in the store
//     /// </summary>
//     /// <param name="key">the name of the object to set</param>
//     /// <returns>the value</returns>
//     public object? Get(string key)
//         => GetValue(key);
//     
//     
//     /// <summary>
//     /// Gets a int in the store
//     /// </summary>
//     /// <param name="key">the name of the object to set</param>
//     /// <returns>the value</returns>
//     public int GetInt(string key)
//         => GetValue(key) as int? ?? 0;
//     
//     /// <summary>
//     /// Gets a bolo in the store
//     /// </summary>
//     /// <param name="key">the name of the object to set</param>
//     /// <returns>the value</returns>
//     public bool GetBool(string key)
//         => GetValue(key) as bool? ?? false;
//     
//     /// <summary>
//     /// Gets a sting in the store
//     /// </summary>
//     /// <param name="key">the name of the object to set</param>
//     /// <returns>the value</returns>
//     public string GetString(string key)
//         => GetValue(key) as string ?? string.Empty;
//
//     
//     /// <summary>
//     /// Sets a object in the store
//     /// </summary>
//     /// <param name="key">the name of the object to set</param>
//     /// <param name="value">the value</param>
//     public void Set(string key, object value)
//         => _Values[key] = value;
//     
//     /// <summary>
//     /// Sets an int in the cache store
//     /// </summary>
//     /// <param name="key">the name of the object to set</param>
//     /// <param name="value">the value</param>
//     public void SetInt(string key, int value)
//         => _Values[key] = value;
//     
//     /// <summary>
//     /// Sets an string in the cache store
//     /// </summary>
//     /// <param name="key">the name of the object to set</param>
//     /// <param name="value">the value</param>
//     public void SetString(string key, string value)
//         => _Values[key] = value;
//     
//     /// <summary>
//     /// Sets an bool in the cache store
//     /// </summary>
//     /// <param name="key">the name of the object to set</param>
//     /// <param name="value">the value</param>
//     public void SetBool(string key, bool value)
//         => _Values[key] = value;
// }