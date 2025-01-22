using System.Text;
using FileFlows.Client.Models;
using FileFlows.Client.Components.Dialogs;
using Microsoft.AspNetCore.Components;
using FileFlows.Client.Components;
using Microsoft.JSInterop;
using FileFlows.Plugin;

namespace FileFlows.Client.Pages;

/// <summary>
/// Page for system settings
/// </summary>
public partial class Settings : InputRegister
{
    /// <summary>
    /// Gets or sets blocker instance
    /// </summary>
    [CascadingParameter] Blocker Blocker { get; set; }
    
    /// <summary>
    /// Gets or sets the javascript runtime used
    /// </summary>
    [Inject] IJSRuntime jsRuntime { get; set; }
    
    /// <summary>
    /// Gets or sets the navigation manager used
    /// </summary>
    [Inject] private NavigationManager NavigationManager { get; set; }
    

    /// <summary>
    /// Gets or sets the profile service
    /// </summary>
    [Inject] protected ProfileService ProfileService { get; set; }
    
    /// <summary>
    /// Gets the profile
    /// </summary>
    protected Profile Profile { get; private set; }

    private bool IsSaving { get; set; }

    private string lblSave, lblSaving, lblHelp, lblEmail, lblDatabase, lblGeneral, lblLogging, 
        lblDbDescription, lblFileServerDescription, lblTest, lblRestart, lblSecurity, mdSecurityDescription, 
        lblLicense, lblUpdates, lblCheckNow, lblTestingDatabase, lblFileServer, lblEmailDescription;

    private string OriginalDatabase, OriginalServer;
    private DatabaseType OriginalDbType;

    private SettingsUiModel Model { get; set; } = new ();

    /// <summary>
    /// The language options
    /// </summary>
    private List<IconListOption> LanguageOptions = new ();
    private string LicenseFlagsString = string.Empty;
    // indicates if the page has rendered or not
    private DateTime firstRenderedAt = DateTime.MaxValue;

    /// <summary>
    /// Required validator
    /// </summary>
    private readonly List<Validator> RequiredValidator = new()
    {
        new Required()
    };

    /// <summary>
    /// The database types
    /// </summary>
    private List<ListOption> DbTypes = [];

    /// <summary>
    /// the SMTP security types
    /// </summary>
    private readonly List<ListOption> SmtpSecurityTypes = new()
    {
        new() { Label = "None", Value = EmailSecurity.None },
        new() { Label = "Auto", Value = EmailSecurity.Auto },
        new() { Label = "TLS", Value = EmailSecurity.TLS },
        new() { Label = "SSL", Value = EmailSecurity.SSL },
    };

    /// <summary>
    /// The security options
    /// </summary>
    private List<ListOption> SecurityOptions;
    
    /// <summary>
    /// Gets or sets the type of database to use
    /// </summary>
    private object DbType
    {
        get => Model.DbType;
        set
        {
            if (value is DatabaseType dbType)
            {
                Model.DbType = dbType;
                if (dbType != DatabaseType.Sqlite && string.IsNullOrWhiteSpace(Model.DbName))
                    Model.DbName = "FileFlows";
            }
        }
    }
    
    /// <summary>
    /// Gets or sets the type of email security to use
    /// </summary>
    private object SmtpSecurity
    {
        get => Model.SmtpSecurity;
        set
        {
            if (value is EmailSecurity security)
                Model.SmtpSecurity = security;
        }
    }

    /// <summary>
    /// Gets or sets the language
    /// </summary>
    private object Language
    {
        get => Model.Language;
        set
        {
            if (value is string lang)
            {
                Model.Language = lang;
            }
        }
    }
    
    
    /// <summary>
    /// Gets or sets the security
    /// </summary>
    private object Security
    {
        get => Model.Security;
        set
        {
            if (value is SecurityMode mode)
            {
                Model.Security = mode;
            }
        }
    }

