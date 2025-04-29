using FileFlows.Client.Components.Editors;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Pages;

/// <summary>
/// Page for revisions 
/// </summary>
public partial class Users: ListPage<Guid, User>
{
    /// <summary>
    /// Gets or sets the modal service
    /// </summary>
    [Inject] private IModalService ModalService { get; set; }
    
    /// <inheritdoc />
    public override string ApiUrl => "/api/user";

    /// <inheritdoc />
    public override string FetchUrl => $"{ApiUrl}";

    private string lblLastLoggedIn, lblNever, lblAdministrator, lblAddress;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        Layout.SetInfo(Translater.Instant("Pages.Users.Title"), "fas fa-users");
        lblLastLoggedIn = Translater.Instant("Pages.Users.Labels.LastLoggedIn");
        lblAddress = Translater.Instant("Pages.Users.Labels.Address");
        lblNever = Translater.Instant("Pages.Users.Labels.Never");
        lblAdministrator = Translater.Instant("Pages.Users.Labels.Administrator");
    }

    /// <summary>
    /// Gets if they are licensed for this page
    /// </summary>
    /// <returns>if they are licensed for this page</returns>
    protected override bool Licensed()
        => Profile.LicensedFor(LicenseFlags.UserSecurity); 

    /// <summary>
    /// Adds a new user
    /// </summary>
    private async Task Add()
    {
        var result = await ModalService.ShowModal<UserEditor, User>(new ModalEditorOptions()
        {
            Model = new User()
        });
        if(result.IsFailed == false)
            await Load(result.Value.Uid);
    }
    
    /// <inheritdoc />
    public override async Task<bool> Edit(User item)
    {
        var result = await ModalService.ShowModal<UserEditor, User>(new ModalEditorOptions()
        {
            Uid = item.Uid
        });
        if(result.IsFailed == false)
            await Load(result.Value.Uid);
        return false;
    }
    

    /// <summary>
    /// Called after deleting items
    /// </summary>
    /// <returns>a task to await</returns>
    protected override Task PostDelete()
    {
        // check if users need to be removed
        if (this.Data?.Any() != true &&
            (Profile.ConfigurationStatus & ConfigurationStatus.Users) == ConfigurationStatus.Users)
            Profile.ConfigurationStatus -= ConfigurationStatus.Users;
        return Task.CompletedTask;
    }
}