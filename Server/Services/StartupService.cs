using FileFlows.Managers.InitializationManagers;
using FileFlows.Node.Workers;
using FileFlows.Server.Helpers;
using FileFlows.Server.Workers;
using FileFlows.ServerShared.Workers;
using FileFlows.Services;
using FileFlows.Services.FileDropServices;
using FileFlows.Shared.Models;
using FileFlows.WebServer;
using LibraryFileService = FileFlows.RemoteServices.LibraryFileService;

namespace FileFlows.Server.Services;

/// <summary>
/// Service used for status up work
/// All startup code, db initialization, migrations, upgrades should be done here,
/// so the UI apps can be shown with a status of what is happening
/// </summary>
public class StartupService : IStartupService
{
    
    /// <summary>
    /// An event that is called when there is status update
    /// </summary>
    public event StartupStatusEvent OnStatusUpdate;
    
    /// <summary>
    /// Gets the current status
    /// </summary>
    public string CurrentStatus { get; set; }


    private AppSettingsService appSettingsService;

    /// <summary>
    /// Run the startup commands
    /// </summary>
    public Result<bool> Run(string serverUrl)
    {
        UpdateStatus("Starting...");
        try
        {
            appSettingsService = ServiceLoader.Load<AppSettingsService>();
            
            FileFlows.Helpers.Decrypter.EncryptionKey = ServiceLoader.Load<AppSettingsService>().Settings.EncryptionKey;

            string error;

            CheckLicense();

            CleanDefaultTempDirectory();

            BackupSqlite();

            if (CanConnectToDatabase().Failed(out error))
            {
                error = "Database Connection Error: " + error;
                UpdateStatus(error);
                return Result<bool>.Fail(error);
            }
            
            if (DatabaseExists()) // only upgrade if it does exist
            {
                if (Upgrade().Failed(out error))
                {
                    error = "Database Upgrade Error: " + error;
                    UpdateStatus(error);
                    return Result<bool>.Fail(error);
                }
            }
            else if (CreateDatabase().Failed(out error))
            {
                error = "Create Database Error: " + error;
                UpdateStatus(error);
                return Result<bool>.Fail(error);
            }

            if (PrepareDatabase().Failed(out error))
            {
                error = "Prepare Database Error: " + error;
                UpdateStatus(error);
                return Result<bool>.Fail(error);
            }

            if (SetVersion().Failed(out error))
            {
                error = "Set Version Error: " + error;
                UpdateStatus(error);
                return Result<bool>.Fail(error);
            }

            if (CheckVersion().Failed(out error))
            {
                UpdateStatus(error);
                return Result<bool>.Fail(error);
            }
        
            // do this so the settings object is loaded
            var settings = ServiceLoader.Load<ISettingsService>().Get().Result;
            var appSettings = ServiceLoader.Load<AppSettingsService>().Settings;

            if (Globals.IsDocker && appSettings.DockerModsOnServer)
                RunnerDockerMods();

            ScanForPlugins();

            ServiceLoader.Load<LanguageService>().Initialize().Wait();
            ServiceLoader.Load<FileFlows.Services.LibraryFileService>().ResetProcessingStatus(CommonVariables.InternalNodeUid).Wait();

            DataLayerDelegates.Setup();
            
            Complete(settings, serverUrl);

            // Start workers right at the end, so the ServerUrl is set in case the worker needs BaseServerUrl
            StartupWorkers();

            StartFileDropServer();
            
            WebServerApp.FullyStarted = true;
            return true;
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog("Startup failure: " + ex.Message + Environment.NewLine + ex.StackTrace);
            #if(DEBUG)
            UpdateStatus("Startup failure: " + ex.Message + Environment.NewLine + ex.StackTrace);
            #else
            UpdateStatus("Startup failure: " + ex.Message);
            #endif
            return Result<bool>.Fail(ex.Message);
        }
    }

    private void StartFileDropServer()
    {
#if(!DEBUG)
        if (LicenseService.IsLicensed(LicenseFlags.FileDrop) == false)
            return;
#endif
        var service = new FileFlows.FileDropApp.FileDropWebService();
        ServiceLoader.AddSpecialCase<IFileDropWebServerService>(service);
        
        var settings = ServiceLoader.Load<FileDropSettingsService>().Get();
        if (settings.Enabled)
        {
            Logger.Instance?.ILog("Starting FileDrop App...");
            service.Start();
        }
    }

