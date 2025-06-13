using FileFlows.Client.Components;
using FileFlows.Client.Components.Editors;
using FileFlows.Client.Components.Inputs;
using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Pages;

/// <summary>
/// Page for Tags
/// </summary>
public partial class Tags : ListPage<Guid, Tag>, IDisposable
{
    /// <inheritdoc />
    public override string ApiUrl => "/api/tag";
    
    /// <summary>
    /// Gets or sets the modal service
    /// </summary>
    [Inject] private IModalService ModalService { get; set; }
    
    /// <inheritdoc />
    protected override string DeleteMessage => "Pages.Tags.Messages.DeleteItems";

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        Profile = feService.Profile.Profile;
        Layout.SetInfo(Translater.Instant("Pages.Tags.Title"), "fas fa-tags", noPadding: true);
        base.OnInitialized(false);
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
    {
        await ModalService.ShowModal<TagEditor>(new ModalEditorOptions()
        {
            Model = new Tag()
            {
            }
        });
    }

    /// <inheritdoc />
    public override async Task<bool> Edit(Tag item)
    {
        await ModalService.ShowModal<TagEditor>(new ModalEditorOptions()
        {
            Model = new Tag()
            {
                Uid = item.Uid,
                Name = item.Name,
                Description = item.Description,
                Icon = item.Icon
            }
        });
        return false;
    }

    /// <summary>
    /// Disposes of the component
    /// </summary>
    public void Dispose()
    {
        feService.Tag.TagsUpdated -= TagOnTagsUpdated;
    }
}