    private string initialLannguage;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        Profile = await ProfileService.Get();
        lblSave = Translater.Instant("Labels.Save");
        lblSaving = Translater.Instant("Labels.Saving");
        lblHelp = Translater.Instant("Labels.Help");
        lblGeneral = Translater.Instant("Pages.Settings.Labels.General");
        lblLicense = Translater.Instant("Labels.License");
        lblEmail = Translater.Instant("Pages.Settings.Labels.Email");
        lblEmailDescription = Translater.Instant("Pages.Settings.Labels.EmailDescription");
        lblUpdates = Translater.Instant("Pages.Settings.Labels.Updates");
        lblSecurity = Translater.Instant("Pages.Settings.Fields.Security.Title");
        lblDatabase = Translater.Instant("Pages.Settings.Labels.Database");
        lblDbDescription = Translater.Instant("Pages.Settings.Fields.Database.Description");
        lblTest = Translater.Instant("Labels.Test");
        lblRestart= Translater.Instant("Pages.Settings.Labels.Restart");
        lblLogging= Translater.Instant("Pages.Settings.Labels.Logging");
        lblCheckNow = Translater.Instant("Pages.Settings.Labels.CheckNow");
        lblTestingDatabase = Translater.Instant("Pages.Settings.Messages.Database.TestingDatabase");
        lblFileServer = Translater.Instant("Pages.Settings.Labels.FileServer");
        lblFileServerDescription = Translater.Instant("Pages.Settings.Fields.FileServer.Description");
        mdSecurityDescription = RenderMarkdown("Pages.Settings.Fields.Security.Description");
        
        LanguageOptions = Profile.LanguageOptions?.Select(x =>
            new IconListOption()
            {
                Label = x.Label,
                Value = x.Value,
                IconUrl = $"/icons/flags/{x.Value}.svg"
            }
        ).ToList();
        
