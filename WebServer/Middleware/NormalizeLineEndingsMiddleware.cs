using System.Text;

namespace FileFlows.WebServer.Middleware;

/// <summary>
/// Middleware to normalize line endings in the request body by replacing all \r\n with \n.
/// </summary>
public class NormalizeLineEndingsMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes a new instance of the <see cref="NormalizeLineEndingsMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the request pipeline.</param>
    public NormalizeLineEndingsMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Invokes the middleware to process the request.
    /// </summary>
    /// <param name="context">The HTTP context of the current request.</param>
    /// <returns>A task representing the completion of the middleware execution.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        // Check if the Content-Type is text, json or xml
        var contentType = context.Request.ContentType;
        if (contentType == null || 
            (contentType.StartsWith("text/") || 
              contentType.Equals("application/json", StringComparison.OrdinalIgnoreCase) ||
              contentType.Equals("application/xml", StringComparison.OrdinalIgnoreCase)) == false)
        {
            // Call the next middleware in the pipeline if not the right content type
            await _next(context);
            return;
        }
            
        // Enable seeking on the request body stream
        context.Request.EnableBuffering();

        // Read the request body as a string
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();

        #if(DEBUG)
        if (body.IndexOf("\r\n", StringComparison.Ordinal) >= 0)
        {
            // Normalize line endings
            body = body.Replace("\r\n", "\n");
        }
        #else
        body = body.Replace("\r\n", "\n");
        #endif

        // Write the normalized body back to the request stream
        var bytes = Encoding.UTF8.GetBytes(body);
        context.Request.Body = new MemoryStream(bytes);
        context.Request.Body.Seek(0, SeekOrigin.Begin);

        // Call the next middleware in the pipeline
        await _next(context);
    }
}