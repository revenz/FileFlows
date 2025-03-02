using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Avalonia;
using FileFlows.Node.Helpers;
using FileFlows.Node.Ui;
using FileFlows.Node.Utils;
using FileFlows.RemoteServices;
using FileFlows.ServerShared;
using FileFlows.ServerShared.Helpers;
using FileFlows.ServerShared.Models;
using FileFlows.ServerShared.Services;
using FileFlows.Shared.Helpers;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace FileFlows.Node;
public class Program
{
    /// <summary>
    /// Gets an instance of a node manager
    /// </summary>
    internal static NodeManager? Manager { get; private set; }
    
    /// <summary>
    /// Gets or sets an optional entry point that launched this
    /// </summary>
    public static string? EntryPoint { get; private set; }

    private static Mutex? appMutex;
    const string appName = "FileFlowsNode";
    public static void Main(string[] args)
    {
        args ??= new string[] { };
        #if(DEBUG)
        //args = new[] { "--no-gui" };
        #endif
        if (args.Any(x => x.ToLower() == "--help" || x.ToLower() == "-?" || x.ToLower() == "/?" || x.ToLower() == "/help" || x.ToLower() == "-help"))
        {
            CommandLineOptions.PrintHelp();
            return;
        }
        HttpHelper.Client = HttpHelper.GetDefaultHttpClient(RemoteService.ServiceBaseUrl);
        ServicePointManager.DefaultConnectionLimit = 50;

        var options = CommandLineOptions.Parse(args);
        if (Globals.IsLinux && options.InstallService)
        {
            if(options.Uninstall)
                SystemdService.Uninstall(true, root: options.Root);
            else
                SystemdService.Install(DirectoryHelper.BaseDirectory, true, root: options.Root);
            return;
        }

        Program.EntryPoint = options.EntryPoint;
        Globals.IsDocker = options.Docker;
        Globals.IsNode = true;
        Globals.IsSystemd = options.IsSystemd;
        // if (options.ApiPort > 0 && options.ApiPort < 65535)
        //     Workers.RestApiWorker.Port = options.ApiPort;
        
        SharedServiceLoader.Loader = type =>
        {
            var method = typeof(ServiceLoader).GetMethod("Load", new Type[] { });
            var genericMethod = method?.MakeGenericMethod(type);
            return genericMethod?.Invoke(null, null)!;
        };

        DirectoryHelper.Init();
        
        Console.WriteLine("BaseDirectory: " + DirectoryHelper.BaseDirectory);
        
        if(string.IsNullOrWhiteSpace(options.EntryPoint) == false && OperatingSystem.IsMacOS())
            File.WriteAllText(Path.Combine(DirectoryHelper.BaseDirectory, "version.txt"), Globals.Version.Split('.').Last());
        
        appMutex = new Mutex(true, appName, out bool createdNew);
        if (createdNew == false)
        {
            // app is already running
            if (options.Gui == false)
            {
                Console.WriteLine("An instance of FileFlows Node is already running");
            }
            else
            {
                GuiHelper.ShowMessage("FileFlows", "FileFlows Node is already running.");
            }
            
            return;
        }
        
        try
        {
            LoadEnvironmentalVaraibles();

            var appSettings = AppSettings.Load();
            RemoteService.ServiceBaseUrl = appSettings.ServerUrl;
            #if(DEBUG)
            if (string.IsNullOrEmpty(RemoteService.ServiceBaseUrl))
                RemoteService.ServiceBaseUrl = "http://localhost:6868/";
            #endif

            RemoteService.AccessToken = appSettings.AccessToken;
            
            if (string.IsNullOrEmpty(options.Server) == false)
                AppSettings.ForcedServerUrl = options.Server;
            if (string.IsNullOrEmpty(options.Temp) == false)
                AppSettings.ForcedTempPath = options.Temp;
            if (string.IsNullOrEmpty(options.Name) == false)
                AppSettings.ForcedHostName = options.Name;
            
            if(File.Exists(DirectoryHelper.NodeConfigFile) == false)
                AppSettings.Instance.Save();

            _ = new ConsoleLogger();
            _ = new FileLogger(DirectoryHelper.LoggingDirectory, "FileFlows-Node");
            _ = new ServerLogger();
            
            Logger.Instance?.ILog("FileFlows Node version: " + Globals.Version);
            if (Globals.IsDocker)
                Logger.Instance?.ILog("Running in a docker container");
            else if (Globals.IsSystemd)
                Logger.Instance?.ILog("Running as a systemd service");

            AppSettings.Init();

            bool showUi = options.Docker == false && options.Gui;

            Manager = new ();
            
            if(File.Exists(Path.Combine(DirectoryHelper.BaseDirectory, "node-upgrade.bat")))
                File.Delete(Path.Combine(DirectoryHelper.BaseDirectory, "node-upgrade.bat"));
            if(File.Exists(Path.Combine(DirectoryHelper.BaseDirectory, "node-upgrade.sh")))
                File.Delete(Path.Combine(DirectoryHelper.BaseDirectory, "node-upgrade.sh"));

            CleanOldConfigurations();
            
            #if(DEBUG)
            showUi = true;
            #endif

            if (showUi)
            {
                if(Globals.IsWindows)
                    WindowsConsoleManager.Hide();
                
                Logger.Instance?.ILog("Launching GUI");
                // Task.Run(async () =>
                // {
                    //await Manager.Register();
                    Manager.Start();
                // });
                try
                {
                    var appBuilder = BuildAvaloniaApp();
                    appBuilder.StartWithClassicDesktopLifetime(args);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message + Environment.NewLine +ex.StackTrace);
                }

            }
            else
            {
                if (AppSettings.IsConfigured() == false)
                {
                    Shared.Logger.Instance?.ELog("Configuration not set");
                    return;
                }

                // Shared.Logger.Instance?.ILog("Registering FileFlow Node");
                //
                // var registerResult = Manager.Register().Result;
                // if (registerResult.Success == false)
                // {
                //     Logger.Instance?.WLog("Register failed: " + registerResult.Message);
                //     return;
                // }


                Shared.Logger.Instance?.ILog("FileFlows node starting");
                
                Manager.Start();

                Thread.Sleep(-1);

                Shared.Logger.Instance?.ILog("Stopping workers");

                Manager.Stop();

                Shared.Logger.Instance?.ILog("Exiting");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message + Environment.NewLine + ex.StackTrace);
        }
    }

    private static void LoadEnvironmentalVaraibles()
    {
        AppSettings.ForcedServerUrl = Environment.GetEnvironmentVariable("ServerUrl");
        AppSettings.ForcedTempPath = Environment.GetEnvironmentVariable("TempPath");
        AppSettings.ForcedHostName = Environment.GetEnvironmentVariable("NodeName");
        AppSettings.ForcedAccessToken = Environment.GetEnvironmentVariable("AccessToken");

        var mappings = Environment.GetEnvironmentVariable("NodeMappings");
        if (string.IsNullOrWhiteSpace(mappings) == false)
        {
            try
            {
                var mappingsArray = JsonSerializer.Deserialize<List<RegisterModelMapping>>(mappings);
                if (mappingsArray?.Any() == true)
                    AppSettings.EnvironmentalMappings = mappingsArray;
            }
            catch (Exception)
            {
            }
        }

        if (int.TryParse(Environment.GetEnvironmentVariable("NodeRunnerCount") ?? string.Empty, out int runnerCount))
        {
            AppSettings.EnvironmentalRunnerCount = runnerCount;
        }
        if (bool.TryParse(Environment.GetEnvironmentVariable("NodeEnabled") ?? string.Empty, out bool enabled))
        {
            AppSettings.EnvironmentalEnabled = enabled;
        }
    }


    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        =>  AppBuilder.Configure<App>().UsePlatformDetect();

    /// <summary>
    /// Quits the node application
    /// </summary>
    /// <param name="exitCode">the exit code</param>
    internal static void Quit(int exitCode = 0)
    {
        // MainWindow.Instance?.ForceQuit();
        Environment.Exit(exitCode);
    }
    
    /// <summary>
    /// Deletes the old configurations, keeping the newest one
    /// </summary>
    static void CleanOldConfigurations()
    {
        var configDir = new DirectoryInfo(DirectoryHelper.ConfigDirectory);
        if (!configDir.Exists)
            return;

        var subdirs = configDir.GetDirectories()
            .Select(d => new { Dir = d, Number = ParseDirectoryNumber(d.Name) })
            .Where(d => d.Number.HasValue) // Only keep directories with valid numeric names
            .OrderByDescending(d => d.Number) // Sort by number (highest first)
            .ToList();

        if (subdirs.Count <= 1) // If there's only one or none, do nothing
            return;

        Logger.Instance.ILog("Deleting old configurations, keeping: " + subdirs.First().Dir.Name);

        foreach (var dir in subdirs.Skip(1)) // Skip the highest-numbered one
        {
            Logger.Instance.ILog("Deleting configuration: " + dir.Dir.Name);
            dir.Dir.Delete(recursive: true);
        }
    }

    /// <summary>
    /// Parses a directory name into an integer if possible.
    /// </summary>
    private static int? ParseDirectoryNumber(string name)
    {
        return int.TryParse(name, out int num) ? num : (int?)null;
    }

    
}
