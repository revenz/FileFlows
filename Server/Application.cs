using System.Collections;
using System.Net;
using Avalonia;
using FileFlows.Charting;
using FileFlows.Server.DefaultTemplates;
using FileFlows.Server.Helpers;
using FileFlows.Server.Services;
using FileFlows.Services;
using FileFlows.Shared.Helpers;
using FileFlows.WebServer;

namespace FileFlows.Server;

/// <summary>
/// The main FileFlows server application
/// </summary>
public class Application
{
    /// <summary>
    /// The Application named used by the appMutex
    /// </summary>
    const string appName = "FileFlowsServer";
    
    /// <summary>
    /// The application mutex to ensure only one instance of FileFlows can run
    /// </summary>
    private static Mutex appMutex = null;

    /// <summary>
    /// Gets or sets an optional entry point that launched this
    /// </summary>
    public static string? EntryPoint { get; internal set; }

    /// <summary>
    /// Gets if this is running inside a docker container
    /// </summary>
    public static bool Docker => Globals.IsDocker;


    /// <summary>
    /// Gets or sets if should show the minimal GUI
    /// </summary>
    public static bool ShowMinimalGui { get; set; }

    /// <summary>
    /// Gets or sets if should show the GUI
    /// </summary>
    public static bool ShowGui { get; set; }

    /// <summary>
    /// Gets the running UID which will be used to verify remote requests come from this running instance
    /// We cant use the InternalNodeUid as thats fixed across all installs
    /// </summary>
    internal static readonly Guid RunningUid = Guid.NewGuid();

