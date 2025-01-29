namespace FileFlows.WebServer.Controllers.RemoteControllers;

/// <summary>
/// Controller for interacting with the cache
/// </summary>
[Route("/remote/cache")]
[FileFlowsApiAuthorize]
[ApiExplorerSettings(IgnoreApi = true)]
public class DistributedCacheController : Controller
{

    /// <summary>
    /// Gets a cached item by key.
    /// </summary>
    [HttpGet("{key}")]
    public IActionResult Get(string key)
    {
        var service = ServiceLoader.Load<DistributedCacheService>();
        var result = service.GetJson(key);
        return result is not null ? Ok(result) : NotFound();
    }

    /// <summary>
    /// Stores an item in the cache.
    /// </summary>
    [HttpPost]
    public IActionResult Store([FromBody] CachePostData data)
    {
        if(data == null)
            return BadRequest("No data to store");
        
        var service = ServiceLoader.Load<DistributedCacheService>();
        service.StoreJson(data.Key, data.Json, data.Expiration);
        return Ok();
    }

    /// <summary>
    /// The post data
    /// </summary>
    public class CachePostData
    {
        /// <summary>
        /// Gets the key to store
        /// </summary>
        public string Key { get; init; }
        /// <summary>
        /// Gets the json to store
        /// </summary>
        public string Json { get; init; }
        /// <summary>
        /// Gets the expiration of the data being stored
        /// </summary>
        public TimeSpan? Expiration { get; init; }
    }
}

