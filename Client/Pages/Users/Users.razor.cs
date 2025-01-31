using FileFlows.Client.Components;
using FileFlows.Client.Components.Inputs;
using FileFlows.Plugin;

namespace FileFlows.Client.Pages;

/// <summary>
/// Page for revisions 
/// </summary>
public partial class Users: ListPage<Guid, User>
{
    /// <inheritdoc />
    public override string ApiUrl => "/api/user";

    /// <inheritdoc />
    public override string FetchUrl => $"{ApiUrl}";

    private string lblLastLoggedIn, lblNever, lblAdministrator, lblAddress;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
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
        await Edit(new User()
        {
        });
    }
    
    public override async Task<bool> Edit(User item)
    {
        Blocker.Show();

        var isUser = item.Uid == Profile.Uid;
        
        List<IFlowField> fields = new ();

        var model = new UserEditModel()
        {
            Name = item.Name,
            IsAdmin = item.Role == UserRole.Admin,
            Password = item.Password,
            Uid = item.Uid,
            Email = item.Email,
            Roles = new ()
        };

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
            InputType = FormInputType.Text,
            Name = nameof(item.Email),
            Validators = new List<Validator> {
                new Required()
            }
        });

        if (Profile.Security != SecurityMode.OpenIdConnect && isUser == false)
        {
            fields.Add(new ElementField
            {
                InputType = FormInputType.Password,
                Name = nameof(item.Password),
                Validators = new List<Validator> {
                    new Required()
                }
            });
        }

        var eleIsAdmin = new ElementField
        {
            InputType = FormInputType.Switch,
            Name = nameof(model.IsAdmin),
            Parameters = new()
            {
                { nameof(InputSwitch.ReadOnly), isUser }
            }
        };
        fields.Add(eleIsAdmin);

        var roleOptions = new List<ListOption>();
        foreach (var role in Enum.GetValues<UserRole>())
        {
            if (role == UserRole.Admin)
                continue;
            if (role == UserRole.Reports && Profile.LicensedFor(LicenseFlags.Reporting) == false)
                continue;
            
            roleOptions.Add(new ListOption()
            {
                Label = Translater.Instant($"Enums.{nameof(UserRole)}.{role}"),
                Value = role
            });
            if(model.IsAdmin)
                model.Roles.Add(role);
            else if((item.Role & role) == role)
                model.Roles.Add(role);
        }
        
        fields.Add(new ElementField()
        {
            InputType = FormInputType.Checklist,
            Name = nameof(model.Roles),
            Parameters = new()
            {
                { nameof(InputChecklist.Options), roleOptions }
            },
            Conditions = new List<Condition>
            {
                new (eleIsAdmin, model.IsAdmin, value: false)
            }
        });
        
        Blocker.Hide();
        await Editor.Open(new()
        {
            TypeName = "Pages.Users", Title = "Pages.Users.Single", Fields = fields, Model = model,
            SaveCallback = Save
        });
        
        return false;
    }
    
    
    async Task<bool> Save(ExpandoObject model)
    {
        Blocker.Show();
        this.StateHasChanged();

        try
        {
            var user = new User();
            var dict = model as IDictionary<string, object>;
            user.Uid = (Guid)dict[nameof(UserEditModel.Uid)];
            user.Name = dict[nameof(UserEditModel.Name)].ToString() ?? string.Empty;
            user.Email = dict[nameof(UserEditModel.Email)].ToString() ?? string.Empty;
            if (dict.TryGetValue(nameof(UserEditModel.Password), out var oPassword) && oPassword is string password)
                user.Password = password;
            var isAdmin = dict[nameof(UserEditModel.IsAdmin)] as bool? == true;
            if (isAdmin)
            {
                user.Role = UserRole.Admin;
            }
            else
            {
                user.Role = (UserRole)0;
                var roles = dict[nameof(UserEditModel.Roles)] as List<object>;
                if (roles?.Any() == true)
                {
                    foreach (var role in roles)
                    {
                        if (role is UserRole r)
                            user.Role |= r;
                    }
                }
            }
            
            var saveResult = await HttpHelper.Post<User>($"{ApiUrl}", user);
            if (saveResult.Success == false)
            {
                Toast.ShowEditorError( saveResult.Body?.EmptyAsNull() ?? Translater.Instant("ErrorMessages.SaveFailed"));
                return false;
            }

            if ((Profile.ConfigurationStatus & ConfigurationStatus.Users) != ConfigurationStatus.Users)
                Profile.ConfigurationStatus |= ConfigurationStatus.Users;

            int index = this.Data.FindIndex(x => x.Uid == saveResult.Data.Uid);
            if (index < 0)
                this.Data.Add(saveResult.Data);
            else
                this.Data[index] = saveResult.Data;
            await this.Load(saveResult.Data.Uid);

            return true;
        }
        finally
        {
            Blocker.Hide();
            this.StateHasChanged();
        }
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

    /// <summary>
    /// User edit model
    /// </summary>
    class UserEditModel : User
    {
        /// <summary>
        /// Gets or sets if the user is an admin
        /// </summary>
        public bool IsAdmin { get; set; }
        
        /// <summary>
        /// Gets or sets the user roles this user has
        /// </summary>
        public List<object> Roles { get; set; }
    }
}