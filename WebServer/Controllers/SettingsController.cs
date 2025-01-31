using FileFlows.ServerModels;
using FileFlows.ServerShared.Models;
using Microsoft.AspNetCore.Authorization;
using NodeService = FileFlows.Services.NodeService;
using ServiceLoader = FileFlows.Services.ServiceLoader;
using SettingsService = FileFlows.Services.SettingsService;

namespace FileFlows.WebServer.Controllers;
/// <summary>
/// Settings Controller
/// </summary>
[Route("/api/settings")]
[FileFlowsAuthorize(UserRole.Admin)]
public class SettingsController : BaseController
{
    /// <summary>
    /// Dummy password to use in place of passwords
    /// </summary>
    private const string DUMMY_PASSWORD = "************";
    
    /// <summary>
    /// The settings for the application
    /// </summary>
    private AppSettings Settings;
    /// <summary>
    /// The settings for the application
    /// </summary>
    private AppSettingsService SettingsService;
    
    /// <summary>
    /// Initializes a new instance of the controller
    /// </summary>
    /// <param name="appSettingsService">the application settings service</param>
    public SettingsController(AppSettingsService appSettingsService)
    {
        SettingsService = appSettingsService;
        Settings = appSettingsService.Settings;
    }

    /// <summary>
    /// Checks latest version from fileflows.com
    /// </summary>
    /// <returns>The latest version number if greater than current</returns>
    [HttpGet("check-update-available")]
    [AllowAnonymous]
    public async Task<string> CheckLatestVersion()
    {
        var settings = await Get();
        if (settings.DisableTelemetry)
            return string.Empty; 
        try
        {
            var onlineService = ServiceLoader.Load<IOnlineUpdateService>();
            var result = onlineService.GetLatestOnlineVersion();
            if (result.updateAvailable == false)
                return string.Empty;
            return result.onlineVersion.ToString();
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog("Failed checking latest version: " + ex.Message + Environment.NewLine + ex.StackTrace);
            return string.Empty;
        }
    }

    /// <summary>
    /// Get the system settings
    /// </summary>
    /// <returns>The system settings</returns>
    [HttpGet("ui-settings")]
    public async Task<SettingsUiModel> GetUiModel()
    {
        var settings = await Get();
        var licenseService = ServiceLoader.Load<LicenseService>();
        var license = licenseService.GetLicense();
        if ((license == null || license.Status == LicenseStatus.Unlicensed) &&
            string.IsNullOrWhiteSpace(Settings.LicenseKey) == false)
        {
            license = new();
            license.Status = LicenseStatus.Invalid;
        }

        license ??= new() { Status = LicenseStatus.Unlicensed };

        // clone it so we can remove some properties we dont want passed to the UI
        string json = JsonSerializer.Serialize(settings);
        var uiModel = JsonSerializer.Deserialize<SettingsUiModel>(json) ?? new ();
        if (string.IsNullOrWhiteSpace(settings.SmtpPassword) == false)
            uiModel.SmtpPassword = DUMMY_PASSWORD;
        
        SetLicenseFields(uiModel, license);
        uiModel.FileServerAllowedPathsString = uiModel.FileServerAllowedPaths?.Any() == true
            ? string.Join("\n", uiModel.FileServerAllowedPaths)
            : string.Empty;
        uiModel.DbType = Settings.DatabaseMigrateType ?? Settings.DatabaseType;
        if (uiModel.DbType != DatabaseType.Sqlite)
            PopulateDbSettings(uiModel,
                Settings.DatabaseMigrateConnection?.EmptyAsNull() ?? Settings.DatabaseConnection);
        uiModel.RecreateDatabase = Settings.RecreateDatabase;
        uiModel.DockerModsOnServer = Settings.DockerModsOnServer;
        if(uiModel.DbType != DatabaseType.Sqlite)
            uiModel.DontBackupOnUpgrade = Settings.DontBackupOnUpgrade;

        uiModel.Language = settings.Language?.EmptyAsNull() ?? "en";

        uiModel.Security = ServiceLoader.Load<AppSettingsService>().Settings.Security;
        if (uiModel.TokenExpiryMinutes < 1)
            uiModel.TokenExpiryMinutes = 24 * 60;
        if (uiModel.LoginLockoutMinutes < 1)
            uiModel.LoginLockoutMinutes = 20;
        if (uiModel.LoginMaxAttempts < 1)
            uiModel.LoginMaxAttempts = 10;
        uiModel.OidcCallbackAddressPlaceholder = Url.Action(nameof(HomeController.Index), "Home", null, Request.Scheme) ?? string.Empty;
        
        return uiModel;
    }


