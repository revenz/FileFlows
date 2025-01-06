namespace FileFlows.Client.Pages.Reseller;

/// <summary>
/// Reseller Users page
/// </summary>
public partial class ResellerUsers : ListPage<Guid, ResellerUser>
{
    /// <inheritdoc />
    public override string ApiUrl => "/api/reseller/user";
    /// <summary>
    /// Translation strings
    /// </summary>
    private string lblPageTitle;
    
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
        base.OnInitialized();
        lblPageTitle = Translater.Instant("Pages.Resellers.Flows.Title");
    }

    /// <inheritdoc />
    public override string FetchUrl => $"{ApiUrl}?page={PageIndex}&pageSize={App.PageSize}";
    
    /// <inheritdoc />
    protected override async Task<RequestResult<List<ResellerUser>>> FetchData()
    {
        var request = await HttpHelper.Get<List<ResellerUser>>(FetchUrl);

        if (request.Success == false)
        {
            return new RequestResult<List<ResellerUser>>
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
        
        var result = new RequestResult<List<ResellerUser>>
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
}