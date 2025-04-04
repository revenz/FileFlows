namespace FileFlows.Client.Pages;

/// <summary>
/// Notifications page
/// </summary>
public partial class Notifications : ListPage<Guid, Notification>
{
    /// <inheritdoc />
    public override string ApiUrl => "/api/notification";
    
    protected override void OnInitialized()
    {
        Profile ??= feService.Profile.Profile;
        lblAdd = Translater.Instant("Labels.Add");
        lblEdit = Translater.Instant("Labels.Edit");
        lblDelete = Translater.Instant("Labels.Delete");
        lblDeleting = Translater.Instant("Labels.Deleting");
        lblRefresh = Translater.Instant("Labels.Refresh");

        Data = feService.Notifications.Notifications;
    }

    /// <inheritdoc />
    public override Task<bool> Edit(Notification item)
    {
        throw new NotImplementedException();
    }
}