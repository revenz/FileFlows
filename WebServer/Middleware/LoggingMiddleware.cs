namespace FileFlows.WebServer.Middleware;

/// <summary>
/// A middleware used to log all requests
/// </summary>
public class LoggingMiddleware
{
    /// <summary>
    /// Next request delegate
    /// </summary>
    private readonly RequestDelegate _next;

    private SettingsService _settingsService;
    /// <summary>
    /// Settings service
    /// </summary>
    private SettingsService SettingsService
    {
        get
        {
            if (_settingsService == null)
                _settingsService = (SettingsService)ServiceLoader.Load<ISettingsService>();
            return _settingsService;
        }
    }
    
    
    /// <summary>
    /// Gets the logger for the request logger
    /// </summary>
    public static FileLogger RequestLogger { get; private set; }

    /// <summary>
    /// Constructs a instance of the exception middleware
    /// </summary>
    /// <param name="next">the next middleware to call</param>
    public LoggingMiddleware(RequestDelegate next)
    {
        _next = next;
        RequestLogger = new FileLogger(DirectoryHelper.LoggingDirectory, "FileFlowsHTTP", register: false);
    }

    /// <summary>
    /// Invokes the middleware
    /// </summary>
    /// <param name="context">the HttpContext executing this middleware</param>
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        finally
        {
            try
            {
                if (WebServerApp.FullyStarted && context.Request != null)
                {   
                    LogType logType = LogType.Info;
                    int statusCode = context.Response?.StatusCode ?? 0;
                    
                    if (context.Request.Path.Value?.Contains("remote/library-file/manually-add") == true)
                        logType = LogType.Debug;
                    if (statusCode is >= 300 and < 400)
                        logType = LogType.Warning;
                    else if (statusCode is >= 400 and < 500)
                        logType = LogType.Warning;
                    else if (statusCode >= 500)
                        logType = LogType.Error;
                    
                    if (logType != LogType.Info || SettingsService.Get().Result.LogEveryRequest)
                    {
                        _ = RequestLogger.Log(logType,
                            $"[{VerbPad(context.Request.Method, 7)}] [{context.Response?.StatusCode}]: {context.Request?.Path.Value}");
                    }
                }
            }
            catch (Exception)
            {
                // Ignored
            }
        }
    }
    /// <summary>
    /// Pads the HTTP method to a fixed width, centering it with spaces.
    /// </summary>
    /// <param name="method">The HTTP method (e.g., GET, POST).</param>
    /// <param name="width">The total width to which the method should be padded.</param>
    /// <returns>A string representing the padded HTTP method.</returns>
    public static string VerbPad(string method, int width)
    {
        if (width < method.Length)
        {
            throw new ArgumentException("Width must be greater than or equal to the length of the method.");
        }

        // Calculate the padding required on each side
        int totalPadding = width - method.Length;
        int leftPadding = totalPadding / 2;
        int rightPadding = totalPadding - leftPadding;

        // Create the padded method
        string paddedMethod = new string(' ', leftPadding) + method + new string(' ', rightPadding);
        return paddedMethod;
    }
}
