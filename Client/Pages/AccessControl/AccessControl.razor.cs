using System.Net;
using FileFlows.Client.Components;
using FileFlows.Client.Components.Common;
using FileFlows.Plugin;

namespace FileFlows.Client.Pages;

/// <summary>
/// Page for access control 
/// </summary>
public partial class AccessControl: ListPage<Guid, AccessControlEntry>
{
    /// <inheritdoc />
    public override string ApiUrl => "/api/acl";

    /// <inheritdoc />
    public override string FetchUrl => $"{ApiUrl}";
    /// <summary>
    /// The skybox instance
    /// </summary>
    private FlowSkyBox<AccessControlType> Skybox;

    private string lblStart, lblEnd, lblAllow;
    
    private List<AccessControlEntry> DataConsole = new();
    private List<AccessControlEntry> DataRemote = new();

    /// <summary>
    /// The selected access control type
    /// </summary>
    private AccessControlType SelectedType = AccessControlType.Console;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        lblStart = Translater.Instant("Pages.AccessControl.Columns.Start");
        lblEnd = Translater.Instant("Pages.AccessControl.Columns.End");
        lblAllow = Translater.Instant("Pages.AccessControl.Columns.Allow");
    }

    /// <summary>
    /// Gets if they are licensed for this page
    /// </summary>
    /// <returns>if they are licensed for this page</returns>
    protected override bool Licensed()
        => Profile.LicensedFor(LicenseFlags.AccessControl); 

    /// <summary>
    /// Adds a new user
    /// </summary>
    private async Task Add()
    {
        await Edit(new AccessControlEntry()
        {
            Allow = true,
            Type = SelectedType
        });
    }
    
    public override async Task<bool> Edit(AccessControlEntry item)
    {
        Blocker.Show();
        
        List<IFlowField> fields = new ();

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
            InputType = FormInputType.TextArea,
            Name = nameof(item.Description)
        });
        
        // Regex pattern for matching IPv4 addresses
        string ipv4 = @"\b((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\b";

        // Regex pattern for matching IPv6 addresses
        string ipv6 = @"\b(?:[0-9a-fA-F]{0,4}::?){1,7}[0-9a-fA-F]{0,4}\b";

        // Combined regex pattern for IPv4 or IPv6 addresses
        string ipPattern = @"^(" + ipv4 + @"|" + ipv6 + @")?$";

        fields.Add(new ElementField
        {
            InputType = FormInputType.Text,
            Name = nameof(item.Start),
            Validators = new List<Validator> {
                new Required(),
                new Pattern() { Expression = ipPattern }
            }
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.Text,
            Name = nameof(item.End),
            Validators = new List<Validator> {
                new Pattern() { Expression = ipPattern }
            }
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.Switch,
            Name = nameof(item.Allow)
        });
        Blocker.Hide();
        await Editor.Open(new()
        {
            TypeName = "Pages.AccessControl", Title = "Pages.AccessControl.Single", Fields = fields, Model = item,
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
            var dict = model as IDictionary<string, object>;
            var start = dict[nameof(AccessControlEntry.Start)] as string;
            if (string.IsNullOrWhiteSpace(start))
                return false;

            if (IPAddress.TryParse(start, out IPAddress? ipStart) == false)
            {
                Toast.ShowEditorError(Translater.Instant("Pages.AccessControl.Messages.InvalidIPAddress", new { address = start }));
                return false;
            }

            var end = dict[nameof(AccessControlEntry.End)] as string;
            if (string.IsNullOrWhiteSpace(end) == false)
            {
                if (IPAddress.TryParse(end, out IPAddress? ipEnd) == false)
                {
                    Toast.ShowEditorError(Translater.Instant("Pages.AccessControl.Messages.InvalidIPAddress", new { address = end }));
                    return false;
                }
                
                if(ipStart.AddressFamily != ipEnd.AddressFamily)
                {
                    Toast.ShowEditorError(Translater.Instant("Pages.AccessControl.Messages.FamilyMismatch"));
                    return false;
                }

                if (IPHelper.IsGreaterThan(ipStart, ipEnd) == false)
                {
                    Toast.ShowEditorError(Translater.Instant("Pages.AccessControl.Messages.EndMustBeGreaterThan"));
                    return false;
                }
            }
            
            var saveResult = await HttpHelper.Post<User>($"{ApiUrl}", model);
            if (saveResult.Success == false)
            {
                Toast.ShowEditorError( saveResult.Body?.EmptyAsNull() ?? Translater.Instant("ErrorMessages.SaveFailed"));
                return false;
            }

            if ((Profile.ConfigurationStatus & ConfigurationStatus.Users) != ConfigurationStatus.Users)
                Profile.ConfigurationStatus |= ConfigurationStatus.Users;

            int index = this.Data.FindIndex(x => x.Uid == saveResult.Data.Uid);
            // if (index < 0)
            //     this.Data.Add(saveResult.Data);
            // else
            //     this.Data[index] = saveResult.Data;
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
    /// Moves the items down
    /// </summary>
    /// <returns>a task to await</returns>
    Task MoveUp() => Move(true);

    /// <summary>
    /// Moves the items down
    /// </summary>
    /// <returns>a task to await</returns>
    Task MoveDown() => Move(false);

    /// <summary>
    /// Moves the items up or down
    /// </summary>
    /// <param name="up">if moving up</param>
    async Task Move(bool up)
    {
        var uids = Table.GetSelected()?.Select(x => x.Uid)?.ToArray() ?? new Guid[] { };
        if (uids.Length == 0)
            return; // nothing to move

        Blocker.Show();
        this.StateHasChanged();

        try
        {
            var result = await HttpHelper.Post($"{ApiUrl}/move?type={SelectedType}&up={up}", new ReferenceModel<Guid> { Uids = uids });
            if (result.Success == false)
            {
                if(Translater.NeedsTranslating(result.Body))
                    Toast.ShowError( Translater.Instant(result.Body));
                else
                    Toast.ShowError( Translater.Instant("Pages.AccessControl.Messages.MoveFailed"));
                return;
            }

            await Refresh();
            var selected = Data.Where(x => uids.Contains(x.Uid)).ToList();
            Table.SetSelected(selected);
        }
        finally
        {
            Blocker.Hide();
            this.StateHasChanged();
        }
    }
    
    
    protected override Task PostDelete() => Refresh();

    public override Task PostLoad()
    {
        UpdateTypeData();
        return Task.CompletedTask;
    }
    
    private void UpdateTypeData()
    {
        this.DataConsole = this.Data.Where(x => x.Type == AccessControlType.Console).ToList();
        this.DataRemote = this.Data.Where(x => x.Type == AccessControlType.RemoteServices).ToList();
        this.Skybox.SetItems(new List<FlowSkyBoxItem<AccessControlType>>()
        {
            new ()
            {
                Name = Translater.Instant("Pages.AccessControl.Labels.Console"),
                Icon = "fas fa-globe",
                Count = this.DataConsole.Count,
                Value = AccessControlType.Console  
            },
            new ()
            {
                Name = Translater.Instant("Pages.AccessControl.Labels.Remote"),
                Icon = "fas fa-network-wired",
                Count = this.DataRemote.Count,
                Value = AccessControlType.RemoteServices
            }
        }.Where(x => x != null).ToList(), this.SelectedType);
    }

    private void SetSelected(FlowSkyBoxItem<AccessControlType> item)
    {
        SelectedType = item.Value;
        // need to tell table to update so the "Default" column is shown correctly
        Table.TriggerStateHasChanged();
        this.StateHasChanged();
    }
}