using FileFlows.Client.Components;
using FileFlows.Client.Components.Inputs;
using FileFlows.Plugin;

namespace FileFlows.Client.Pages;

/// <summary>
/// Editor for the DockerMod
/// </summary>
public partial class DockerMods
{
    /// <summary>
    /// Opens the editor
    /// </summary>
    /// <param name="item">the item being edited</param>
    /// <returns>true if successful, otherwise false</returns>
    private async Task<bool> OpenEditor(DockerMod item)
    {
        if (item.Uid != Guid.Empty)
        {
            // load the complete DockerMod
            Blocker.Show();
            var dmResult = await HttpHelper.Get<DockerMod>($"{ApiUrl}/{item.Uid}");
            Blocker.Hide();

            if (dmResult.Success == false || dmResult.Data == null)
            {
                ShowEditHttpError(dmResult, "DockerMod not found");
                return false;
            }

            item = dmResult.Data;
        }

        var fields = new List<IFlowField>();
        fields.Add(new ElementField()
        {
            Name = nameof(item.Name),
            InputType = FormInputType.Text,
            Parameters = new ()
            {
                { nameof(InputCode.ReadOnly), item.Repository}
            }
        });
        fields.Add(new ElementField()
        {
            Name = nameof(item.Description),
            InputType = FormInputType.TextArea,
            Parameters = new ()
            {
                { nameof(InputTextArea.Rows), 3},
                { nameof(InputCode.ReadOnly), item.Repository}
            }
        });
        fields.Add(new ElementField()
        {
            Name = nameof(item.Icon),
            InputType = FormInputType.IconPicker,
            Parameters = new ()
            {
                { nameof(InputCode.ReadOnly), item.Repository}
            }
        });
        fields.Add(new ElementField()
        {
            InputType = FormInputType.HorizontalRule
        });
        fields.Add(new ElementField()
        {
            Name = nameof(item.Code),
            InputType = FormInputType.Code,
            Parameters = new ()
            {
                { nameof(InputCode.Language), "shell" },
                { nameof(InputCode.ReadOnly), item.Repository}
            }
        });
        
        await Editor.Open(new()
        {
            TypeName = "Pages.DockerMod", Title = "Pages.DockerMod.Title", Model = item,
            SaveCallback = Save, ReadOnly = item.Repository, Fields = fields,
            HelpUrl = "https://fileflows.com/docs/webconsole/extensions/dockermods"
        });
        return true;
    }

    /// <summary>
    /// Saves the edited item
    /// </summary>
    /// <param name="model">the model</param>
    /// <returns>true if successfully saved, otherwise false</returns>
    async Task<bool> Save(ExpandoObject model)
    {
        Blocker.Show();
        this.StateHasChanged();

        try
        {
            var saveResult = await HttpHelper.Post<DockerMod>($"{ApiUrl}", model);
            if (saveResult.Success == false)
            {
                feService.Notifications.ShowEditorError( Translater.TranslateIfNeeded(saveResult.Body?.EmptyAsNull() ?? "ErrorMessages.SaveFailed"));
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
}