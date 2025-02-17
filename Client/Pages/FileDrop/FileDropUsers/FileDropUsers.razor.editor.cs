using FileFlows.Client.Components;
using FileFlows.Client.Components.Inputs;
using FileFlows.Plugin;

namespace FileFlows.Client.Pages.FileDrop;

/// <summary>
/// File Drop Users editor
/// </summary>
public partial class FileDropUsers
{

    /// <inheritdoc />
    public override async Task<bool> Edit(FileDropUser item)
    {
        var fields = new List<IFlowField>();
        fields.Add(new ElementField()
        {
            Name = nameof(item.Name),
            InputType = FormInputType.Text,
            Parameters = new ()
            {
                { nameof(InputCode.ReadOnly), true}
            }
        });
        fields.Add(new ElementField()
        {
            Name = nameof(item.Provider),
            InputType = FormInputType.Text,
            Parameters = new ()
            {
                { nameof(InputCode.ReadOnly), true}
            }
        });
        fields.Add(new ElementField()
        {
            Name = nameof(item.Name),
            InputType = FormInputType.Text,
            Parameters = new ()
            {
                { nameof(InputCode.ReadOnly), true}
            }
        });
        fields.Add(new ElementField()
        {
            InputType = FormInputType.HorizontalRule
        });
        fields.Add(new ElementField()
        {
            Name = nameof(item.Tokens),
            InputType = FormInputType.Int
        });
        
        await Editor.Open(new()
        {
            TypeName = "Pages.FileDrop.User", Title = "Pages.FileDrop.User.Title", Model = item,
            SaveCallback = Save, Fields = fields,
            HelpUrl = "https://fileflows.com/docs/webconsole/file-drop/users"
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
            var saveResult = await HttpHelper.Post<FileDropUser>($"{ApiUrl}", model);
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