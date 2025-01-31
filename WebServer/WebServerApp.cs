using FileFlows.Server.Workers;
using Microsoft.OpenApi.Models;
using System.Runtime.InteropServices;
using FileFlows.WebServer.Filters;
using FileFlows.WebServer.Hubs;
using FileFlows.WebServer.Middleware;
using Microsoft.AspNetCore.SignalR;
using HttpMethod = System.Net.Http.HttpMethod;
using ServiceLoader = FileFlows.Services.ServiceLoader;
using SettingsService = FileFlows.Services.SettingsService;

namespace FileFlows.WebServer;

/// <summary>
/// Web server state
/// </summary>
public enum WebServerState
{
    /// <summary>
    /// Web Server is Starting
    /// </summary>
    Starting,
    /// <summary>
    /// Web Server is Listening
    /// </summary>
    Listening,
    /// <summary>
    /// Web Server failed to start
    /// </summary>
    Error
}

/// <summary>
/// Web Server for the FileFlows Server
/// </summary>
public class WebServerApp
{
    private static WebApplication app;
    
    /// <summary>
    /// Represents a method that handles status update events from the web server.
    /// </summary>
    /// <param name="state">The current state of the web server.</param>
    /// <param name="message">A message associated with the status update.</param>
    /// <param name="url">The URL the server is listening on </param>
    public delegate void StatusUpdateHandler(WebServerState state, string message, string url);

    /// <summary>
    /// Event that occurs when the status of the web server is updated.
    /// </summary>
    public static event StatusUpdateHandler OnStatusUpdate;

    /// <summary>
    /// Gets or sets if the web server started
    /// </summary>
    public static bool Started { get; private set; }

    /// <summary>
    /// Gets or sets a error when starting the web server
    /// </summary>
    public static string StartError { get; private set; }

    /// <summary>
    /// Gets or sets the port
    /// </summary>
    public static int Port { get; private set; }

    /// <summary>
    /// Gets or sets if the web server has fully started and no longer in a starting state
    /// </summary>
    public static bool FullyStarted { get; set; }

    /// <summary>
    /// Stops the server
    /// </summary>
    public static async Task Stop()
    {
        if (app == null)
            return;
        await app.StopAsync();
    }

    /// <summary>
    /// Gets the server address
    /// </summary>
    /// <param name="args">the command line arguments</param>
    /// <returns>the server address</returns>
    public static string GetServerUrl(string[] args)
    {
        bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        string protocol = "http";
        var appSettings = ServiceLoader.Load<AppSettingsService>().Settings;
        Port = appSettings.ServerPort ?? 19200;
#if (DEBUG)
        Port = 6868;
#endif

        var url = args?.Where(x => x?.StartsWith("--urls=") == true)?.FirstOrDefault();
        if (string.IsNullOrEmpty(url) == false)
        {
            var portMatch = Regex.Match(url, @"(?<=(:))[\d]+");
            if (portMatch.Success)
                Port = int.Parse(portMatch.Value);
            if (url.StartsWith("https"))
                protocol = "https";
        }

        if (int.TryParse(Environment.GetEnvironmentVariable("Port"), out int port) && port is > 0 and <= 65535)
            Port = port;
        if (Environment.GetEnvironmentVariable("HTTPS") == "1")
            protocol = "https";

        string serverUrl = $"{protocol}://0.0.0.0:{Port}/";
        Logger.Instance.ILog("Server URL: " + serverUrl);
        return serverUrl;
    }

