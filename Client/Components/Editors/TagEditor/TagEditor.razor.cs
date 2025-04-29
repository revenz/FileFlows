using System.Web;
using FileFlows.ServerShared.Models;

namespace FileFlows.Client.Components.Editors;

/// <summary>
/// Tag editor
/// </summary>
public partial class TagEditor : ModalEditor
{
    /// <summary>
    /// Gets or sets if the Tag 
    /// </summary>
    public Tag Model { get; set; }

    /// <inheritdoc />
    public override string HelpUrl => "https://fileflows.com/docs/webconsole/config/extensions/tags";

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        Title = Translater.Instant("Pages.Tag.Title");
    }

    /// <inheritdoc />
    public override async Task LoadModel()
    {
        if ((Options as ModalEditorOptions)?.Model is Tag model)
        {
            Model = model;
        }
        else
        {
            var uid = GetModelUid();

            var result = await HttpHelper.Get<Tag>("/api/tag/" + uid);
            if (result.Success == false || result.Data == null)
            {
                Close();
                return;
            }

            Model = result.Data;
        }

        StateHasChanged(); // needed to update the title
    }

    
    /// <summary>
    /// Saves the Tag
    /// </summary>
    public override async Task Save()
    {
        Container.ShowBlocker();
        
        try
        {
            var saveResult = await HttpHelper.Post<Tag>($"/api/tag", Model);
            if (saveResult.Success == false)
            {
                feService.Notifications.ShowError(saveResult.Body?.EmptyAsNull() ?? 
                                                  Translater.Instant("ErrorMessages.SaveFailed"));
                return;
            }

            await Task.Delay(500); // give change for Tag to get updated in list

            TaskCompletionSource.TrySetResult(saveResult.Data);
        }
        finally
        {
             Container.HideBlocker();
        }
        
    }
}