    /// <summary>
    /// Get the system settings
    /// </summary>
    /// <returns>The system settings</returns>
    [HttpGet]
    public async Task<Settings> Get()
    {
        var settings = await ServiceLoader.Load<ISettingsService>().Get() ?? new ();
        if (string.IsNullOrWhiteSpace(settings.SmtpPassword) == false)
        {
            string json = JsonSerializer.Serialize(settings);
            settings = JsonSerializer.Deserialize<Settings>(json) ?? new ();
            settings.SmtpPassword = DUMMY_PASSWORD;
        }
        return settings;
    }

    private void SetLicenseFields(SettingsUiModel settings, License license)
    {
        settings.LicenseKey = Settings.LicenseKey;
        settings.LicenseEmail  = Settings.LicenseEmail;
        settings.LicenseFiles = license == null ? string.Empty :
            license.Files >= 1_000_000_000 ? "Unlimited" : license.Files.ToString();
        settings.LicenseFlags = license?.Flags ?? 0;
        settings.LicenseLevel = license?.Level ?? 0;
        var licenseService = ServiceLoader.Load<LicenseService>();
        settings.LicenseProcessingNodes = licenseService.GetLicensedProcessingNodes();
        settings.LicenseExpiryDate = license == null ? DateTime.MinValue : license.ExpirationDateUtc.ToLocalTime();
        settings.LicenseStatus = (license == null ? LicenseStatus.Unlicensed : license.Status).ToString();
    }

    /// <summary>
    /// Save the system settings
    /// </summary>
    /// <param name="model">the system settings to save</param>
    /// <returns>The saved system settings</returns>
    [HttpPut("ui-settings")]
    public async Task SaveUiModel([FromBody] SettingsUiModel model)
    {
        if (model == null)
            return;

        var existing = await ServiceLoader.Load<ISettingsService>().Get() ?? new ();
        if (model.SmtpPassword == DUMMY_PASSWORD)
        {
            // need to get the existing password
            model.SmtpPassword = existing.SmtpPassword;
        }

        bool langChanged = existing.Language != model.Language;
        
        // validate license it
        Settings.LicenseKey = model.LicenseKey?.Trim() ?? string.Empty;
        Settings.LicenseEmail = model.LicenseEmail?.Trim() ?? string.Empty;
        var licenseService = ServiceLoader.Load<LicenseService>();
        await licenseService.Update();

        await Save(new ()
        {
            EulaAccepted = model.EulaAccepted,
            InitialConfigDone = model.InitialConfigDone,
            
            DelayBetweenNextFile = model.DelayBetweenNextFile,
            PausedUntil = model.PausedUntil,
            Language = model.Language,
            LogFileRetention = model.LogFileRetention,
            LogEveryRequest = model.LogEveryRequest,
            AutoUpdate = model.AutoUpdate,
            DisableTelemetry = model.DisableTelemetry,
            AutoUpdateNodes = model.AutoUpdateNodes,
            AutoUpdatePlugins = model.AutoUpdatePlugins,
            LogQueueMessages = model.LogQueueMessages,
            KeepFailedFlowTempFiles = model.KeepFailedFlowTempFiles,
            DontUseTempFilesWhenMovingOrCopying = model.DontUseTempFilesWhenMovingOrCopying,
            ShowFileAddedNotifications = model.ShowFileAddedNotifications,
            HideProcessingStartedNotifications = model.HideProcessingStartedNotifications,
            HideProcessingFinishedNotifications = model.HideProcessingFinishedNotifications,
            ProcessFileCheckInterval = model.ProcessFileCheckInterval,
            FileServerDisabled = model.FileServerDisabled,
            FileServerOwnerGroup = model.FileServerOwnerGroup,
            FileServerFilePermissions = model.FileServerFilePermissions,
            FileServerFolderPermissions = model.FileServerFolderPermissions,
            FileServerAllowedPaths = model.FileServerAllowedPathsString?.Split(["\r\n", "\r", "\n"], StringSplitOptions.RemoveEmptyEntries) ?? [],
            AccessToken = model.AccessToken ?? string.Empty,
            
            SmtpFrom = model.SmtpFrom ?? string.Empty,
            SmtpPassword = model.SmtpPassword ?? string.Empty,
            SmtpServer = model.SmtpServer ?? string.Empty,
            SmtpPort = model.SmtpPort,
            SmtpSecurity = model.SmtpSecurity,
            SmtpUser = model.SmtpUser ?? string.Empty,
            SmtpFromAddress = model.SmtpFromAddress ?? string.Empty,
                
            TokenExpiryMinutes = model.TokenExpiryMinutes,
            LoginLockoutMinutes = model.LoginLockoutMinutes < 1 ? 20 : model.LoginLockoutMinutes,
            LoginMaxAttempts = model.LoginMaxAttempts < 1 ? 10 : model.LoginMaxAttempts,
            OidcAuthority = model.OidcAuthority ?? string.Empty,
            OidcClientId = model.OidcClientId ?? string.Empty,
            OidcClientSecret = model.OidcClientSecret ?? string.Empty,
            OidcCallbackAddress = model.OidcCallbackAddress ?? string.Empty,
        }, await GetAuditDetails());
        ServerGlobals.AccessToken = model.AccessToken;
        
        Settings.Security = model.Security;
        
        var newConnectionString = GetConnectionString(model, model.DbType);
        if (IsConnectionSame(Settings.DatabaseConnection, newConnectionString) == false)
        {
             // need to migrate the database
             Settings.DatabaseMigrateConnection = newConnectionString;
             Settings.DatabaseMigrateType = model.DbType;
        }
        else if (Settings.DatabaseType != model.DbType)
        {
            // if switching from SQLite new connection 
            Settings.DatabaseType = model.DbType;
        }

        Settings.RecreateDatabase = model.RecreateDatabase;
        Settings.DockerModsOnServer = model.DockerModsOnServer;
        Settings.DontBackupOnUpgrade = model.DbType != DatabaseType.Sqlite && model.DbType != DatabaseType.SqlitePooledConnection && model.DontBackupOnUpgrade;
        // save AppSettings with updated license and db migration if set
        SettingsService.Save();

        if (langChanged)
        {
            ServiceLoader.Load<IPluginScanner>().Scan();
            await ServiceLoader.Load<LanguageService>().Initialize();

        }
    }
    
