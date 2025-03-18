using FileFlows.Client.Components;
using FileFlows.Client.Components.Inputs;
using FileFlows.Plugin;

namespace FileFlows.Client.Pages;

/// <summary>
/// Page for Tags
/// </summary>
public partial class Tags : ListPage<Guid, Tag>, IDisposable
{
    /// <inheritdoc />
    public override string ApiUrl => "/api/tag";
    
    /// <summary>
    /// Translation strings
    /// </summary>
    private string lblTitle;
        
    /// <summary>
    /// The item being edited
    /// </summary>
    private Tag EditingItem = null;
    
    /// <inheritdoc />
    protected override string DeleteMessage => "Pages.Tags.Messages.DeleteItems";

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        Profile = feService.Profile.Profile;
        base.OnInitialized(false);
        lblTitle = Translater.Instant("Pages.Tags.Title");
        feService.Tag.TagsUpdated += TagOnTagsUpdated;
        Data = feService.Tag.Tags;
    }

    /// <summary>
    /// Called when the tags are updated
    /// </summary>
    /// <param name="obj">the updated tags</param>
    private void TagOnTagsUpdated(List<Tag> obj)
    {
        Data = obj;
        StateHasChanged();
    }

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            Table.SetData(Data);
            StateHasChanged();
        }

        base.OnAfterRender(firstRender);
    }

    /// <summary>
    /// Adds a new tag
    /// </summary>
    private async Task Add()
        => await Edit(new Tag());

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
        StateHasChanged();

        try
        {
            var saveResult = await HttpHelper.Post<Tag>($"{ApiUrl}", model);
            if (saveResult.Success == false)
            {
                Toast.ShowEditorError( saveResult.Body?.EmptyAsNull() ?? Translater.Instant("ErrorMessages.SaveFailed"));
                return false;
            }

            return true;
        }
        finally
        {
            Blocker.Hide();
            StateHasChanged();
        }
    }

    /// <summary>
    /// Disposes of the component
    /// </summary>
    public void Dispose()
    {
        feService.Tag.TagsUpdated -= TagOnTagsUpdated;
    }
}