using FileFlows.Client.Components;
using FileFlows.Client.Components.Dialogs;
using FileFlows.Client.Services.Frontend;
using FileFlows.Client.Shared;
using FileFlows.Shared.Models.Configuration;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client.Pages.Config.Configuration.Settings;

public partial class UpdatesPage : InputRegister
{
    /// <summary>
    /// Gets or sets blocker instance
    /// </summary>
    [CascadingParameter] Blocker Blocker { get; set; }
    
    /// <summary>
    /// Gets or sets the confirm service
    /// </summary>
    [Inject] ConfirmService Confirm { get; set; }
    
    /// <summary>
    /// Gets or sets the javascript runtime used
    /// </summary>
    [Inject] IJSRuntime jsRuntime { get; set; }
    
    /// <summary>
    /// Gets or sets the navigation manager used
    /// </summary>
    [Inject] private NavigationManager NavigationManager { get; set; }
    
    /// <summary>
    /// Gets or sets the Layout
    /// </summary>
    [CascadingParameter] public MainLayout Layout { get; set; }
    
    /// <summary>
    /// Gets or sets the frontend service
    /// </summary>
    [Inject] protected FrontendService feService { get; set; }
    
    /// <summary>
    /// Gets the profile
    /// </summary>
    protected Profile Profile { get; private set; }

    private bool IsSaving { get; set; }

    private string lblSaving, lblCheckNow;
    
    /// <summary>
    /// The help URL
    /// </summary>
    private const string HelpUrl = "https://fileflows.com/docs/webconsole/config/settings/updates";

    private UpdatesModel Model { get; set; } = new ();
    
    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        Profile = feService.Profile.Profile;
        if (Profile.LicensedFor(LicenseFlags.AutoUpdates) == false)
        {
            NavigationManager.NavigateTo("/", true);
            return;
        }

        Layout.SetInfo(Translater.Instant("Pages.Settings.Labels.Updates"), "fas fa-cloud");
        lblSaving = Translater.Instant("Labels.Saving");
        lblCheckNow = Translater.Instant("Pages.Settings.Labels.CheckNow");
        
        Blocker.Show();
        try
        {
            await Refresh();
        }
        finally
        {
            Blocker.Hide();
        }
    }

    /// <summary>
    /// Loads the settings
    /// </summary>
    /// <param name="blocker">if the blocker should be shown or not</param>
    private async Task Refresh(bool blocker = true)
    {
        if(blocker)
            Blocker.Show();
        
        var response = await HttpHelper.Get<UpdatesModel>("/api/configuration/updates");
        if (response.Success)
            this.Model = response.Data;

        this.StateHasChanged();
        
        if(blocker)
            Blocker.Hide();
    }

    /// <summary>
    /// Saves the settings
    /// </summary>
    private async Task Save()
    {
        this.Blocker.Show(lblSaving);
        this.IsSaving = true;
        try
        {
            bool valid = await this.Validate();
            if (valid == false)
                return;
            
            await HttpHelper.Put<string>("/api/configuration/updates", this.Model);
        }
        finally
        {
            this.IsSaving = false;
            this.Blocker.Hide();
        }
    }
    
    /// <summary>
    /// Check for an update
    /// </summary>
    private async Task CheckForUpdateNow()
    {
        Blocker.Show();
        var available = await HttpHelper.Post<bool>("/api/configuration/updates/check-for-update-now");
        Blocker.Hide();
        if (available.Success == false)
        {
            feService.Notifications.ShowError("Pages.Settings.Messages.Update.Failed");
            return;
        }

        if (available.Data == false)
        {
            feService.Notifications.ShowInfo("Pages.Settings.Messages.Update.NotAvailable");
            return;
        }

        if (await Confirm.Show("Pages.Settings.Messages.Update.Title",
                "Pages.Settings.Messages.Update.Message") == false)
            return;
        await HttpHelper.Post("/api/configuration/updates/upgrade-now");
        feService.Notifications.ShowInfo("Pages.Settings.Messages.Update.Downloading");

    }
}
