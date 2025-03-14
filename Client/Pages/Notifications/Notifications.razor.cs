namespace FileFlows.Client.Pages;

/// <summary>
/// Notifications page
/// </summary>
public partial class Notifications : ListPage<Guid, Notification>
{
    /// <inheritdoc />
    public override string ApiUrl => "/api/notification";
    
    // /// <inheritdoc />
    // public override async Task PostLoad()
    // {
    //     await feService.Refresh();
    // }

    /// <inheritdoc />
    public override Task<bool> Edit(Notification item)
    {
        throw new NotImplementedException();
    }
}