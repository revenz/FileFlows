using System.Text;
using FileFlows.Client;
using FileFlows.Client.Components;
using FileFlows.Client.Components.Dialogs;
using FileFlows.Client.Models;
using FileFlows.Client.Services.Frontend;
using FileFlows.Client.Shared;
using FileFlows.Plugin;
using FileFlows.Shared.Models.Configuration;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client.Pages.Config.Configuration.Settings;

public partial class DatabasePage : InputRegister
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
    /// Gets or sets the message service
    /// </summary>
    [Inject] MessageService Message { get; set; }
    
    /// <summary>
    /// Gets or sets the navigation manager used
    /// </summary>
    [Inject] private NavigationManager NavigationManager { get; set; }
    /// <summary>
    /// Gets or sets the Layout
    /// </summary>
    [CascadingParameter] public MainLayout Layout { get; set; }
    

    /// <summary>
    /// Gets or sets the frontend service
    /// </summary>
    [Inject] protected FrontendService feService { get; set; }
    
    /// <summary>
    /// Gets the profile
    /// </summary>
    protected Profile Profile { get; private set; }

    private bool IsSaving { get; set; }

    private string lblSaving, lblDbDescription, lblTest, lblRestart, lblTestingDatabase;

    private string OriginalDatabase, OriginalServer;
    private DatabaseType OriginalDbType;
    
    /// <summary>
    /// The help URL
    /// </summary>
    private const string HelpUrl = "https://fileflows.com/docs/webconsole/config/settings/database";

    private DatabaseModel Model { get; set; } = new ();
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

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        Layout.SetInfo(Translater.Instant("Pages.Settings.Labels.Database"), "fas fa-database", noPadding: true);
        Profile = feService.Profile.Profile;
        lblSaving = Translater.Instant("Labels.Saving");
        lblDbDescription = Translater.Instant("Pages.Settings.Fields.Database.Description");
        lblTest = Translater.Instant("Labels.Test");
        lblRestart = Translater.Instant("Pages.Settings.Labels.Restart");
        lblTestingDatabase = Translater.Instant("Pages.Settings.Messages.Database.TestingDatabase");
        
        Blocker.Show();
        try
        {
            await Refresh();
        }
        finally
        {
            Blocker.Hide();
        }
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
        
        var response = await HttpHelper.Get<DatabaseModel>("/api/configuration/database");
        if (response.Success)
        {
            this.Model = response.Data;
            this.OriginalServer = this.Model?.DbServer;
            this.OriginalDatabase = this.Model?.DbName;
            this.OriginalDbType = this.Model?.DbType ?? DatabaseType.Sqlite;
            if (this.Model is { DbPort: < 1 })
                this.Model.DbPort = 3306;
            
            if(Profile.LicensedFor(LicenseFlags.ExternalDatabase))
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

    /// <summary>
    /// Saves the settings
    /// </summary>
    private async Task Save()
    {
        this.Blocker.Show(lblSaving);
        this.IsSaving = true;
        try
        {
            bool valid = await this.Validate();
            if (valid == false)
                return;
            
            await HttpHelper.Put("/api/configuration/database", this.Model);
        }
        finally
        {
            this.IsSaving = false;
            this.Blocker.Hide();
        }
    }

    private async Task TestDbConnection()
    {
        var server = Model?.DbServer?.Trim();
        var name = Model?.DbName?.Trim();
        var user = Model?.DbUser?.Trim();
        var password = Model?.DbPassword?.Trim();
        int port = Model?.DbPort ?? 0;
        if (string.IsNullOrWhiteSpace(server))
        {
            feService.Notifications.ShowError(Translater.Instant("Pages.Settings.Messages.Database.NoServer"));
            return;
        }
        if (string.IsNullOrWhiteSpace(name))
        {
            feService.Notifications.ShowError(Translater.Instant("Pages.Settings.Messages.Database.NoName"));
            return;
        }
        if (string.IsNullOrWhiteSpace(user))
        {
            feService.Notifications.ShowError(Translater.Instant("Pages.Settings.Messages.Database.NoUser"));
            return;
        }
        if (string.IsNullOrWhiteSpace(password))
        {
            feService.Notifications.ShowError(Translater.Instant("Pages.Settings.Messages.Database.NoPassword"));
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
            feService.Notifications.ShowSuccess(Translater.Instant("Pages.Settings.Messages.Database.TestSuccess"));
        }
        catch (Exception ex)
        {
            feService.Notifications.ShowError(ex.Message);
        }
        finally
        {
            Blocker.Hide();
        }
    }

    async void Restart()
    {
        var confirmed = await Message.Confirm(
            Translater.Instant("Pages.Settings.Messages.Restart.Title"),
            Translater.Instant("Pages.Settings.Messages.Restart.Message")
        );
        if (confirmed == false)
            return;
        await Save();
        await HttpHelper.Post("/api/system/restart");
    }


    /// <summary>
    /// When the user changes the DB backup value
    /// </summary>
    /// <param name="enabled">if the switch is enabled</param>
    private async Task OnDbBackupChange(bool enabled)
    {
        if (firstRenderedAt < DateTime.UtcNow.AddSeconds(-1) && enabled)
        {
            if (await Message.Confirm("Labels.Confirm",
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