    /// <summary>
    /// Save the system settings
    /// </summary> 
    /// <param name="model">the system settings to save</param>
    /// <param name="auditDetails">the audit details</param>
    /// <returns>The saved system settings</returns>
    internal async Task Save(Settings model, AuditDetails? auditDetails)
    {
        if (model == null)
            return;
        var service = (SettingsService)ServiceLoader.Load<ISettingsService>();
        await service.Save(model, auditDetails);
    }
    
    /// <summary>
    /// Determines if the new connection string is the same as the original connection string.
    /// </summary>
    /// <param name="original">The original connection string.</param>
    /// <param name="newConnection">The new connection string to compare against the original.</param>
    /// <returns>
    /// <c>true</c> if both connection strings are for SQLite or if they are exactly the same;
    /// otherwise, <c>false</c>.
    /// </returns>
    private bool IsConnectionSame(string original, string newConnection)
    {
        if (IsSqliteConnection(original) && IsSqliteConnection(newConnection))
            return true;
        return original == newConnection;
    }

    /// <summary>
    /// Determines if the provided connection string is an SQLite connection.
    /// </summary>
    /// <param name="connString">The connection string to evaluate.</param>
    /// <returns>
    /// <c>true</c> if the connection string is for an SQLite database or if it is null or whitespace;
    /// otherwise, <c>false</c>.
    /// </returns>
    private bool IsSqliteConnection(string connString)
    {
        if (string.IsNullOrWhiteSpace(connString))
            return true;
        return connString.IndexOf("FileFlows.sqlite", StringComparison.Ordinal) > 0;
    }

    /// <summary>
    /// Retrieves the connection string based on the provided settings and database type.
    /// </summary>
    /// <param name="settings">The settings containing the connection details.</param>
    /// <param name="dbType">The type of the database.</param>
    /// <returns>The connection string for the specified database type.</returns>
    private string GetConnectionString(SettingsUiModel settings, DatabaseType dbType)
    {
        return new DbConnectionInfo()
        {
            Type = dbType,
            Server = settings.DbServer,
            Name = settings.DbName,
            Port = settings.DbPort,
            User = settings.DbUser,
            Password = settings.DbPassword
        }.ToString();
    }
    

    /// <summary>
    /// Tests a database connection
    /// </summary>
    /// <param name="model">The database connection info</param>
    /// <returns>OK if successful, otherwise a failure message</returns>
    [HttpPost("test-db-connection")]
    public IActionResult TestDbConnection([FromBody] DbConnectionInfo model)
    {
        if (model == null)
            throw new ArgumentException(nameof(model));

        if (model.Type == DatabaseType.Sqlite)
            return BadRequest("Unsupport database type");

        var dbService = ServiceLoader.Load<DatabaseService>();
        var result = dbService.TestConnection(model.Type, model.ToString());
        if(result.Failed(out string error))
            return BadRequest(error);
        return Ok();
    }

    /// <summary>
    /// Triggers a check for an update
    /// </summary>
    [HttpPost("check-for-update-now")]
    public async Task<bool> CheckForUpdateNow()
    {
        var service = ServiceLoader.Load<UpdateService>();
        await service.Trigger();
        
        if (LicenseService.IsLicensed(LicenseFlags.AutoUpdates) == false)
            return false;

        if (string.IsNullOrEmpty(service.Info.FileFlowsVersion))
            return false;
        return true;
    }

