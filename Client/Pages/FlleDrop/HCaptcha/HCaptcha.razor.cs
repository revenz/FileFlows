using FileFlows.Client.Components;
using FileFlows.Client.Services.Frontend;
using FileFlows.Client.Shared;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Pages.FileDrop;

/// <summary>
/// hCaptcha onfig page
/// </summary>
public partial class HCaptcha 
{
    /// <summary>
    /// Gets or sets the navigation manager used
    /// </summary>
    [Inject] private NavigationManager NavigationManager { get; set; }
    
    /// <summary>
    /// Gets or sets blocker instance
    /// </summary>
    [CascadingParameter] Blocker Blocker { get; set; }
    /// <summary>
    /// Gets or sets the Layout
    /// </summary>
    [CascadingParameter] public MainLayout Layout { get; set; }

    /// <summary>
    /// Gets or sets the frotnend service
    /// </summary>
    [Inject] protected FrontendService feService { get; set; }
    
    /// <summary>
    /// Gets the profile
    /// </summary>
    protected Profile Profile { get; private set; }
    private bool IsSaving { get; set; }

    private string lblSaving;
    private bool initDone = false;

    /// <summary>
    /// The help URL
    /// </summary>
    private const string HelpUrl = "https://fileflows.com/docs/file-drop/config/hcaptcha";

    private FileFlows.Shared.Models.FileDropSettings Model { get; set; } = new ();
    
    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        Profile = feService.Profile.Profile;
        if (Profile.LicensedFor(LicenseFlags.FileDrop) == false)
        {
            NavigationManager.NavigateTo("/");
            return;
        }

        Layout.SetInfo("hCaptcha", "fas fa-key", noPadding: true);
        lblSaving = Translater.Instant("Labels.Saving");
        Blocker.Show("Loading Settings");
        try
        {
            await Refresh();
        }
        finally
        {
            initDone = true;
            StateHasChanged();
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
        
        var response = await HttpHelper.Get<FileFlows.Shared.Models.FileDropSettings>("/api/file-drop/hcaptcha");
        if (response.Success)
        {
            this.Model = response.Data;
        }
        this.StateHasChanged();
        if(blocker)
            Blocker.Hide();
    }
    
    /// <summary>
    /// Saves the FileDrop settings
    /// </summary>
    private async Task Save()
    {
        Blocker.Show(lblSaving);
        IsSaving = true;
        try
        {
            await HttpHelper.Put<string>("/api/file-drop/hcaptcha", Model);
        }
        finally
        {
            IsSaving = false;
            Blocker.Hide();
        }
    }
}