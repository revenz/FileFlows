using System.Web;
using FileFlows.ServerShared.Models;

namespace FileFlows.Client.Components.Editors;

/// <summary>
/// DockerMod editor
/// </summary>
public partial class DockerModEditor : ModalEditor
{
    /// <summary>
    /// Gets or sets if the DockerMod 
    /// </summary>
    public DockerMod Model { get; set; }

    /// <inheritdoc />
    public override string HelpUrl => "https://fileflows.com/docs/webconsole/config/extensions/dockermods";

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        Title = Translater.Instant("Pages.DockerMod.Title");
    }

    /// <inheritdoc />
    public override async Task LoadModel()
    {
        if ((Options as ModalEditorOptions)?.Model is DockerMod model)
        {
            if (model.Repository)
            {
                // need to get the code from backend
                var contentResult =
                    await HttpHelper.Get<string>("/api/repository/content?path=" + HttpUtility.UrlEncode(model.Code));
                if (contentResult.Success == false || string.IsNullOrWhiteSpace(contentResult.Data))
                {
                    Close();
                    return;
                }

                int index = contentResult.Data.IndexOf("#!/bin/bash", StringComparison.InvariantCultureIgnoreCase);
                model.Code = index > 0 ? contentResult.Data[index..] : contentResult.Data;
            }

            Model = model;
        }
        else
        {
            var uid = GetModelUid();

            var result = await HttpHelper.Get<DockerMod>("/api/dockermod/" + uid);
            if (result.Success == false || result.Data == null)
            {
                Close();
                return;
            }

            Model = result.Data;
        }

        ReadOnly = Model.Repository;
        if (Model.Repository)
            Title = Model.Name;

        StateHasChanged(); // needed to update the title
    }

    
    /// <summary>
    /// Saves the DockerMod
    /// </summary>
    public override async Task Save()
    {
        Container.ShowBlocker();
        
        try
        {
            var saveResult = await HttpHelper.Post<DockerMod>($"/api/dockermod", Model);
            if (saveResult.Success == false)
            {
                feService.Notifications.ShowError(saveResult.Body?.EmptyAsNull() ?? 
                                                  Translater.Instant("ErrorMessages.SaveFailed"));
                return;
            }

            await Task.Delay(500); // give change for DockerMod to get updated in list

            TaskCompletionSource.TrySetResult(saveResult.Data);
        }
        finally
        {
             Container.HideBlocker();
        }
        
    }
}