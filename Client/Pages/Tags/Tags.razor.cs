using FileFlows.Client.Components;
using FileFlows.Client.Components.Inputs;
using FileFlows.Plugin;

namespace FileFlows.Client.Pages;

/// <summary>
/// Page for Tags
/// </summary>
public partial class Tags : ListPage<Guid, Tag>
{
    /// <inheritdoc />
    public override string ApiUrl => "/api/tag";
    
    /// <summary>
    /// The item being edited
    /// </summary>
    private Tag EditingItem = null;
    /// <inheritdoc />
    protected override string DeleteMessage => "Pages.Tags.Messages.DeleteItems";

    /// <summary>
    /// Adds a new tag
    /// </summary>
    private async Task Add()
    {
        await Edit(new Tag());
    }

    /// <inheritdoc />
    public override async Task<bool> Edit(Tag item)
    {
        this.EditingItem = item;
        List<IFlowField> fields = new ();
        fields.Add(new ElementField
        {
            InputType = FormInputType.Text,
            Name = nameof(item.Name),
            Validators = new List<Validator> {
                new Required()
            }
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.TextArea,
            Name = nameof(item.Description),
            Parameters = new ()
            {
                { nameof(InputTextArea.Rows), 5 }
            }
        });
        fields.Add(new ElementField()
        {
            Name = nameof(item.Icon),
            InputType = FormInputType.IconPicker
        });
        await Editor.Open(new () { TypeName = "Pages.Tags", Title = "Pages.Tags.Title", 
            Fields = fields, Model = item, SaveCallback = Save,
            FullWidth = true
        });
        return false;
    }

    async Task<bool> Save(ExpandoObject model)
    {
        Blocker.Show();
        this.StateHasChanged();

        try
        {
            var saveResult = await HttpHelper.Post<Tag>($"{ApiUrl}", model);
            if (saveResult.Success == false)
            {
                Toast.ShowEditorError( saveResult.Body?.EmptyAsNull() ?? Translater.Instant("ErrorMessages.SaveFailed"));
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