    /// <summary>
    /// Starts the server
    /// </summary>
    /// <param name="args">command line arguments</param>
    public static void Start(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.WebHost.ConfigureKestrel((context, options) =>
        {
            // remove the file upload limit so the File Service can receive larger files
            options.Limits.MaxRequestBodySize = null; // Set to null to remove the limit
        });

        string serverUrl = GetServerUrl(args);

        Logger.Instance.ILog("Started web server: " + serverUrl);
        Task.Run(() => OnStatusUpdate?.Invoke(WebServerState.Starting, "Starting web server", serverUrl));

        // Add services to the container.

        // Dynamically register services from the console application's service provider
        builder.Services.AddSingleton<AppSettingsService>(x => ServiceLoader.Load<AppSettingsService>());
        builder.Services.AddSingleton<SettingsService>(x => (SettingsService)ServiceLoader.Load<ISettingsService>());
        // builder.Services.AddSingleton<ProfileService>(x => ServiceLoader.Load<ProfileService>());

        var appSettings = ServiceLoader.Load<AppSettingsService>().Settings;

        builder.Services.AddServerSideBlazor();
        
        builder.Services.AddControllersWithViews(options =>
        {
            if(appSettings.DatabaseType != DatabaseType.Sqlite)
                options.Filters.Add<DatabaseExceptionFilter>();
            
            options.Filters.Add<TrimStringsFilter>();
        });
        builder.Services.AddSignalR(options =>
        {
            options.MaximumReceiveMessageSize = 1048576; // Set to 1 MB (1048576 bytes)
        });
        builder.Services.AddResponseCompression();
        builder.Services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = null;
            options.JsonSerializerOptions.DefaultIgnoreCondition =
                System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault |
                System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        });
        builder.Services.AddMvc();
        
        
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "FileFlows", Version = "v1" });

            if (LicenseService.IsLicensed(LicenseFlags.UserSecurity) && appSettings.Security != SecurityMode.Off)
            {
                c.AddSecurityDefinition("Bearer", new()
                {
                    Type = SecuritySchemeType.ApiKey,
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Description = "Authorization bearer token"
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type=ReferenceType.SecurityScheme,
                                Id="Bearer"
                            }
                        },
                        new string[]{}
                    }
                });
            }

            var filePath = Path.Combine(System.AppContext.BaseDirectory, "FileFlows.Server.xml");
            if (File.Exists(filePath))
                c.IncludeXmlComments(filePath);
            else
            {
                filePath = Path.Combine(System.AppContext.BaseDirectory, "FileFlows.xml");
                if (File.Exists(filePath))
                    c.IncludeXmlComments(filePath);
            }
        });

        app = builder.Build();

        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.IndexStream = () =>
                typeof(WebServerApp).Assembly.GetManifestResourceStream("FileFlows.WebServer.Resources.SwaggerIndex.html");

            c.RoutePrefix = "api/help";
            c.DocumentTitle = "FileFlows API";
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "FileFlows API");
            c.InjectStylesheet("/css/swagger.min.css");
        });

        app.UseDefaultFiles();

        var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
        provider.Mappings[".br"] = "text/plain";

        //var wwwroot = Path.Combine(DirectoryHelper.BaseDirectory, "Server", "wwwroot");
        app.UseStaticFiles(new StaticFileOptions
        {
            //FileProvider = new PhysicalFileProvider(wwwroot),
            ContentTypeProvider = provider,
            OnPrepareResponse = x =>
            {
                if (x?.File?.PhysicalPath?.ToLower()?.Contains("_framework") == true)
                    return;
                if (x?.File?.PhysicalPath?.ToLower()?.Contains("_content") == true)
                    return;
                x?.Context?.Response?.Headers?.Append("Cache-Control", "no-cache");
            }
        });

        app.UseMiddleware<LoadingMiddleware>(); // must be first, it will prevent any others
        app.UseMiddleware<ExceptionMiddleware>();
        app.UseMiddleware<LoggingMiddleware>();
        app.UseMiddleware<FileFlowsIPAddressAuthorizeFilter>();
        app.UseMiddleware<NormalizeLineEndingsMiddleware>();
        // this is an experiment, may reuse it one day
        //app.UseMiddleware<UiMiddleware>();
        app.UseRouting();


        Globals.IsDevelopment = app.Environment.IsDevelopment();

        if (Globals.IsDevelopment)
            app.UseCors(builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader().WithExposedHeaders("*"));
        else
            app.UseMiddleware<RemoteCorsMiddleware>();

        // this will allow refreshing from a SPA url to load the index.html file
        // app.MapWhen(context => FullyStarted, builder =>
        // {
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.MapControllerRoute(
                name: "Spa",
                pattern: "{*url}",
                defaults: new { controller = "Home", action = "Index" }
            );
        //});

        app.MapHub<FlowHub>("/flow");
        app.MapHub<ClientServiceHub>("/client-service");

        app.UseResponseCompression();

        // this will run the asp.net app and wait until it is killed
        Logger.Instance.ILog("Running FileFlows Server");

        var _clientServiceHub = app.Services.GetRequiredService<IHubContext<ClientServiceHub>>();
        ServiceLoader.AddSpecialCase<IClientService>(new ClientServiceManager(_clientServiceHub));

        app.MapBlazorHub(); // This is necessary for Blazor Server-Side
        
        Task task = app.RunAsync(serverUrl);

        _ = Task.Run(async () =>
        {
            await Task.Delay(750);
            OnStatusUpdate?.Invoke(WebServerState.Listening, "Web server listening", serverUrl);
        });
        
        if (RunStartupCode(serverUrl).Failed(out string error))
        {
            Logger.Instance.ELog("Startup failed: " + error);
            if (Globals.UsingWebView)
                Thread.Sleep(10_000);

            return;
        }
        
        task.Wait();
        Logger.Instance.ILog("Finished running FileFlows Server");
        WorkerManager.StopWorkers();
    }

    /// <summary>
    /// Runs the startup code
    /// </summary>
    /// <returns>the result</returns>
    private static Result<bool> RunStartupCode(string serverUrl)
    {
        var service = ServiceLoader.Load<IStartupService>();
        service.OnStatusUpdate += (message, _, _) =>
        {
            Task.Run(() => OnStatusUpdate?.Invoke(WebServerState.Starting, message, string.Empty));
        };
        return service.Run(serverUrl);
    }


    private static async Task<bool> CheckServerListening(string url)
    {
        using var handler = new HttpClientHandler();
        // Ignore all certificate errors
        handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

        using var client = new HttpClient(handler);

        client.Timeout = TimeSpan.FromSeconds(30); // Set the timeout to 30 seconds
        var settings = ServiceLoader.Load<AppSettingsService>().Settings;
        var accessToken = (await ServiceLoader.Load<ISettingsService>().Get()).AccessToken;

        for (int i = 0; i < 15; i++) // Retry 15 times (15 * 2 seconds = 30 seconds)
        {
            try
            {
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get ,
                    RequestUri = new Uri(url, UriKind.RelativeOrAbsolute)
                };
                if (settings.Security != SecurityMode.Off && LicenseService.IsLicensed(LicenseFlags.UserSecurity) && string.IsNullOrWhiteSpace(accessToken) == false)
                {
                    request.Headers.Add("x-token", accessToken);
                }

                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    // If successful, the server is actively listening
                    return true;
                }
                // If the status code is not success, wait for 2 seconds before retrying
                await Task.Delay(2000);
            }
            catch (Exception)
            {
                // If an exception occurs, wait for 2 seconds before retrying
                await Task.Delay(2000);
            }
        }

        // If the server fails to bind after retrying, return false
        return false;
    }
}