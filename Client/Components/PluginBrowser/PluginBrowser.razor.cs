using Microsoft.AspNetCore.Components;
using FileFlows.Client.Components.Inputs;
using FileFlows.Plugin;
using FileFlows.Client.Components.Common;
using FileFlows.Client.Services.Frontend;

namespace FileFlows.Client.Components;

/// <summary>
/// Browser for Plugins
/// </summary>
public partial class PluginBrowser : ComponentBase
{
    /// <summary>
    /// The URL to get the Plugins
    /// </summary>
    const string ApiUrl = "/api/plugin";
    /// <summary>
    /// Gets or sets the blocker
    /// </summary>
    [CascadingParameter] public Blocker Blocker { get; set; }
    /// <summary>
    /// Gets or sets the frontend service
    /// </summary>
    [Inject] private FrontendService feService { get; set; }
    /// <summary>
    /// Gets or sets the editor
    /// </summary>
    [CascadingParameter] public Editor Editor { get; set; }
    /// <summary>
    /// Gets or sets the table
    /// </summary>
    public FlowTable<PluginPackageInfo> Table { get; set; }
    /// <summary>
    /// Gets or sets if this is visible
    /// </summary>
    public bool Visible { get; set; }
    /// <summary>
    /// If this has been updated
    /// </summary>
    private bool Updated;
    /// <summary>
    /// The translated labels
    /// </summary>
    private string lblTitle, lblClose, lblVersion, lblFlowElements, lblDescription;

    /// <summary>
    /// The open task to complete when closing
    /// </summary>
    TaskCompletionSource<bool> OpenTask;
    /// <summary>
    /// If the components needs rendering
    /// </summary>
    private bool _needsRendering = false;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        lblClose = Translater.Instant("Labels.Close");
        lblTitle = Translater.Instant("Pages.Plugins.Labels.PluginBrowser");
        lblVersion = Translater.Instant("Labels.Version");
        lblFlowElements = Translater.Instant("Labels.FlowElements");
        lblDescription = Translater.Instant("Labels.Description");
    }

    /// <summary>
    /// Opens the plugin browser
    /// </summary>
    /// <returns>returns true if one or more plugins were downloaded</returns>
    internal Task<bool> Open()
    {
        this.Visible = true;
        this.Table.SetData(new List<PluginPackageInfo>());
        OpenTask = new TaskCompletionSource<bool>();
        _ = LoadData();
        this.StateHasChanged();
        return OpenTask.Task;
    }

    /// <summary>
    /// Loads the plugins
    /// </summary>
    private async Task LoadData()
    {
        Blocker.Show();
        this.StateHasChanged();
        try
        {
            var result = await HttpHelper.Get<List<PluginPackageInfo>>(ApiUrl + "/plugin-packages?missing=true");
            if (result.Success == false)
            {
                feService.Notifications.ShowError(result.Body, duration: 15_000);
                // close this and show message
                this.Close();
                return;
            }

            foreach (var item in result.Data)
            {
                item.Name =
                    Translater.TranslateIfHasTranslation($"Plugins.{item.Package.Replace(".", "")}.Label", item.Name);
                item.Description =
                    Translater.TranslateIfHasTranslation($"Plugins.{item.Package.Replace(".", "")}.Description",
                        item.Description);
            }

            this.Table.SetData(result.Data);
        }
        finally
        {
            Blocker.Hide();
            this.StateHasChanged();
        }
    }

    /// <summary>
    /// Waits for the component to render
    /// </summary>
    private async Task WaitForRender()
    {
        _needsRendering = true;
        StateHasChanged();
        while (_needsRendering)
        {
            await Task.Delay(50);
        }
    }

    /// <summary>
    /// Closes the component
    /// </summary>
    private void Close()
    {
        OpenTask.TrySetResult(Updated);
        this.Visible = false;
    }

    /// <summary>
    /// Downloads the selected plugins
    /// </summary>
    private async Task Download()
    {
        var selected = Table.GetSelected().ToArray();
        var items = selected;
        if (items.Any() == false)
            return;
        this.Blocker.Show();
        this.StateHasChanged();
        try
        {
            this.Updated = true;
            var result = await HttpHelper.Post(ApiUrl + "/download", new { Packages = items });
            if (result.Success == false)
            {
                // close this and show message
                return;
            }
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog("Error: " + ex.Message);
        }
        finally
        {
            this.Blocker.Hide();
            this.StateHasChanged();
        }
        await LoadData();
    }

    /// <summary>
    /// When the view button is clicked
    /// </summary>
    private async Task ViewAction()
    {
        var item = Table.GetSelected().FirstOrDefault();
        if (item != null)
            await View(item);
    }

    /// <summary>
    /// Views the item
    /// </summary>
    /// <param name="plugin">the item</param>
    private async Task View(PluginPackageInfo plugin)
    {
        await Editor.Open(new () { TypeName = "Pages.Plugins", Title = plugin.Name, Fields = new List<IFlowField>
        {
            new ElementField
            {
                Name = nameof(plugin.Name),
                InputType = FormInputType.TextLabel
            },
            new ElementField
            {
                Name = nameof(plugin.Authors),
                InputType = FormInputType.TextLabel
            },
            new ElementField
            {
                Name = nameof(plugin.Version),
                InputType = FormInputType.TextLabel
            },
            new ElementField
            {
                Name = nameof(plugin.Url),
                InputType = FormInputType.TextLabel,
                Parameters = new Dictionary<string, object>
                {
                    { nameof(InputTextLabel.Link), true }
                }
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
                        plugin.Elements.Select(x => new ListOption{ Label = x, Value = x }).ToList()
                    }
                }
            },
        }, Model = plugin, ReadOnly= true});
    }

}