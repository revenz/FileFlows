using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Pages;

/// <summary>
/// Page for revisions 
/// </summary>
public partial class Revisions: ListPage<Guid, RevisionedObject>
{
    public override string ApiUrl => "/api/revision";

    public override string FetchUrl => $"{ApiUrl}/list";

    private string lblTitle;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        lblTitle = Translater.Instant("Pages.Revisions.Title");
    }

    /// <summary>
    /// Gets if they are licensed for this page
    /// </summary>
    /// <returns>if they are licensed for this page</returns>
    protected override bool Licensed()
        => Profile.LicensedFor(LicenseFlags.Revisions); 

    public override async Task<bool> Edit(RevisionedObject item)
    {
        await Revisions();
        return false;
    }
}