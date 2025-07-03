using FileFlows.Client.Services.Frontend;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Widgets;

/// <summary>
/// Extension Updates widget
/// </summary>
public partial class ExtensionUpdatesWidget : ComponentBase, IDisposable
{
    /// <summary>
    /// Gets or sets the mode
    /// </summary>
    private int Mode { get; set; }
    
    private const int MODE_PLUGINS = 0;
    private const int MODE_SCRIPTS = 1;
    private const int MODE_DOCKERMODS = 2;
    
    /// <summary>
    /// Gets or sets the frontend service
    /// </summary>
    [Inject] public FrontendService feService { get; set; }

    private UpdateInfo _Data;

    /// <summary>
    /// Gets or sets the update data
    /// </summary>
    [Parameter]
    public UpdateInfo Data
    {
        get => _Data;
        set => _Data = value;
    }

    /// <summary>
    /// Gets or sets the users profile
    /// </summary>
    [Parameter] public Profile Profile { get; set; }
    /// <summary>
    /// Gets or sets the blocker
    /// </summary>
    [CascadingParameter] public Blocker Blocker { get; set; }
    
    /// <summary>
    /// Event fired when a package is updated
    /// </summary>
    [Parameter] public EventCallback OnUpdate { get; set; }

    /// <summary>
    /// Translations
    /// </summary>
    private string lblTitle, lblPlugins, lblScripts, lblDockerMods, lblUpdateAll, lblUpdate;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        lblTitle = Translater.Instant("MenuGroups.Extensions");
        lblUpdateAll = Translater.Instant("Labels.UpdateAll");
        lblUpdate = Translater.Instant("Labels.Update");
        DataRefreshed();
        feService.Dashboard.UpdateInfoUpdated += OnUpdatesUpdateInfo;
        _Data = feService.Dashboard.CurrentUpdatesInfo;
        CheckMode();
    }

    private void DataRefreshed()
    {
        lblPlugins = Translater.Instant("Pages.Dashboard.Widgets.Updates.Plugins", new { count = Data.PluginUpdates.Count });
        lblScripts = Translater.Instant("Pages.Dashboard.Widgets.Updates.Scripts", new { count = Data.ScriptUpdates.Count });
        lblDockerMods = Translater.Instant("Pages.Dashboard.Widgets.Updates.DockerMods", new { count = Data.DockerModUpdates.Count });
    }

    /// <summary>
    /// Called when the update info has been updated
    /// </summary>
    /// <param name="info">the new info</param>
    private void OnUpdatesUpdateInfo(UpdateInfo info)
    {
        _Data = info;
        DataRefreshed();
        CheckMode();
        StateHasChanged();
    }

    /// <summary>
    /// Check which mode to show
    /// </summary>
    private void CheckMode()
    {
        if (Mode == MODE_PLUGINS && Data.PluginUpdates.Count > 0)
            return;
        if (Mode == MODE_SCRIPTS && Data.ScriptUpdates.Count > 0)
            return;
        if (Mode == MODE_DOCKERMODS && Data.DockerModUpdates.Count > 0)
            return;
        
        if (Data.PluginUpdates.Count > 0)
            Mode = MODE_PLUGINS;
        else if (Data.ScriptUpdates.Count > 0)
            Mode = MODE_SCRIPTS;
        else if (Data.DockerModUpdates.Count > 0)
            Mode = MODE_DOCKERMODS;
        StateHasChanged();
    }
    
    /// <summary>
    /// Gets the selected list
    /// </summary>
    private List<PackageUpdate> SelectedList => Mode switch
    {
        MODE_PLUGINS => Data.PluginUpdates,
        MODE_SCRIPTS => Data.ScriptUpdates,
        MODE_DOCKERMODS => Data.DockerModUpdates,
        _ => new()
    };
    
    /// <summary>
    /// Gets the default icon
    /// </summary>
    private string DefaultIcon => Mode switch
    {
        MODE_PLUGINS => "fas fa-puzzle-piece",
        MODE_SCRIPTS => "fas fa-scroll",
        MODE_DOCKERMODS => "fab fa-docker",
        _ => string.Empty
    };

    /// <summary>
    /// Gets if the updates are viewable for this user
    /// </summary>
    /// <returns>this user</returns>
    private bool UpdatesViewable()
    {
        if (Profile.IsAdmin)
            return true;
        switch (Mode)
        {
            case MODE_PLUGINS:
                return Profile.HasRole(UserRole.Plugins);
            case MODE_SCRIPTS:
                return Profile.HasRole(UserRole.Scripts);
            case MODE_DOCKERMODS:
                return Profile.HasRole(UserRole.DockerMods);
        }

        return false;
    }

    /// <summary>
    /// Refreshes the updates
    /// </summary>
    private async Task Refresh()
    {
        await OnUpdate.InvokeAsync();
        CheckMode();
    }

    /// <summary>
    /// Updates all packages
    /// </summary>
    private async Task UpdateAll()
    {
        if(UpdatesViewable() == false)
            return;
        
        if (Mode == MODE_PLUGINS)
            await UpdatePlugins();
        else if (Mode == MODE_SCRIPTS)
            await UpdateScripts();
        else if (Mode == MODE_DOCKERMODS)
            await UpdateDockerMods();
    }

    /// <summary>
    /// Updates a single package
    /// </summary>
    private async Task Update(PackageUpdate package)
    {
        if (UpdatesViewable() == false)
            return;
        
        switch (Mode)
        {
            case MODE_PLUGINS:
                await UpdatePlugins(package.Uid);
                break;
            case MODE_SCRIPTS:
                await UpdateScripts(package.Uid);
                break;
            case MODE_DOCKERMODS:
                await UpdateDockerMods(package.Uid);
                break;
        }
    }

    /// <summary>
    /// Updates the plugins
    /// </summary>
    /// <param name="uid">the UID of the plugin to update</param>
    private async Task UpdatePlugins(Guid? uid = null)
    {
        Blocker.Show("Pages.Plugins.Messages.UpdatingPlugins");
        StateHasChanged();
        try
        {
            var uids = uid == null ? Data.PluginUpdates.Select(x => x.Uid).ToArray() : [uid.Value];
            await HttpHelper.Post($"/api/plugin/update", new ReferenceModel<Guid> { Uids = uids });
            await Refresh();
        }
        finally
        {
            Blocker.Hide();
            StateHasChanged();
        }
    }
    
    /// <summary>
    /// Updates the scripts
    /// </summary>
    /// <param name="uid">the UID of the script to update</param>
    private async Task UpdateScripts(Guid? uid = null)
    {
        Blocker.Show(Translater.Instant("Pages.Scripts.Labels.UpdatingScripts"));
        try
        {
            if(uid == null)
                await HttpHelper.Post("/api/repository/update-scripts");
            else
                await HttpHelper.Post($"/api/repository/update-specific-scripts", new ReferenceModel<Guid> { Uids = [uid.Value] });
                
            await Refresh();
        }
        finally
        {
            Blocker.Hide();
        }
    }
    
    /// <summary>
    /// Updates the DockerMods
    /// </summary>
    /// <param name="uid">the UID of the DockerMod to update</param>
    private async Task UpdateDockerMods(Guid? uid = null)
    {
        Blocker.Show("Pages.DockerMod.Messages.Updating");
        StateHasChanged();
        try
        {
            var uids = uid == null ? Data.DockerModUpdates.Select(x => x.Uid).ToArray() : [uid.Value];
            await HttpHelper.Post($"/api/repository/DockerMod/update", new ReferenceModel<Guid> { Uids = uids });
            await Refresh();
        }
        finally
        {
            Blocker.Hide();
            StateHasChanged();
        }
    }

    /// <summary>
    /// Disposes the component
    /// </summary>
    public void Dispose()
    {
        feService.Dashboard.UpdateInfoUpdated -= OnUpdatesUpdateInfo;
    }
}