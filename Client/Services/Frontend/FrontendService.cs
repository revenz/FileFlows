using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using FileFlows.Client.Services.Frontend.Handlers;
using Jint.Native.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using HttpMethod = System.Net.Http.HttpMethod;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace FileFlows.Client.Services.Frontend;

/// <summary>
/// Service used to store all front end data
/// All queues, recently finished, paused status etc, is stored in here
/// and the server will pipe updates directly to this service.
/// This service will broadcast events which components can subscribe to update
/// </summary>
public class FrontendService : IAsyncDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly NavigationManager _navigationManager;
    private readonly FFLocalStorageService _ffLocalStorage;
    private CancellationTokenSource _cts;
    private Task _listeningTask;
    
    /// <summary>
    /// Called when the connection is lost or reestablished
    /// </summary>
    public event Action<bool>? OnConnectionLost;
    
    /// <summary>
    /// Gets if the connection is lost
    /// </summary>
    public bool ConnectionLost { get; private set; }

    /// <summary>
    /// Gets the register that is called when a event is received
    /// </summary>
    public FrontendRegister Registry { get; init; } = new ();
    /// <summary>
    /// Gets or sets if this has been initialized
    /// </summary>
    public bool IsInitialized { get; private set; }
    /// <summary>
    /// Event triggered when the service is initialized for the first time.
    /// Multiple subscribers can listen to this.
    /// </summary>
    public event Action? OnInitialized;

    /// <summary>
    /// Gets or sets the dashboard handler
    /// </summary>
    public DashboardHandler Dashboard { get;private set; }
    /// <summary>
    /// Gets or sets the node handler
    /// </summary>
    public NodeHandler Node { get;private set; }
    /// <summary>
    /// Gets or sets the profile handler
    /// </summary>
    public ProfileHandler Profile { get;private set; }
    /// <summary>
    /// Gets or sets the file handler
    /// </summary>
    public FileHandler Files { get;private set; }
    /// <summary>
    /// Gets or sets the flow handler
    /// </summary>
    public FlowHandler Flow { get;private set; }
    /// <summary>
    /// Gets or sets the library handler
    /// </summary>
    public LibraryHandler Library { get;private set; }
    /// <summary>
    /// Gets or sets the plugin handler
    /// </summary>
    public PluginHandler Plugin { get;private set; }
    /// <summary>
    /// Gets or sets the script handler
    /// </summary>
    public ScriptHandler Script { get;private set; }
    /// <summary>
    /// Gets or sets the report handler
    /// </summary>
    public ReportHandler Report { get;private set; }
    /// <summary>
    /// Gets or sets the runner handler
    /// </summary>
    public RunnerHandler Runner { get;private set; }
    /// <summary>
    /// Gets or sets the tag handler
    /// </summary>
    public TagHandler Tag { get;private set; }
    /// <summary>
    /// Gets or sets the variable handler
    /// </summary>
    public VariableHandler Variable { get;private set; }
    /// <summary>
    /// Gets or sets the DockerMod handler
    /// </summary>
    public DockerModHandler DockerMod { get;private set; }
    /// <summary>
    /// Gets or sets the runner handler
    /// </summary>
    public SystemHandler System { get;private set; }

    /// <summary>
    /// Gets or sets the page size
    /// </summary>
    public int PageSize { get; set; } = 500;

    /// <summary>
    /// Initializes a new instance of the <see cref="FrontendService"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    /// <param name="navigationManager">The Blazor navigation manager.</param>
    /// <param name="ffLocalStorageService">the local storage service</param>
    public FrontendService(IServiceProvider serviceProvider, NavigationManager navigationManager, FFLocalStorageService ffLocalStorageService)
    {
        _serviceProvider = serviceProvider;
        _navigationManager = navigationManager;
        _ffLocalStorage = ffLocalStorageService;
    }

    /// <summary>
    /// Starts listening to the specified SSE endpoint.
    /// </summary>
    /// <param name="endpoint">The relative URL of the SSE endpoint.</param>
    /// <param name="authToken">The authorization token</param>
    public void StartListening(string endpoint, string authToken)
    {
        if (_cts != null)
            return; // Already listening

        _cts = new CancellationTokenSource();
        _listeningTask = Task.Run(() => ListenToSSE(endpoint, authToken, _cts.Token));
    }

    /// <summary>
    /// Listens to the SSE stream and handles reconnection attempts.
    /// </summary>
    /// <param name="endpoint">The SSE endpoint to connect to.</param>
    /// <param name="authToken">The authorization token</param>
    /// <param name="cancellationToken">The cancellation token for stopping the connection.</param>
    private async Task ListenToSSE(string endpoint, string authToken, CancellationToken cancellationToken)
    {
        string url = _navigationManager.ToAbsoluteUri(endpoint).ToString();
        int retryDelay = 1000; // Start with 1 second delay

        DateTime connectionLostAt = DateTime.MaxValue;
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();

                if (IsInitialized == false)
                    url += "?initialData=true";
                if(App.Instance.IsMobile)
                    url += (IsInitialized ? "?" : "&") + "mobile=true";

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.SetBrowserResponseStreamingEnabled(true);
                request.Headers.Add("Accept", "text/event-stream");
                if(string.IsNullOrWhiteSpace(authToken) == false)
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                    
                using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _navigationManager.NavigateTo("/login", true);
                    return;
                }

                if (response.StatusCode == HttpStatusCode.UnavailableForLegalReasons)
                {
                    if (response.Headers.Location != null)
                    {
                        string redirectUrl = response.Headers.Location.IsAbsoluteUri
                            ? response.Headers.Location.ToString() // Convert absolute URI to string
                            : "/"; // Resolve relative URI

                        _navigationManager.NavigateTo(redirectUrl, true);
                        return; // Ensure we completely exit the SSE listener
                    }
                }

                response.EnsureSuccessStatusCode();

                if (ConnectionLost)
                {
                    connectionLostAt = DateTime.MaxValue;
                    ConnectionLost = false;
                    OnConnectionLost?.Invoke(false);
                }

                if (System.Upgrading)
                {
                    System.TriggerUpgraded();
                }

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var reader = new StreamReader(stream);

                // Read the stream asynchronously, line by line
                while (!cancellationToken.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync(cancellationToken);
                    if (line == null)
                    {
                        // End of stream
                        break;
                    }

                    if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:") || line.Length <= 5)
                        continue;

                    var message = line[5..].Trim();
                    string len = message.Length > 1024 ? $"{(message.Length / 1024):#.##} KB" : $"{message.Length} Bytes";
                        
                    try
                    {
                        if (message.StartsWith("x:"))  // 🔥 Check if it's compressed
                        {
                            var payload = message[2..]; // Remove "x:"
                            int firstColonIndex = payload.IndexOf(':');
                            if (firstColonIndex > 0)
                            {
                                string eventName = payload[..firstColonIndex];
                                string compressedBase64 = payload[(firstColonIndex + 1)..];

                                // 🔥 Decode and decompress
                                byte[] compressedBytes = Convert.FromBase64String(compressedBase64);
                                string decompressedJson = DecompressDeflate(compressedBytes);

                                // 🔥 Restore original format: "eventName:decompressedJson"
                                message = $"{eventName}:{decompressedJson}";
                            }
                        }
                        
                        if(message.Length > 30 * 1024) // only log bigger messsages
                            Console.WriteLine($"SSE message: {message.Split(':')[0]} : {len}"); 
                        
                        if (message.StartsWith("id:"))
                            Initialize(message[3..]);
                        else
                            await Registry.HandleRequest(message);
                    }
                    catch (Exception)
                    {
                        // ignore, keep reading
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.WLog($"SSE connection lost: {ex.Message}. Retrying in {retryDelay} ms");
            }

            if (System.ReceivedUpgradingEvent)
            {
                ConnectionLost = true;
                System.TriggerUpgrading();
            }

            if (connectionLostAt == DateTime.MaxValue)
            {
                connectionLostAt = DateTime.Now;
            }
            else if (connectionLostAt < DateTime.Now.AddSeconds(-10) && ConnectionLost == false)
            {
                ConnectionLost = true;
                OnConnectionLost?.Invoke(true);
            }
            
            // Exponential backoff (max 60s)
            await Task.Delay(retryDelay, cancellationToken);
            retryDelay = Math.Min(retryDelay * 2, 15000);
        }
    }


    /// <summary>
    /// Helper method to decompress Deflate
    /// </summary>
    /// <param name="compressedData">the compressed data</param>
    /// <returns>the uncompressed string</returns>
    static string DecompressDeflate(byte[] compressedData)
    {
        using var inputStream = new MemoryStream(compressedData);
        using var deflateStream = new DeflateStream(inputStream, CompressionMode.Decompress);
        using var reader = new StreamReader(deflateStream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
    
    /// <summary>
    /// Called only once, after the first successful SSE connection.
    /// Override this method in a derived class if needed.
    /// </summary>
    protected void Initialize(string idJson)
    {
        Logger.Instance.ILog("FrontendService initialized after first successful SSE connection.");
        var data = JsonSerializer.Deserialize<InitialClientData>(idJson);
        // Add any initialization logic here
        PageSize = data.PageSize;
        Profile = new(this, _ffLocalStorage);
        Profile.Initialize(data);
        Dashboard = new(this);
        Dashboard.Initialize(data);
        Node = new(this);
        Node.Initialize(data);
        Files = new(this);
        Files.Initialize(data);
        Flow = new(this);
        Flow.Initialize(data);
        Library = new(this);
        Library.Initialize(data);
        Tag = new(this);
        Tag.Initialize(data);
        Variable = new(this);
        Variable.Initialize(data);
        DockerMod = new(this);
        DockerMod.Initialize(data);
        Plugin = new(this);
        Plugin.Initialize(data);
        Script = new(this);
        Script.Initialize(data);
        Report = new(this);
        Report.Initialize(data);
        Runner = new(this);
        Runner.Initialize(data);
        System = new(this);
        System.Initialize(data);
        IsInitialized = true;
        OnInitialized?.Invoke();
    }

    
    /// <summary>
    /// Disposes of the service and cancels the SSE connection.
    /// </summary>
    /// <returns>A task representing the asynchronous dispose operation.</returns>
    public async ValueTask DisposeAsync()
    {
        await _cts?.CancelAsync();
        await (_listeningTask ?? Task.CompletedTask);
        _cts?.Dispose();
    }
}
