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
                Toast.ShowEditorError( Translater.TranslateIfNeeded(saveResult.Body?.EmptyAsNull() ?? "ErrorMessages.SaveFailed"));
                return false;
            }

            int index = this.Data.FindIndex(x => x.Uid == saveResult.Data.Uid);
            if (index < 0)
                this.Data.Add(saveResult.Data);
            else
                this.Data[index] = saveResult.Data;

            await this.Load(saveResult.Data.Uid);

            return true;
        }
        finally
        {
            Blocker.Hide();
            this.StateHasChanged();
        }
    }
}