    /// <summary>
    /// Final startup code
    /// </summary>
    /// <param name="settings">the settings</param>
    /// <param name="serverUrl">the server URL</param>
    private void Complete(Settings settings, string serverUrl)
    {
        string protocol = serverUrl[..serverUrl.IndexOf(":", StringComparison.Ordinal)];

        Globals.ServerUrl = $"{protocol}://localhost:{WebServerApp.Port}";
        // update the client with the proper ServiceBaseUrl
        FileFlows.Helpers.HttpHelper.Client =
            FileFlows.Helpers.HttpHelper.GetDefaultHttpClient(Globals.ServerUrl);

        FileFlows.RemoteServices.RemoteService.ServiceBaseUrl = Globals.ServerUrl;
        FileFlows.RemoteServices.RemoteService.AccessToken = settings.AccessToken;
        FileFlows.RemoteServices.RemoteService.NodeUid = Application.RunningUid;

        WebServerApp.FullyStarted = true;
    }

    /// <summary>
    /// Scans for plugins
    /// </summary>
    private void ScanForPlugins()
    {
        UpdateStatus("Scanning for Plugins");
        var service = ServiceLoader.Load<IPluginScanner>();
        // need to scan for plugins before initing the translater as that depends on the plugins directory
        service.Scan();
    }

    /// <summary>
    /// Starts the workers
    /// </summary>
    private void StartupWorkers()
    {
        UpdateStatus("Starting Workers");
        WorkerManager.StartWorkers(
            new StartupWorker(),
            new LicenseValidatorWorker(),
            new SystemMonitor(),
            //new LibraryWorker(),
            new LogFileCleaner(),
            new FlowWorker(string.Empty, isServer: true),
            new ConfigCleaner(),
            new PluginUpdaterWorker(),
            new LibraryFileLogPruner(),
            new LogConverter(),
            new TelemetryReporter(),
            new ServerUpdater(),
            new TempFileCleaner(string.Empty),
            new FlowRunnerMonitor(),
            new ObjectReferenceUpdater(),
            new FileFlowsTasksWorker(),
            new RepositoryUpdaterWorker(),
            new ScheduledReportWorker(),
            new StatisticSyncer(),
            new UpdateWorker(),
            new FileDropWorker(),
            new DistributedCacheCleanerWorker()
            //new LibraryFileServiceUpdater()
        );

        // setup the library watches
        ServiceLoader.Load<LibraryService>().SetupWatches().Wait();
    }

    /// <summary>
    /// Looks for a DockerMods output file and if found, logs its contents
    /// </summary>
    private void RunnerDockerMods()
    {
        UpdateStatus("Running DockerMods");
        var mods = ServiceLoader.Load<DockerModService>().GetAll().Result.Where(x => x.Enabled).ToList();
        foreach (var mod in mods)
        {
            UpdateStatus("Running DockerMods", mod.Name);
            DockerModHelper.Execute(mod, outputCallback: (output) =>
            {
                UpdateStatus("Running DockerMods", mod.Name, output);
            }).Wait();
        }

        // var output = Path.Combine(DirectoryHelper.DockerModsDirectory, "output.log");
        // if (File.Exists(output) == false)
        //     return;
        // var content = File.ReadAllText(output);
        // Logger.Instance.ILog("DockerMods: \n" + content);
        // File.Delete(output);
    }

    /// <summary>
    /// Tests a connection to a database
    /// </summary>
    private Result<bool> CanConnectToDatabase()
        => MigrationManager.CanConnect(appSettingsService.Settings.DatabaseType,
            appSettingsService.Settings.DatabaseConnection);

    /// <summary>
    /// Checks if the database exists
    /// </summary>
    /// <returns>true if it exists, otherwise false</returns>
    private Result<bool> DatabaseExists()
        => MigrationManager.DatabaseExists(appSettingsService.Settings.DatabaseType,
            appSettingsService.Settings.DatabaseConnection);

    /// <summary>
    /// Creates the database
    /// </summary>
    /// <returns>true if it exists, otherwise false</returns>
    private Result<bool> CreateDatabase()
        => MigrationManager.CreateDatabase(Logger.Instance, appSettingsService.Settings.DatabaseType,
            appSettingsService.Settings.DatabaseConnection);