    /// <summary>
    /// Triggers a upgrade now
    /// </summary>
    [HttpPost("upgrade-now")]
    public async Task UpgradeNow()
    {
        if (LicenseService.IsLicensed(LicenseFlags.AutoUpdates) == false)
            return;

        _ = Task.Run(async () =>
        {
            await Task.Delay(1);
            var service = ServiceLoader.Load<IOnlineUpdateService>();
            return service.RunCheck(skipEnabledCheck: true);
        });
        await Task.CompletedTask;
    }
    
    /// <summary>
    /// Gets the current configuration revision
    /// </summary>
    /// <returns>the current revision</returns>
    [HttpGet("current-config/revision")]
    public Task<int> GetCurrentConfigRevision()
        => ServiceLoader.Load<ISettingsService>().GetCurrentConfigurationRevision();
    
    /// <summary>
    /// Loads the current configuration
    /// </summary>
    /// <returns>the current configuration</returns>
    [HttpGet("current-config")]
    public Task<ConfigurationRevision?> GetCurrentConfig()
        => ServiceLoader.Load<ISettingsService>().GetCurrentConfiguration();

    
    /// <summary>
    /// Parses the connection string and populates the provided DbSettings object with server, user, password, database name, and port information.
    /// </summary>
    /// <param name="settings">The setting object to populate.</param>
    /// <param name="connectionString">The connection string to parse.</param>
    void PopulateDbSettings(SettingsUiModel settings, string connectionString)
    {
        var parts = connectionString.Split(';');

        foreach (var part in parts)
        {
            var keyValue = part.Split('=');
            if (keyValue.Length != 2)
                continue;

            var key = keyValue[0].Trim().ToLowerInvariant();
            var value = keyValue[1].Trim();

            switch (key)
            {
                case "server":
                case "host":
                    settings.DbServer = value;
                    break;
                case "user":
                case "uid":
                case "username":
                case "user id":
                    settings.DbUser = value;
                    break;
                case "password":
                case "pwd":
                    settings.DbPassword = value;
                    break;
                case "database":
                case "initial catalog": // SQL Server specific
                    settings.DbName = value;
                    break;
                case "port":
                    if(int.TryParse(value, out int port))
                        settings.DbPort = port;
                    break;
            }
        }
    }

    /// <summary>
    /// Saves the initial configuration
    /// </summary>
    /// <param name="model">the model</param>
    /// <returns>the response</returns>
    [HttpPost("initial-config")]
    public async Task<IActionResult> SaveInitialConfiguration([FromBody] InitialConfigurationModel model)
    {
        if (model.EulaAccepted == false)
            return BadRequest("EULA not accepted");
        if (model.Plugins?.Any() == true)
        {
            var pluginService = ServiceLoader.Load<PluginService>();
            await pluginService.DownloadPlugins(model.Plugins);
        }

        var service = (SettingsService)ServiceLoader.Load<ISettingsService>();
        var settings = await service.Get();
        settings.EulaAccepted = model.EulaAccepted;
        settings.InitialConfigDone = true;
        settings.Language = model.Language?.EmptyAsNull() ?? "en";

        if (model.Runners > 0)
        {
            var nodeService = ServiceLoader.Load<NodeService>();
            var node = await nodeService.GetServerNodeAsync();
            if (node != null)
            {
                node.FlowRunners = model.Runners;
                await nodeService.Update(node, null);
            }
        }

        await service.Save(settings, await GetAuditDetails());

        if (model.DockerMods?.Any() == true)
        {
            var dmService = ServiceLoader.Load<DockerModService>();
            var repoService = ServiceLoader.Load<RepositoryService>();
            foreach (var dm in model.DockerMods)
            {
                var content = await repoService.GetContent(dm.Path!);
                if (content.IsFailed == false)
                    await dmService.ImportFromRepository(dm, content.Value, null);
            }
        }

        return Ok();
    }

    /// <summary>
    /// The initial configuration model
    /// </summary>
    public class InitialConfigurationModel
    {
        /// <summary>
        /// Gets or sets the plugins to download
        /// </summary>
        public List<PluginPackageInfo> Plugins { get; set; } = null!;
        /// <summary>
        /// Gets or sets if the EULA was accepted
        /// </summary>
        public bool EulaAccepted { get; set; }

        /// <summary>
        /// Gets or sets the DockerMods to install
        /// </summary>
        public List<RepositoryObject> DockerMods { get; set; } = null!;
        /// <summary>
        /// Gets or sets the language
        /// </summary>
        public string Language { get; set; } = null!;
        /// <summary>
        /// Gets or sets the flow runners
        /// </summary>
        public int Runners { get; set; }
    }
}