        InitSecurityModes();
        Blocker.Show("Loading Settings");
        try
        {
            await Refresh();
            initialLannguage = Model.Language;
        }
        finally
        {
            Blocker.Hide();
        }
    }

    /// <summary>
    /// Initiate the security modes avaiable
    /// </summary>
    private void InitSecurityModes()
    {
        SecurityOptions = new();
        if (Profile.LicensedFor(LicenseFlags.UserSecurity) == false)
        {
            Model.Security = SecurityMode.Off;
            return;
        }

        SecurityOptions = new()
        {
            new() { Label = $"Enums.{nameof(SecurityMode)}.{nameof(SecurityMode.Off)}", Value = SecurityMode.Off },
            new() { Label = $"Enums.{nameof(SecurityMode)}.{nameof(SecurityMode.Local)}", Value = SecurityMode.Local }
        };
        if (Profile.LicensedFor(LicenseFlags.SingleSignOn) == false)
        {
            if(Model.Security == SecurityMode.OpenIdConnect)
                Model.Security = SecurityMode.Off;
            return;
        }

        SecurityOptions.Add(
            new()
            {
                Label = $"Enums.{nameof(SecurityMode)}.{nameof(SecurityMode.OpenIdConnect)}",
                Value = SecurityMode.OpenIdConnect
            });
    }

    /// <summary>
    /// Renders markdown to HTML
    /// </summary>
    /// <param name="text">the text to render</param>
    /// <returns>the HTML</returns>
    private string RenderMarkdown(string text)
    {
        text = Translater.TranslateIfNeeded(text);
        List<string> lines = new();
        foreach (var t in text.Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(t))
            {
                lines.Add(string.Empty);
                continue;
            }

            string html = Markdig.Markdown.ToHtml(t).Trim();
            if (html.StartsWith("<p>") && html.EndsWith("</p>"))
                html = html[3..^4].Trim();
            html.Replace("<a ", "<a onclick=\"ff.openLink(event);return false;\" ");
            lines.Add(html);
        }

        return string.Join("\n", lines);

    }


    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
            firstRenderedAt = DateTime.UtcNow;
        base.OnAfterRender(firstRender);
    }

    /// <summary>
    /// Loads the settings
    /// </summary>
    /// <param name="blocker">if the blocker should be shown or not</param>
    private async Task Refresh(bool blocker = true)
    {
        if(blocker)
            Blocker.Show();
        
        var response = await HttpHelper.Get<SettingsUiModel>("/api/settings/ui-settings");
        if (response.Success)
        {
            this.Model = response.Data;
            LicenseFlagsString = LicenseFlagsToString(Model.LicenseFlags);
            this.OriginalServer = this.Model?.DbServer;
            this.OriginalDatabase = this.Model?.DbName;
            this.OriginalDbType = this.Model?.DbType ?? DatabaseType.Sqlite;
            if (this.Model is { DbPort: < 1 })
                this.Model.DbPort = 3306;

            if (Model is { SmtpPort: < 1})
                Model.SmtpPort = 25;

            if (Model != null && string.IsNullOrWhiteSpace(Model.AccessToken))
                Model.AccessToken = Guid.NewGuid().ToString("N");
            
            if(LicensedFor(LicenseFlags.ExternalDatabase))
            {
                DbTypes =
                [
                    new() { Label = "SQLite", Value = DatabaseType.Sqlite },
                    //new() { Label = "SQLite (Pooled Connection)", Value = DatabaseType.SqlitePooledConnection },
                    new() { Label = "MySQL", Value = DatabaseType.MySql },
                    new() { Label = "Postgres", Value = DatabaseType.Postgres },
                    new() { Label = "SQL Server", Value = DatabaseType.SqlServer }
                ];
            }
            else
            {
                DbTypes =
                [
                    new() { Label = "SQLite", Value = DatabaseType.Sqlite },
                    //new() { Label = "SQLite (Pooled Connection)", Value = DatabaseType.SqlitePooledConnection }
                ];
            }
        }

        this.StateHasChanged();
        
        if(blocker)
            Blocker.Hide();
    }

    private async Task Save()
    {
        this.Blocker.Show(lblSaving);
        this.IsSaving = true;
        try
        {
            bool valid = await this.Validate();
            if (valid == false)
                return;
            
            await HttpHelper.Put<string>("/api/settings/ui-settings", this.Model);
            if (Model.Security == SecurityMode.Off)
                await ProfileService.ClearAccessToken();

            await ProfileService.Refresh();
            InitSecurityModes();

            if (initialLannguage != Model.Language)
            {
                // need to do a full page reload
                NavigationManager.NavigateTo(NavigationManager.Uri, forceLoad: true);
            }
            else
            {
                await this.Refresh();
            }
        }
        finally
        {
            this.IsSaving = false;
            this.Blocker.Hide();
        }
    }

    /// <summary>
    /// Opens the help page
    /// </summary>
    private void OpenHelp()
        => _ = App.Instance.OpenHelp("https://fileflows.com/docs/webconsole/admin/settings");

    private async Task TestDbConnection()
    {
        var server = Model?.DbServer?.Trim();
        var name = Model?.DbName?.Trim();
        var user = Model?.DbUser?.Trim();
        var password = Model?.DbPassword?.Trim();
        int port = Model?.DbPort ?? 0;
        if (string.IsNullOrWhiteSpace(server))
        {
            Toast.ShowError(Translater.Instant("Pages.Settings.Messages.Database.NoServer"));
            return;
        }
        if (string.IsNullOrWhiteSpace(name))
        {
            Toast.ShowError(Translater.Instant("Pages.Settings.Messages.Database.NoName"));
            return;
        }
        if (string.IsNullOrWhiteSpace(user))
        {
            Toast.ShowError(Translater.Instant("Pages.Settings.Messages.Database.NoUser"));
            return;
        }
        if (string.IsNullOrWhiteSpace(password))
        {
            Toast.ShowError(Translater.Instant("Pages.Settings.Messages.Database.NoPassword"));
            return;
        }

        Blocker.Show(lblTestingDatabase);
        try
        {
            var result = await HttpHelper.Post("/api/settings/test-db-connection", new
            {
                server, name, port, user, password, Type = DbType
            });
            if (result.Success == false)
                throw new Exception(result.Body);
            Toast.ShowSuccess(Translater.Instant("Pages.Settings.Messages.Database.TestSuccess"));
        }
        catch (Exception ex)
        {
            Toast.ShowError(ex.Message);
        }
        finally
        {
            Blocker.Hide();
        }
    }

    async void Restart()
    {
        var confirmed = await Confirm.Show(
            Translater.Instant("Pages.Settings.Messages.Restart.Title"),
            Translater.Instant("Pages.Settings.Messages.Restart.Message")
        );
        if (confirmed == false)
            return;
        await Save();
        await HttpHelper.Post("/api/system/restart");
    }

    private bool IsLicensed => string.IsNullOrEmpty(Model?.LicenseStatus) == false && Model.LicenseStatus != "Unlicensed" && Model.LicenseStatus != "Invalid";

    /// <summary>
    /// Checks if the user is licensed for a feature
    /// </summary>
    /// <param name="feature">the feature to check</param>
    /// <returns>If the user is licensed for a feature</returns>
    private bool LicensedFor(LicenseFlags feature)
    {
        if (IsLicensed == false)
            return false;
        return (Model.LicenseFlags & feature) == feature;
    }

    /// <summary>
    /// Check for an update
    /// </summary>
    private async Task CheckForUpdateNow()
    {
        Blocker.Show();
        var available = await HttpHelper.Post<bool>("/api/settings/check-for-update-now");
        Blocker.Hide();
        if (available.Success == false)
        {
            Toast.ShowError("Pages.Settings.Messages.Update.Failed");
            return;
        }

        if (available.Data == false)
        {
            Toast.ShowInfo("Pages.Settings.Messages.Update.NotAvailable");
            return;
        }

        if (await Confirm.Show("Pages.Settings.Messages.Update.Title",
                "Pages.Settings.Messages.Update.Message") == false)
            return;
        await HttpHelper.Post("/api/settings/upgrade-now");
        Toast.ShowInfo("Pages.Settings.Messages.Update.Downloading");

    }

    /// <summary>
    /// Enumerates through the specified enum flags and returns a comma-separated string
    /// containing the names of the enum values that are present in the given flags.
    /// </summary>
    /// <param name="myValue">The enum value with flags set.</param>
    /// <returns>A comma-separated string of the enum values present in the given flags.</returns>
    string LicenseFlagsToString(LicenseFlags myValue)
    {
        List<string> components = new();

        foreach (LicenseFlags enumValue in Enum.GetValues(typeof(LicenseFlags)))
        {
            if (enumValue == LicenseFlags.NotLicensed)
                continue;
            if (myValue.HasFlag(enumValue))
            {
                components.Add(SplitWordsOnCapitalLetters(enumValue.ToString()));
            }
        }

        return string.Join("\n", components.OrderBy(x => x.ToLowerInvariant()));
    }
    
    /// <summary>
    /// Splits a given input string into separate words whenever a capital letter is encountered.
    /// </summary>
    /// <param name="input">The input string to be split.</param>
    /// <returns>A new string with spaces inserted before each capital letter (except the first one).</returns>
    string SplitWordsOnCapitalLetters(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        StringBuilder sb = new StringBuilder();
        foreach (char c in input)
        {
            if (char.IsUpper(c) && sb.Length > 0)
                sb.Append(' ');
            sb.Append(c);
        }

        return sb.ToString();
    }
    
    
    /// <summary>
    /// When the user changes the telemetry value
    /// </summary>
    /// <param name="disabled">if the switch is disabled</param>
    private async Task OnTelemetryChange(bool disabled)
    {
        if (firstRenderedAt < DateTime.UtcNow.AddSeconds(-1) && disabled)
        {
            if (await Confirm.Show("Pages.Settings.Messages.DisableTelemetryConfirm.Title",
                    "Pages.Settings.Messages.DisableTelemetryConfirm.Message",
                    false) == false)
            {
                Model.DisableTelemetry = false;
            }
        }
    }

    /// <summary>
    /// When the user changes the DB backup value
    /// </summary>
    /// <param name="enabled">if the switch is enabled</param>
    private async Task OnDbBackupChange(bool enabled)
    {
        if (firstRenderedAt < DateTime.UtcNow.AddSeconds(-1) && enabled)
        {
            if (await Confirm.Show("Labels.Confirm",
                    "Pages.Settings.Messages.Database.DontBackupOnUpgrade",
                    false) == false)
            {
                Model.DontBackupOnUpgrade = false;
            }
        }
    }
    
    private string GetDatabasePortHelp()
    {
        switch (Model.DbType)
        {
            case DatabaseType.Postgres:
                return Translater.Instant("Pages.Settings.Fields.Database.Port-Help-Postgres");
            case DatabaseType.MySql:
                return Translater.Instant("Pages.Settings.Fields.Database.Port-Help-MySql");
            case DatabaseType.SqlServer:
                return Translater.Instant("Pages.Settings.Fields.Database.Port-Help-SQLServer");
        }
        return string.Empty;
    }
}
