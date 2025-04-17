using System.Web;
using FileFlows.Plugin;
using FileFlows.ServerShared.Models;

namespace FileFlows.Client.Components.Editors;

/// <summary>
/// User editor
/// </summary>
public partial class UserEditor : ModalEditor
{
    /// <summary>
    /// Gets or sets if the User 
    /// </summary>
    public User Model { get; set; }
    
    /// <summary>
    /// Gets or sets if the user is an administrator
    /// </summary>
    private bool Administrator { get; set; }

    /// <inheritdoc />
    public override string HelpUrl => "https://fileflows.com/docs/webconsole/config/security/users";

    private List<ListOption> RoleOptions;
    private List<object> Roles = [];
    private bool ShowPassword;
    private bool IsUser;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        Title = Translater.Instant("Pages.Users.Single");

        ShowPassword = feService.Profile.Profile.Security != SecurityMode.OpenIdConnect;
        RoleOptions = new List<ListOption>();
        foreach (var role in Enum.GetValues<UserRole>())
        {
            if (role == UserRole.Admin)
                continue;
            if (role == UserRole.Reports && LicensedFor(LicenseFlags.Reporting) == false)
                continue;
            
            RoleOptions.Add(new ListOption()
            {
                Label = Translater.Instant($"Enums.{nameof(UserRole)}.{role}"),
                Value = role
            });
        }
    }

    /// <inheritdoc />
    public override async Task LoadModel()
    {
        if ((Options as ModalEditorOptions)?.Model is User model)
        {
            Model = model;
        }
        else
        {
            var uid = GetModelUid();

            var result = await HttpHelper.Get<User>("/api/user/" + uid);
            if (result.Success == false || result.Data == null)
            {
                Close();
                return;
            }

            Model = result.Data;
        }

        Administrator = Model.Role == UserRole.Admin;
        foreach (ListOption lo in RoleOptions)
        {
            if (lo.Value is UserRole ur && (Model.Role & ur) == ur)
            {
                Roles.Add(ur);
            }
        }
        IsUser = Model.Uid == feService.Profile.Profile.Uid;
        
        StateHasChanged(); // needed to update the title
    }

    
    /// <summary>
    /// Saves the User
    /// </summary>
    public override async Task Save()
    {
        Container.ShowBlocker();
        
        try
        {
            var user = new User()
            {
                Uid = Model.Uid,
                Name = Model.Name.Trim(),
                Email = Model.Email.Trim(),
                Password = Model.Password.Trim()
            };
            if(Administrator)
                user.Role = UserRole.Admin;
            else
            {
                foreach (var role in Roles)
                {
                    if (role is UserRole ur)
                        user.Role |= ur;
                }
            }
            
            var saveResult = await HttpHelper.Post<User>($"/api/user", user);
            if (saveResult.Success == false)
            {
                feService.Notifications.ShowError(saveResult.Body?.EmptyAsNull() ?? 
                                                  Translater.Instant("ErrorMessages.SaveFailed"));
                return;
            }

            TaskCompletionSource.TrySetResult(saveResult.Data);
        }
        finally
        {
             Container.HideBlocker();
        }
        
    }
}