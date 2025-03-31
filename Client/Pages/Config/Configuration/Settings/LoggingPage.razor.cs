using FileFlows.Client.Components;
using FileFlows.Client.Services.Frontend;
using FileFlows.Shared.Models.Configuration;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client.Pages.Config.Configuration.Settings;

public partial class LoggingPage : InputRegister
{
    /// <summary>
    /// Gets or sets blocker instance
    /// </summary>
    [CascadingParameter] Blocker Blocker { get; set; }
    
    /// <summary>
    /// Gets or sets the javascript runtime used
    /// </summary>
    [Inject] IJSRuntime jsRuntime { get; set; }
    
    /// <summary>
    /// Gets or sets the navigation manager used
    /// </summary>
    [Inject] private NavigationManager NavigationManager { get; set; }
    

    /// <summary>
    /// Gets or sets the frontend service
    /// </summary>
    [Inject] protected FrontendService feService { get; set; }
    
    /// <summary>
    /// Gets the profile
    /// </summary>
    protected Profile Profile { get; private set; }
    
    /// <summary>
    /// The help URL
    /// </summary>
    private const string HelpUrl = "https://fileflows.com/docs/webconsole/config/settings/logging";

    private bool IsSaving { get; set; }

    private string lblTitle, lblSaving;

    private LoggingModel Model { get; set; } = new ();

    private bool IsLicensed;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        Profile = feService.Profile.Profile;
        IsLicensed = Profile.LicenseLevel != LicenseLevel.Free; 
        lblTitle= Translater.Instant("Pages.Settings.Labels.Logging");
        lblSaving = Translater.Instant("Labels.Saving");
        
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
        
        var response = await HttpHelper.Get<LoggingModel>("/api/configuration/logging");
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
            
            await HttpHelper.Put<string>("/api/configuration/logging", this.Model);
        }
        finally
        {
            this.IsSaving = false;
            this.Blocker.Hide();
        }
    }
}
