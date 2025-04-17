using System.Web;
using FileFlows.Client.Components.Inputs;
using FileFlows.ServerShared.Models;

namespace FileFlows.Client.Components.Editors;

/// <summary>
/// Resource editor
/// </summary>
public partial class ResourceEditor : ModalEditor
{
    /// <summary>
    /// Gets or sets if the Resource 
    /// </summary>
    public Resource Model { get; set; }

    /// <summary>
    /// Gets or sets the file data
    /// </summary>
    private FileData FileData { get; set; }

    /// <inheritdoc />
    public override string HelpUrl => "https://fileflows.com/docs/webconsole/config/system/resources";

    private List<Validator> Validators;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        Validators =
        [
            new Required(),
            new SafeName()
        ];
        Title = Translater.Instant("Pages.Resources.Singular");
    }

    /// <inheritdoc />
    public override async Task LoadModel()
    {
        if ((Options as ModalEditorOptions)?.Model is Resource model)
        {
            Model = model;
        }
        else
        {
            var uid = GetModelUid();

            var result = await HttpHelper.Get<Resource>("/api/resource/" + uid);
            if (result.Success == false || result.Data == null)
            {
                Close();
                return;
            }

            Model = result.Data;
        }

        FileData = Model is { MimeType: not null, Data: not null }
            ? new FileData()
            {
                MimeType = Model.MimeType,
                Content = Model.Data
            }
            : null;

        StateHasChanged(); 
    }

    
    /// <summary>
    /// Saves the Resource
    /// </summary>
    public override async Task Save()
    {
        Container.ShowBlocker();
        
        try
        {
            var saveResult = await HttpHelper.Post<Resource>($"/api/resource", new Resource()
            {
                Uid = Model.Uid,
                Name = Model.Name,
                Data = FileData.Content,
                MimeType = FileData.MimeType
            });
            if (saveResult.Success == false)
            {
                feService.Notifications.ShowError(saveResult.Body?.EmptyAsNull() ?? 
                                                  Translater.Instant("ErrorMessages.SaveFailed"));
                return;
            }

            TaskCompletionSource.TrySetResult(saveResult.Data);
        }
        finally
        {
             Container.HideBlocker();
        }
        
    }
}