    /// <summary>
    /// Runst the application
    /// </summary>
    /// <param name="args">the command line arguments</param>
    public void Run(string[] args)
    {
        try
        {
            bool noGui = ShowGui == false && ShowMinimalGui == false;

            if (string.IsNullOrWhiteSpace(EntryPoint) == false && OperatingSystem.IsMacOS())
                File.WriteAllText(Path.Combine(DirectoryHelper.BaseDirectory, "version.txt"),
                    Globals.Version.Split('.').Last());

            if (noGui == false && Globals.IsWindows)
            {
                // hide the console on window
                Utils.WindowsConsoleManager.Hide();
            }

            if (Docker == false)
            {
                appMutex = new Mutex(true, appName, out bool createdNew);
                if (createdNew == false)
                {
                    // app is already running;
                    if (noGui)
                    {
                        Console.WriteLine("An instance of FileFlows is already running");
                    }
                    else
                    {
                        try
                        {
                            var appBuilder = Gui.Avalon.App.BuildAvaloniaApp(true);
                            appBuilder.StartWithClassicDesktopLifetime(args);
                        }
                        catch (Exception)
                        {
                        }
                    }

                    return;
                }
            }

            DirectoryHelper.Init();

            if (File.Exists(Path.Combine(DirectoryHelper.BaseDirectory, "server-upgrade.bat")))
                File.Delete(Path.Combine(DirectoryHelper.BaseDirectory, "server-upgrade.bat"));
            if (File.Exists(Path.Combine(DirectoryHelper.BaseDirectory, "server-upgrade.sh")))
                File.Delete(Path.Combine(DirectoryHelper.BaseDirectory, "server-upgrade.sh"));

            ServicePointManager.DefaultConnectionLimit = 50;

            InitializeLoggers();

            RegisterServerServices();
            
            WriteLogHeader(args);

            HttpHelper.Client = HttpHelper.GetDefaultHttpClient(string.Empty);

            if (Docker || noGui)
            {
                Console.WriteLine("Starting FileFlows Server...");
                WebServerApp.Start(args);
            }
            else if (ShowGui)
            {
                Globals.UsingWebView = true;

                var webview = new Gui.Photino.WebView();
                _ = Task.Run(async () =>
                {
                    // this fixes a bug on windows that if the webview hasnt opened, it has a memory access exception
                    do
                    {
                        await Task.Delay(100);
                    } while (webview.Opened == false);

                    try
                    {
                        Logger.Instance.ILog("Starting FileFlows Server...");
                        WebServerApp.Start(args);
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.ELog("Failed starting server: " + ex.Message + Environment.NewLine +
                                             ex.StackTrace);
                    }
                });
                webview.Open();
            }
            else if (ShowMinimalGui)
            {
                // do this first, to populate the Port so the Minimal UI shows the correct url
                string serverUrl = WebServerApp.GetServerUrl(args);
                _ = Task.Run(() =>
                {
                    try
                    {
                        Logger.Instance.ILog("Starting FileFlows Server...");
                        WebServerApp.Start(args);
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.ELog("Failed starting server: " + ex.Message + Environment.NewLine +
                                             ex.StackTrace);
                    }
                });
                DateTime dt = DateTime.UtcNow;
                try
                {
                    var appBuilder = Gui.Avalon.App.BuildAvaloniaApp();
                    appBuilder.StartWithClassicDesktopLifetime(args);
                }
                catch (Exception ex)
                {
                    if (DateTime.UtcNow.Subtract(dt) < new TimeSpan(0, 0, 2))
                        Console.WriteLine("Failed to launch GUI: " + ex.Message + Environment.NewLine + ex.StackTrace);
                    else
                        Console.WriteLine("Error: " + ex.Message + Environment.NewLine + ex.StackTrace);
                }
            }

            _ = WebServerApp.Stop();
            Console.WriteLine("Exiting FileFlows Server...");
        }
        catch (Exception ex)
        {
            try
            {
                Logger.Instance.ELog("Error: " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
            catch (Exception)
            {
            }

            Console.WriteLine("Error: " + ex.Message + Environment.NewLine + ex.StackTrace);
        }
    }

    /// <summary>
    /// Registers any special sevices
    /// </summary>
    private void RegisterServerServices()
    {
        FileFlows.Services.ServiceLoader.AddSpecialCase<IStartupService>(new StartupService());
        FileFlows.Services.ServiceLoader.AddSpecialCase<IPluginScanner>(new PluginScanner());
        FileFlows.Services.ServiceLoader.AddSpecialCase<ITemplateService>(new TemplateLoader());
        FileFlows.Services.ServiceLoader.AddSpecialCase<ISystemEventsService>(new SystemEvents());
    }

    /// <summary>
    /// Writes the start log header to indicate FileFlows has started
    /// </summary>
    /// <param name="args">the command line arguments</param>
    private void WriteLogHeader(string[] args)
    {
        if (Globals.IsDocker && File.Exists("/app/startup.log"))
        {
            Logger.Instance.ILog(new string('=', 100));
            string startup = File.ReadAllText("/app/startup.log");
            Logger.Instance.ILog(" Startup.log" + Environment.NewLine + startup);
            try
            {
                File.Delete("/app/startup.log");
            }
            catch (Exception)
            {
            }
        }
        Logger.Instance.ILog(new string('=', 100));
        Logger.Instance.ILog("Starting FileFlows " + Globals.Version);
        if (Docker)
            Logger.Instance.ILog("Running inside docker container");
        if (string.IsNullOrWhiteSpace(Application.EntryPoint) == false)
            Logger.Instance.DLog("Entry Point: " + EntryPoint);
        if (string.IsNullOrWhiteSpace(Globals.CustomFileFlowsDotComUrl) == false)
            Logger.Instance.ILog("Custom FileFlows.com: " + Globals.CustomFileFlowsDotComUrl);
        Logger.Instance.DLog("Arguments: " + (args?.Any() == true ? string.Join(" ", args) : "No arguments"));
        foreach (DictionaryEntry var in Environment.GetEnvironmentVariables())
        {
            Logger.Instance.DLog($"ENV.{var.Key} = {var.Value}");
        }

        Logger.Instance.ILog(new string('=', 100));

        var hardwareInfo = ServiceLoader.Load<HardwareInfoService>().GetHardwareInfo();
        Logger.Instance.ILog("Hardware Info: " + Environment.NewLine + hardwareInfo);
    }


    /// <summary>
    /// Initialise the loggers
    /// </summary>
    private void InitializeLoggers()
    {
        new FileLogger(DirectoryHelper.LoggingDirectory, "FileFlows");
        new ConsoleLogger();
        ChartHelper.Logger = Logger.Instance;
    }
}