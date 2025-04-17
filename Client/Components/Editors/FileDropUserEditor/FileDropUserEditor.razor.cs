using System.Web;
using FileFlows.Plugin;
using FileFlows.ServerShared.Models;

namespace FileFlows.Client.Components.Editors;

/// <summary>
/// FileDrop  User editor
/// </summary>
public partial class FileDropUserEditor : ModalEditor
{
    /// <summary>
    /// Gets or sets if the User 
    /// </summary>
    public FileDropUser Model { get; set; }

    /// <inheritdoc />
    public override string HelpUrl => "https://fileflows.com/docs/filedrop/users";


    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        Title = Translater.Instant("Pages.FileDrop.User.Title");
    }

    /// <inheritdoc />
    public override async Task LoadModel()
    {
        if ((Options as ModalEditorOptions)?.Model is FileDropUser model == false )
        {
            Close();
            return;
        }

        Model = new ()
        {
            Name = model.Name,
            Uid = model.Uid,
            Enabled = model.Enabled,
            Picture = model.Picture,
            Provider = model.Provider,
            Tokens = model.Tokens,
            DateCreated = model.DateCreated,
            DateModified = model.DateModified,
            DisplayName = model.DisplayName,
            PasswordHash = model.PasswordHash,
            PictureBase64 = model.PictureBase64,
            ProviderUid = model.ProviderUid,
            LastAutoTokensUtc = model.LastAutoTokensUtc
        };
        
        StateHasChanged();
        await Task.CompletedTask;
    }

    
    /// <summary>
    /// Saves the User
    /// </summary>
    public override async Task Save()
    {
        Container.ShowBlocker();
        
        try
        {
            var saveResult = await HttpHelper.Post<FileDropUser>($"/api/file-drop/user", Model);
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