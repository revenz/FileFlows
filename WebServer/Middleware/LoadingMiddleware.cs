namespace FileFlows.WebServer.Middleware;

public class LoadingMiddleware
{
    
    private readonly RequestDelegate _next;

    /// <summary>
    /// Constructs a instance of the loading middleware
    /// </summary>
    /// <param name="next">the next middleware to call</param>
    public LoadingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Invokes the middleware
    /// </summary>
    /// <param name="context">the HttpContext executing this middleware</param>
    public async Task Invoke(HttpContext context)
    {
        if (WebServerApp.FullyStarted == true)
        {
            await _next(context);
            return;
        }

        if (context.Request.Path.StartsWithSegments("/remote"))
        {
            context.Response.StatusCode = 500;
            return;
        }
        if (context.Request.Path.StartsWithSegments("/frasier"))
        {
            // no body, this avoids error in js console, but doesn't trigger the reset
            context.Response.StatusCode = 200;
            return;
        }

        if (context.Request.Path.StartsWithSegments("/_framework") || context.Request.Path.StartsWithSegments("/_blazor"))
        {
            await _next(context);
            return;
        }
        if (context.Request.Path.StartsWithSegments("/loading"))
        {
            await _next(context);
            return;
        }

        context.Response.Redirect("/loading");
    }
}