using FileFlows.Client.Components;
using FileFlows.Client.Components.Dialogs;
using FileFlows.Client.Services.Frontend;
using FileFlows.Shared.Models.Configuration;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client.Pages.Config.Configuration.Settings;

public partial class FileServerPage : InputRegister
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

    private bool IsSaving { get; set; }

    /// <summary>
    /// The help URL
    /// </summary>
    private const string HelpUrl = "https://fileflows.com/docs/webconsole/config/file-server";

    private string lblTitle, lblSaving, lblFileServerDescription;

    private FileServerModel Model { get; set; } = new ();
    
    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        Profile = feService.Profile.Profile;
        if (Profile.LicensedFor(LicenseFlags.AutoUpdates) == false)
        {
            NavigationManager.NavigateTo("/", true);
            return;
        }

        lblTitle= Translater.Instant("Pages.Settings.Labels.FileServer");
        lblSaving = Translater.Instant("Labels.Saving");
        lblFileServerDescription = Translater.Instant("Pages.Settings.Fields.FileServer.Description");
        
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
        
        var response = await HttpHelper.Get<FileServerModel>("/api/configuration/file-server");
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
            
            await HttpHelper.Put<string>("/api/configuration/file-server", this.Model);
        }
        finally
        {
            this.IsSaving = false;
            this.Blocker.Hide();
        }
    }
}
