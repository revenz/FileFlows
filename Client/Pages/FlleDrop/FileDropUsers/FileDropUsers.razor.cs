using FileFlows.Client.Components.Editors;
using FileFlows.Client.Shared;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Pages.FileDrop;

/// <summary>
/// File Drop Users page
/// </summary>
public partial class FileDropUsers : ListPage<Guid, FileDropUser>
{
    /// <summary>
    /// Gets or sets the modal service
    /// </summary>
    [Inject] private IModalService ModalService { get; set; }
    
    /// <inheritdoc />
    public override string ApiUrl => "/api/file-drop/user";
    
    /// <summary>
    /// The current page
    /// </summary>
    private int PageIndex;
    /// <summary>
    /// The total number of items across all pages
    /// </summary>
    private int TotalItems;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        Layout.SetInfo(Translater.Instant("Pages.FileDrop.User.Plural"), "fas fa-user-astronaut", noPadding: true);
        base.OnInitialized();
    }

    /// <inheritdoc />
    public override string FetchUrl => $"{ApiUrl}?page={PageIndex}";
    
    /// <inheritdoc />
    protected override async Task<RequestResult<List<FileDropUser>>> FetchData()
    {
        var request = await HttpHelper.Get<List<FileDropUser>>(FetchUrl);

        if (request.Success == false)
        {
            return new RequestResult<List<FileDropUser>>
            {
                Body = request.Body,
                Success = request.Success
            };
        }

        if (request.Headers.ContainsKey("x-total-items") &&
            int.TryParse(request.Headers["x-total-items"], out int totalItems))
        {
            this.TotalItems = totalItems;
        }
        
        var result = new RequestResult<List<FileDropUser>>
        {
            Body = request.Body,
            Success = request.Success,
            Data = request.Data
        };
        return result;
    }
    
    /// <summary>
    /// Changes to a specific page
    /// </summary>
    /// <param name="index">the page to change to</param>
    private async Task PageChange(int index)
    {
        PageIndex = index;
        await this.Refresh();
    }

    /// <summary>
    /// Updates the number of items shown on a page
    /// </summary>
    /// <param name="size">the number of items</param>
    private async Task PageSizeChange(int size)
    {
        this.PageIndex = 0;
        await this.Refresh();
    }

    /// <inheritdoc />
    public override async Task<bool> Edit(FileDropUser item)
    {
        var result = await ModalService.ShowModal<FileDropUserEditor, FileDropUser>(new ModalEditorOptions()
        {
            Model = item
        });
        if(result.IsFailed == false)
            await Load(result.Value.Uid);
        return false;
    }
}