    /// <summary>
    /// Backups the database file if using SQLite and not migrating
    /// </summary>
    private void BackupSqlite()
    {
        if (appSettingsService.Settings.DatabaseType is DatabaseType.Sqlite or DatabaseType.SqlitePooledConnection == false)
            return;
        if (appSettingsService.Settings.DatabaseMigrateType != null)
            return;
        try
        {
            string dbfile = Path.Combine(DirectoryHelper.DatabaseDirectory, "FileFlows.sqlite");
            if (File.Exists(dbfile))
                File.Copy(dbfile, dbfile + ".backup", true);
        }
        catch (Exception ex)
        {
            Logger.Instance.WLog("Failed to backup SQLite database file: " + ex.Message);
        }
    }

    /// <summary>
    /// Sends a message update
    /// </summary>
    /// <param name="message">the message</param>
    /// <param name="subStatus">sub status</param>
    /// <param name="details">additional details</param>
    void UpdateStatus(string message, string subStatus = null, string details = null)
    {
        Logger.Instance.ILog(message);
        CurrentStatus = message;
        OnStatusUpdate?.Invoke(message, subStatus, details);
    }
    

    /// <summary>
    /// Checks the license key
    /// </summary>
    void CheckLicense()
    {
        var service = ServiceLoader.Load<LicenseService>();
        service.Update().Wait();
    }
    
    /// <summary>
    /// Clean the default temp directory on startup
    /// </summary>
    private void CleanDefaultTempDirectory()
    {
        UpdateStatus("Cleaning temporary directory");
        
        string tempDir = Globals.IsDocker
            ? Path.Combine(DirectoryHelper.DataDirectory, "temp") // legacy reasons docker uses lowercase temp
            : Path.Combine(DirectoryHelper.BaseDirectory, "Temp");
        DirectoryHelper.CleanDirectory(tempDir);
    }

    /// <summary>
    /// Runs an upgrade
    /// </summary>
    /// <returns>the upgrade result</returns>
    Result<bool> Upgrade()
    {
        string error;
        var upgrader = new Upgrade.Upgrader();
        var upgradeRequired = upgrader.UpgradeRequired(appSettingsService.Settings);
        if (upgradeRequired.Failed(out error))
            return Result<bool>.Fail(error);

        bool needsUpgrade = upgradeRequired.Value.Required;

        if (needsUpgrade)
        {
            UpdateStatus("Backing up old database...");
            upgrader.Backup(upgradeRequired.Value.Current, appSettingsService.Settings,
                (details) => { UpdateStatus("Backing up old database...", details); });

            UpdateStatus("Upgrading Please Wait...");
            var upgradeResult = upgrader.Run(upgradeRequired.Value.Current, appSettingsService,
                (details) => { UpdateStatus("Upgrading Please Wait...", details); });
            if (upgradeResult.Failed(out error))
                return Result<bool>.Fail(error);
        }
        
        
        upgrader.EnsureColumnsExist(appSettingsService);
        upgrader.EnsureDefaultsExist(appSettingsService);

        return true;
    }


    /// <summary>
    /// Prepares the database
    /// </summary>
    /// <returns>the result</returns>
    Result<bool>PrepareDatabase()
    {
        UpdateStatus("Initializing database...");

        string error;
        
        var service = ServiceLoader.Load<DatabaseService>();

        if (service.MigrateRequired())
        {
            UpdateStatus("Migrating database, please wait this may take a while.");
            if (service.MigrateDatabase().Failed(out error))
                return Result<bool>.Fail(error);
        }
        
        if (service.PrepareDatabase().Failed(out error))
            return Result<bool>.Fail(error);

        return true;
    }


    /// <summary>
    /// Sets the version in the database
    /// </summary>
    /// <returns>true if successful</returns>
    private Result<bool> SetVersion()
    {
        try
        {
            var service = ServiceLoader.Load<DatabaseService>();
            service.SetVersion().Wait();
            return true;
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(ex.Message);
        }
    }

    /// <summary>
    /// Gets the version
    /// </summary>
    /// <returns>true if successful</returns>
    private Result<bool> CheckVersion()
    {
        try
        {
            var versionParts = Globals.Version.Split(".");
            int year = int.Parse("20" + versionParts[0]);
            int month = int.Parse(versionParts[1]);
            var date = new DateTime(year, month, 1);

            if (date < DateTime.UtcNow.AddMonths(-4))
                return Result<bool>.Fail("Version is out of date, please upgrade to continue.");
            
            return true;
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(ex.Message);
        }
    }
}