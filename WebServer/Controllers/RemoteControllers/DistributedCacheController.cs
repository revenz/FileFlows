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
    /// The service
    /// </summary>
    private readonly DistributedCacheService service;

    public DistributedCacheController()
    {
        service = ServiceLoader.Load<DistributedCacheService>();
    }

    /// <summary>
    /// Gets a cached item by key.
    /// </summary>
    [HttpGet("{key}")]
    public IActionResult Get(string key)
    {
        var result = service.Get<object>(key);
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
        
        service.Store(data.Key, data.Value, data.Expiration);
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
        /// Gets the value to store
        /// </summary>
        public object Value { get; init; }
        /// <summary>
        /// Gets the expiration of the data being stored
        /// </summary>
        public TimeSpan? Expiration { get; init; }
    }
}

