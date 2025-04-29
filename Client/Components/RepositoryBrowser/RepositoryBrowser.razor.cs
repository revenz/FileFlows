using FileFlows.Client.ClientModels;
using Microsoft.AspNetCore.Components;
using FileFlows.Client.Components.Inputs;
using FileFlows.Plugin;
using FileFlows.Client.Components.Common;
using FileFlows.Client.Services.Frontend;
using Humanizer;

namespace FileFlows.Client.Components;

/// <summary>
/// Browser for generic Repository Objects
/// </summary>
public partial class RepositoryBrowser : ComponentBase
{
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
    public FlowTable<RepositoryObject> Table { get; set; }
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
    private string lblTitle, lblClose;

    /// <summary>
    /// The open task to complete when closing
    /// </summary>
    TaskCompletionSource<bool> OpenTask;
    /// <summary>
    /// If the components needs rendering
    /// </summary>
    private bool _needsRendering = false;

    /// <summary>
    /// Gets or sets the type
    /// </summary>
    [Parameter] public string Type { get; set; }

    /// <summary>
    /// Gets or sets if icons should be shown
    /// </summary>
    [Parameter] public bool Icons { get; set; }

    /// <summary>
    /// Gets or sets the icon to show in the title bar
    /// </summary>
    [Parameter] public string Icon { get; set; }

    /// <summary>
    /// Gets or sets the title
    /// </summary>
    [Parameter]
    public string Title { get; set; }
    
    /// <inheritdoc />
    protected override void OnInitialized()
    {
        lblClose = Translater.Instant("Labels.Close");
        lblTitle = Translater.TranslateIfNeeded(Title?.EmptyAsNull() ?? "Labels.Repository");
    }

    /// <summary>
    /// Opens the plugin browser
    /// </summary>
    /// <returns>returns true if one or more plugins were downloaded</returns>
    internal Task<bool> Open()
    {
        this.Visible = true;
        this.Table.SetData(new List<RepositoryObject>());
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
            var result = await HttpHelper.Get<List<RepositoryObject>>("/api/repository/by-type/" + this.Type);
            if (result.Success == false)
            {
                feService.Notifications.ShowError(result.Body, duration: 15_000);
                // close this and show message
                this.Close();
                return;
            }

            if (Type.StartsWith("Script:"))
            {
                foreach (var item in result.Data)
                {
                    item.Icon = ScriptIconHelper.GetIcon(item.Name);
                }
            }

            if (Type == "DockerMod")
            {
                foreach (var item in result.Data)
                {
                    string key = item.Name.Replace(".", "").Replace("-", "").Dehumanize();
                    item.Name = Translater.TranslateIfHasTranslation($"DockerMods.{key}.Label", item.Name);
                    item.Description =
                        Translater.TranslateIfHasTranslation($"DockerMods.{key}.Description", item.Description);
                }
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
            var result = await HttpHelper.Post($"/api/repository/download/{Type}", items);
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
    /// <param name="item">the item</param>
    private async Task View(RepositoryObject item)
    {
        Blocker.Show();
        List<IFlowField> fields;
        object model;
        try
        {
            var result = await HttpHelper.Post<FormFieldsModel>($"/api/repository/{Type}/fields", item);
            if (result.Success == false)
                return;
            fields = result.Data.Fields.Select(x => (IFlowField)x).ToList();
            model = result.Data.Model;
            if(Type == "DockerMod")
            {
                if(model is IDictionary<string, object> dockerModel)
                {
                    string key = item.Name.Replace(".", "").Replace("-", "").Dehumanize();
                    dockerModel["Name"] = Translater.TranslateIfHasTranslation($"DockerMods.{key}.Label", item.Name);
                    dockerModel["Description"] = Translater.TranslateIfHasTranslation($"DockerMods.{key}.Description", item.Description);
                }
            }
        }
        finally
        {
            Blocker.Hide();
        }

        await Editor.Open(new () { TypeName = "Pages.RepositoryObject", Title = item.Name, Fields = fields, 
            Model = model, ReadOnly= true});
    }

}