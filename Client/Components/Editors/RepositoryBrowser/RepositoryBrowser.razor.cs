using FileFlows.Client.Components.Common;
using Humanizer;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Editors;

/// <summary>
/// Repository Browser
/// </summary>
public partial class RepositoryBrowser : ModalEditor
{
    /// <summary>
    /// Gets or sets the modal service
    /// </summary>
    [Inject] private IModalService ModalService { get; set; }
    
    /// <summary>
    /// Gets or sets the data
    /// </summary>
    public List<RepositoryObject> Data { get; set; }
    
    /// <summary>
    /// Gets or sets the table
    /// </summary>
    public FlowTable<RepositoryObject> Table { get; set; }

    /// <summary>
    /// Gets or sets the repository type
    /// </summary>
    private RepositoryType Type { get; set; }

    /// <inheritdoc />
    public override string HelpUrl => "https://fileflows.com/docs/webconsole/config/extensions/nodes";

    /// <summary>
    /// Maps repository objects to plugsin
    /// </summary>
    private Dictionary<RepositoryObject, PluginPackageInfo> Plugins = [];
    
    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();;
        ReadOnly = true; // removes the save/cancel button for a Close button
        Title = Translater.Instant("Labels.Repository");
        Type =  ((RepositoryOptions)Options).Type;
    }

    /// <inheritdoc />
    public override async Task LoadModel()
    {
        if (Type == RepositoryType.Plugins)
        {
            await LoadPlugins();
            return;
        }
        
        var result = await HttpHelper.Get<List<RepositoryObject>>("/api/repository/by-type/" + Type);
        if (result.Success == false)
        {
            feService.Notifications.ShowError(result.Body, duration: 15_000);
            // close this and show message
            Close();
            return;
        }

        if (Type is RepositoryType.ScriptFlow or RepositoryType.ScriptSystem)
        {
            foreach (var item in result.Data)
            {
                item.Icon = ScriptIconHelper.GetIcon(item.Name);
            }
        }
        else if (Type ==  RepositoryType.DockerMod)
        {
            foreach (var item in result.Data)
            {
                string key = item.Name.Replace(".", "").Replace("-", "").Dehumanize();
                item.Name = Translater.TranslateIfHasTranslation($"DockerMods.{key}.Label", item.Name);
                item.Description =
                    Translater.TranslateIfHasTranslation($"DockerMods.{key}.Description", item.Description);
            }
        }
        else if (Type ==  RepositoryType.Plugins)
        {
            foreach (var item in result.Data)
            {
                string key = item.Name.Replace(".", "").Replace("-", "").Dehumanize();
                item.Name = Translater.TranslateIfHasTranslation($"Plugins.{key}.Label", item.Name);
                item.Description =
                    Translater.TranslateIfHasTranslation($"Plugins.{key}.Description", item.Description);
            }
        }

        Data = result.Data;
    }

    /// <summary>
    /// Loads the plugins
    /// </summary>
    private async Task LoadPlugins()
    {
        var result = await HttpHelper.Get<List<PluginPackageInfo>>("/api/plugin/plugin-packages?missing=true");
        if (result.Success == false)
        {
            feService.Notifications.ShowError(result.Body, duration: 15_000);
            // close this and show message
            Close();
            return;
        }
        
        foreach (var item in result.Data)
        {
            string key = item.Name.Replace(".", "").Replace("-", "").Dehumanize();
            item.Name = Translater.TranslateIfHasTranslation($"Plugins.{key}.Label", item.Name);
            item.Description =
                Translater.TranslateIfHasTranslation($"Plugins.{key}.Description", item.Description);
        }

        Data = result.Data.Select(x =>
        {
            var ro = new RepositoryObject()
            {
                Name = x.Name,
                Description = x.Description,
                Icon = x.Icon,
            };
            Plugins[ro] = x;
            return ro;
        }).ToList();
    }

    
    
    /// <summary>
    /// Downloads the selected plugins
    /// </summary>
    private async Task Download()
    {
        var selected = Table.GetSelected().ToArray();
        var items = selected;
        if (items.Length == 0)
            return;
        Container.ShowBlocker();
        try
        {
            RequestResult<string> result;
            if (Type == RepositoryType.Plugins)
            {
                var packages = items.Where(x => Plugins.ContainsKey(x))
                    .Select(x => Plugins[x]).ToList();
                if (packages.Count == 0)
                    return;
                
                result = await HttpHelper.Post("/api/plugin/download", new { Packages = packages });
            }
            else
            {
                result = await HttpHelper.Post($"/api/repository/download/{Type}", items);
            }
            
            if (result.Success == false)
            {
                // close this and show message
                return;
            }

            Data = Data.Where(x => selected.Contains(x) == false).ToList();
            Table.SetData(Data);
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog("Error: " + ex.Message);
        }
        finally
        {
            Container.HideBlocker();;
        }
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
        switch (Type)
        {
            case RepositoryType.DockerMod:
                await ModalService.ShowModal<DockerModEditor>(new ModalEditorOptions()
                {
                    Model = new DockerMod()
                    {
                        Name = item.Name,
                        Description = item.Description,
                        Repository = true,
                        Code = item.Path
                    }
                });
                break;
            case RepositoryType.ScriptFlow:
            case RepositoryType.ScriptSystem:
                await ModalService.ShowModal<ScriptEditor>(new ModalEditorOptions()
                {
                    Model = new Script()
                    {
                        Name = item.Name,
                        Description = item.Description,
                        Repository = true,
                        Code = item.Path
                    }
                });
                break;
        }
    }

}


/// <summary>
/// Repository options
/// </summary>
public class RepositoryOptions : IModalOptions
{
    /// <summary>
    /// Gets or sets the repository type
    /// </summary>
    public RepositoryType Type { get; set; }
}

public enum RepositoryType
{
    DockerMod,
    Plugins,
    ScriptFlow,
    ScriptSystem
}