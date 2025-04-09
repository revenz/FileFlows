using FileFlows.Client.Components.Dialogs;

using FileFlows.Client.Components;
using FileFlows.Plugin;
using FileFlows.Client.Components.Inputs;
using System.Text.Json;

namespace FileFlows.Client.Pages;

public partial class Plugins : ListPage<Guid, PluginInfoModel>, IDisposable
{
    public override string ApiUrl => "/api/plugin";

    private PluginBrowser PluginBrowser { get; set; }

    private string lblTitle, lblSettings, lblInUse, lblFlowElement, lblFlowElements;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        Profile = feService.Profile.Profile;
        base.OnInitialized(false);
        lblTitle = Translater.Instant("Pages.Plugins.Title");
        lblSettings = Translater.Instant("Labels.Settings");
        lblInUse = Translater.Instant("Labels.InUse");
        lblFlowElement = Translater.Instant("Labels.FlowElement");
        lblFlowElements = Translater.Instant("Labels.FlowElements");
        feService.Plugin.PluginsUpdated += PluginOnPluginsUpdated;
        Data = feService.Plugin.Plugins;
    }

    /// <summary>
    /// Plugins have been updated
    /// </summary>
    /// <param name="data">the updated plugins</param>
    private void PluginOnPluginsUpdated(List<PluginInfoModel> data)
    {
        Data = data;
        StateHasChanged();
    }
    
    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            Table.SetData(Data);
            StateHasChanged();
        }

        base.OnAfterRender(firstRender);
    }

    protected override string DeleteMessage => "Pages.Plugins.Messages.DeletePlugins";

    async Task Add()
        => await PluginBrowser.Open();

    async Task Update()
    {
        var plugins = Table.GetSelected()?.Select(x => x.Uid)?.ToArray() ?? new System.Guid[] { };
        if (plugins?.Any() != true)
            return;
        await Update(plugins);
    }
    
    async Task Update(params Guid[] plugins)
    {
        Blocker.Show("Pages.Plugins.Messages.UpdatingPlugins");
        this.StateHasChanged();
        Data.Clear();
        try
        {
            await HttpHelper.Post($"{ApiUrl}/update", new ReferenceModel<Guid> { Uids = plugins });
        }
        finally
        {
            Blocker.Hide();
            this.StateHasChanged();
        }
    }

    private PluginInfo EditingPlugin = null;

    async Task<bool> SaveSettings(ExpandoObject model)
    {
        Blocker.Show();
        this.StateHasChanged();

        try
        {
            string json = System.Text.Json.JsonSerializer.Serialize(model);
            var pluginResult = await HttpHelper.Post($"{ApiUrl}/{EditingPlugin.PackageName}/settings", json);
            if (pluginResult.Success == false)
            {
                feService.Notifications.ShowEditorError( Translater.Instant("ErrorMessages.SaveFailed"));
                return false;
            }
            return true;
        }
        finally
        {
            Blocker.Hide();
            this.StateHasChanged();
        }
    }

    public override async Task<bool> Edit(PluginInfoModel plugin)
    {
        if (plugin?.HasSettings != true)
            return false;
        
        Blocker.Show();
        this.StateHasChanged();
        Data.Clear();

        PluginInfo? pluginInfo = null;

        ExpandoObject model = new ExpandoObject();
        try
        {
            var pluginInfoResult = await HttpHelper.Get<PluginInfo>($"{ApiUrl}/{plugin.Uid}");
            if (pluginInfoResult.Success == false)
                return false;

            pluginInfo = pluginInfoResult.Data;
        
            var pluginResult = await HttpHelper.Get<string>($"{ApiUrl}/{plugin.PackageName}/settings");
            if (pluginResult.Success == false)
                return false;
            if (string.IsNullOrWhiteSpace(pluginResult.Data) == false)
                model = JsonSerializer.Deserialize<ExpandoObject>(pluginResult.Data) ?? new ExpandoObject();
        }
        finally
        {
            Blocker.Hide();
            this.StateHasChanged();
        }
        this.EditingPlugin = plugin;

        // clone the fields as they get wiped
        var fields = pluginInfo.Settings.Select(x => (IFlowField)x).ToList();

        await Editor.Open(new()
        {
            TypeName = "Plugins." + pluginInfo.PackageName, Title = pluginInfo.Name, Fields = fields, Model = model,
            HelpUrl = GetPluginHelpUrl(plugin, true),
            SaveCallback = SaveSettings
        });
        return false; // we dont need to reload the list
    }


    private async Task AboutAction()
    {
        var item = Table.GetSelected().FirstOrDefault();
        if (item != null)
            await About(item);
    }
    private async Task DoubleClick(PluginInfoModel plugin)
    {
        if (plugin.HasSettings == true)
            await Edit(plugin);
    }

    private async Task About(PluginInfoModel plugin)
    {
        await Editor.Open(new()
        {
            TypeName = "Pages.Plugins", Title = plugin.Name, HelpUrl = GetPluginHelpUrl(plugin), Fields = new List<IFlowField>
            {
                new ElementField
                {
                    Name = nameof(plugin.Name),
                    InputType = FormInputType.TextLabel
                },
                new ElementField
                {
                    Name = nameof(plugin.Version),
                    InputType = FormInputType.TextLabel
                },
                new ElementField
                {
                    Name = nameof(plugin.Description),
                    InputType = FormInputType.TextLabel,
                    Parameters = new Dictionary<string, object>
                    {
                        { nameof(InputTextLabel.Pre), true }
                    }
                },
                new ElementField
                {
                    Name = nameof(plugin.Elements),
                    InputType = FormInputType.Checklist,
                    Parameters = new Dictionary<string, object>
                    {
                        { nameof(InputChecklist.ListOnly), true },
                        {
                            nameof(InputChecklist.Options),
                            plugin.Elements?.OrderBy(x =>x.Name?.ToLowerInvariant())?.Select(x => new ListOption { Label = x.Name, Value = x })?.ToList() ??
                            new List<ListOption>()
                        }
                    }
                },
            },
            Model = plugin, ReadOnly = true
        });
    }

    /// <summary>
    /// Gets the help URL for the plugin 
    /// </summary>
    /// <param name="plugin">the plugin</param>
    /// <returns>the help URL</returns>
    private string GetPluginHelpUrl(PluginInfoModel plugin, bool settings = false)
    {
        string name = plugin.Name;
        if (plugin.PackageName.EndsWith("Nodes") && plugin.Name is "Discord" or "Email" == false)
            name += " Nodes"; // for now will be removed later
        name = name.Replace(" ", "-").ToLowerInvariant();
        string url = $"https://fileflows.com/docs/plugins/{name}";
        if (settings)
            url += "/settings";
        return url;
    }


    public override async Task Delete()
    {
        var used = Table.GetSelected()?.Any(x => x.UsedBy?.Any() == true) == true;
        if (used)
        {
            feService.Notifications.ShowError("Pages.Plugins.Messages.DeleteUsed");
            return;
        }

        await base.Delete();
    }
    
    private async Task UsedBy()
    {
        var item = Table.GetSelected()?.FirstOrDefault();
        if (item?.UsedBy?.Any() != true)
            return;
        await OpenUsedBy(item);
    }
    
    /// <summary>
    /// Opens the used by dialog
    /// </summary>
    /// <param name="item">the plugin info model to open used by for</param>
    /// <returns>a task to await</returns>
    private Task OpenUsedBy(PluginInfoModel item)
        => UsedByDialog.Show(item.UsedBy);
    

    /// <summary>
    /// we only want to do the sort the first time, otherwise the list will jump around for the user
    /// </summary>
    private List<Guid> initialSortOrder;
    
    /// <inheritdoc />
    public override Task PostLoad()
    {
        if (initialSortOrder == null)
        {
            Data = Data?.OrderByDescending(x => x.Enabled)?.ThenBy(x => x.Name)
                ?.ToList();
            initialSortOrder = Data?.Select(x => x.Uid)?.ToList();
        }
        else
        {
            Data = Data?.OrderBy(x => initialSortOrder.Contains(x.Uid) ? initialSortOrder.IndexOf(x.Uid) : 1000000)
                .ThenBy(x => x.Name)
                ?.ToList();
        }
        return base.PostLoad();
    }

    /// <summary>
    /// Disposes of the component
    /// </summary>
    public void Dispose()
    {
        feService.Plugin.PluginsUpdated -= PluginOnPluginsUpdated;
    }
}