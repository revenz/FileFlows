using FileFlows.Client.Components;
using FileFlows.Client.Services.Frontend;
using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Pages.FileDrop;

/// <summary>
/// Tokens config page
/// </summary>
public partial class Tokens 
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
    /// Gets or sets the frotnend service
    /// </summary>
    [Inject] protected FrontendService feService { get; set; }
    
    /// <summary>
    /// Gets the profile
    /// </summary>
    protected Profile Profile { get; private set; }
    private bool IsSaving { get; set; }

    private string lblTitle, lblSaving;
    private bool initDone = false;
    private string FileFlowsCallbackUrl;


    /// <summary>
    /// The help URL
    /// </summary>
    private const string HelpUrl = "https://fileflows.com/docs/file-drop/config/tokens";
    private FileFlows.Shared.Models.FileDropSettings Model { get; set; } = new ();
    private List<ListOption> openInOptions;
    
    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        Profile = feService.Profile.Profile;
        if (Profile.LicensedFor(LicenseFlags.FileDrop) == false)
        {
            NavigationManager.NavigateTo("/");
            return;
        }

        openInOptions =
        [
            new () {Label = "A New Window", Value = false},
            new () {Label = "A Popup Dialog", Value = true}
        ];

        FileFlowsCallbackUrl = NavigationManager.BaseUri + "api/file-drop/user/{uuid}/";

        lblTitle = "Tokens";
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
        
        var response = await HttpHelper.Get<FileFlows.Shared.Models.FileDropSettings>("/api/file-drop/tokens");
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
            await HttpHelper.Put<string>("/api/file-drop/tokens", Model);
        }
        finally
        {
            IsSaving = false;
            Blocker.Hide();
        }
    }
    
    /// <summary>
    /// Gets or sets Open Url In Popup
    /// </summary>
    private object BoundTokenPurchaseInPopup
    {
        get => Model.TokenPurchaseInPopup;
        set
        {
            if (value is bool v)
                Model.TokenPurchaseInPopup = v;
        }
    }
}