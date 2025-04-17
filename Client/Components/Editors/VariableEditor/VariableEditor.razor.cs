using System.Web;
using FileFlows.ServerShared.Models;

namespace FileFlows.Client.Components.Editors;

/// <summary>
/// Variable editor
/// </summary>
public partial class VariableEditor : ModalEditor
{
    /// <summary>
    /// Gets or sets if the Variable 
    /// </summary>
    public Variable Model { get; set; }

    /// <inheritdoc />
    public override string HelpUrl => "https://fileflows.com/docs/webconsole/config/extensions/variables";

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        Title = Translater.Instant("Pages.Variable.Title");
    }

    /// <inheritdoc />
    public override async Task LoadModel()
    {
        if ((Options as ModalEditorOptions)?.Model is Variable model)
        {
            Model = model;
        }
        else
        {
            var uid = GetModelUid();

            var result = await HttpHelper.Get<Variable>("/api/variable/" + uid);
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
    /// Saves the Variable
    /// </summary>
    public override async Task Save()
    {
        Container.ShowBlocker();
        
        try
        {
            var saveResult = await HttpHelper.Post<Variable>($"/api/variable", Model);
            if (saveResult.Success == false)
            {
                feService.Notifications.ShowError(saveResult.Body?.EmptyAsNull() ?? 
                                                  Translater.Instant("ErrorMessages.SaveFailed"));
                return;
            }

            await Task.Delay(500); // give change for Variable to get updated in list

            TaskCompletionSource.TrySetResult(saveResult.Data);
        }
        finally
        {
             Container.HideBlocker();
        }
        
    }
}