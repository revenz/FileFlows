namespace FileFlows.WebServer.Middleware;

/// <summary>
/// Middleware to handle CORS policies for specific routes.
/// </summary>
public class RemoteCorsMiddleware
{
    /// <summary>
    /// The next request delegate in the pipeline.
    /// </summary>
    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteCorsMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next request delegate in the pipeline.</param>
    public RemoteCorsMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Invokes the middleware, adding CORS headers if the request path starts with /remote.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        // Check if the request path starts with /remote
        if (context.Request.Path.StartsWithSegments("/remote") == false)
        {
            await _next(context);
            return;
        }

        // Apply CORS policy for /remote routes
        context.Response.Headers.AccessControlAllowOrigin = "*";
        context.Response.Headers.AccessControlAllowMethods = "GET, POST, PUT, DELETE, OPTIONS";
        context.Response.Headers.AccessControlAllowHeaders = "Content-Type, Authorization, x-token";
        
        if (context.Request.Method == HttpMethods.Options)
            context.Response.StatusCode = StatusCodes.Status200OK; // No content needed
        else
            await _next(context);
    }
}