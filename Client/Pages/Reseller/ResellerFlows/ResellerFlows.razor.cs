namespace FileFlows.Client.Pages.Reseller;

/// <summary>
/// Reseller Flows page
/// </summary>
public partial class ResellerFlows : ListPage<Guid, ResellerFlow>
{
    /// <inheritdoc />
    public override string ApiUrl => "/api/reseller/flow";

    /// <summary>
    /// Translation strings
    /// </summary>
    private string lblPageTitle;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        lblPageTitle = Translater.Instant("Pages.Resellers.Flows.Title");
    }

    /// <summary>
    /// Adds an item
    /// </summary>
    private async Task Add()
    {
        await Edit(new ()
        {  
            Enabled = true, 
            Tokens = 10,
            MaxFileSize = 10_000_000
        });
    }
    
    /// <inheritdoc />
    public override async Task<bool> Edit(ResellerFlow item)
    {
        // this.EditingItem = library;
        return await OpenEditor(item);
    }

    /// <summary>
    /// Opens the flow in the editor
    /// </summary>
    /// <param name="flowUid">the UID of the flow</param>
    private void OpenFlow(Guid? flowUid)
    {
        if (flowUid == null || Profile.HasRole(UserRole.Flows) == false)
            return;

        NavigationManager.NavigateTo($"/flows/{flowUid}");
    }
}