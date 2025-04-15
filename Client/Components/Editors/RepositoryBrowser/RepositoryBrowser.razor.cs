using FileFlows.Client.Components.Common;
using FileFlows.Plugin;
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
    /// Gets or sets if icons should be shown
    /// </summary>
    private bool Icons { get; set; }

    /// <summary>
    /// Gets or sets the repository type
    /// </summary>
    private RepositoryType Type { get; set; }

    /// <inheritdoc />
    public override string HelpUrl => "https://fileflows.com/docs/webconsole/config/extensions/nodes";
    
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
        var result = await HttpHelper.Get<List<RepositoryObject>>("/api/repository/by-type/" + Type);
        if (result.Success == false)
        {
            feService.Notifications.ShowError(result.Body, duration: 15_000);
            // close this and show message
            Close();
            return;
        }

        if (Type == RepositoryType.Script)
        {
            Icons = false;
            foreach (var item in result.Data)
            {
                item.Icon = ScriptIconHelper.GetIcon(item.Name);
            }
        }
        else if (Type ==  RepositoryType.DockerMod)
        {
            Icons = true;
            foreach (var item in result.Data)
            {
                string key = item.Name.Replace(".", "").Replace("-", "").Dehumanize();
                item.Name = Translater.TranslateIfHasTranslation($"DockerMods.{key}.Label", item.Name);
                item.Description =
                    Translater.TranslateIfHasTranslation($"DockerMods.{key}.Description", item.Description);
            }
        }

        Data = result.Data;
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
        Container.ShowBlocker();
        try
        {
            var result = await HttpHelper.Post($"/api/repository/download/{Type}", items);
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
        }
        // Blocker.Show();
        // List<IFlowField> fields;
        // object model;
        // try
        // {
        //     var result = await HttpHelper.Post<FormFieldsModel>($"/api/repository/{Type}/fields", item);
        //     if (result.Success == false)
        //         return;
        //     fields = result.Data.Fields.Select(x => (IFlowField)x).ToList();
        //     model = result.Data.Model;
        //     if(Type == "DockerMod")
        //     {
        //         if(model is IDictionary<string, object> dockerModel)
        //         {
        //             string key = item.Name.Replace(".", "").Replace("-", "").Dehumanize();
        //             dockerModel["Name"] = Translater.TranslateIfHasTranslation($"DockerMods.{key}.Label", item.Name);
        //             dockerModel["Description"] = Translater.TranslateIfHasTranslation($"DockerMods.{key}.Description", item.Description);
        //         }
        //     }
        // }
        // finally
        // {
        //     Blocker.Hide();
        // }
        //
        // await Editor.Open(new () { TypeName = "Pages.RepositoryObject", Title = item.Name, Fields = fields, 
        //     Model = model, ReadOnly= true});
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
    Script
}