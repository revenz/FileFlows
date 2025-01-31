namespace FileFlows.WebServer.Middleware;

/// <summary>
/// Middleware for the UI
/// </summary>
public class UiMiddleware
{
    
    private readonly RequestDelegate _next;

    /// <summary>
    /// Constructs a instance of the exception middleware
    /// </summary>
    /// <param name="next">the next middleware to call</param>
    public UiMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Invokes the middleware
    /// </summary>
    /// <param name="context">the HttpContext executing this middleware</param>
    public async Task Invoke(HttpContext context)
    {
        int filesRemaining = 100;
        context.Response.Headers.TryAdd("x-files-remaining", filesRemaining.ToString());
        
        await _next(context);
    }
}