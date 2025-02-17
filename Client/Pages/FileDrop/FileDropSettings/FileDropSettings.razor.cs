using FileFlows.Client.Components;
using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Pages.FileDrop;

/// <summary>
/// File Drop settings
/// </summary>
public partial class FileDropSettings
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
    /// Gets or sets the profile service
    /// </summary>
    [Inject] protected ProfileService ProfileService { get; set; }
    
    /// <summary>
    /// Gets the profile
    /// </summary>
    protected Profile Profile { get; private set; }
    private bool IsSaving { get; set; }

    private string lblTitle, lblSave, lblSaving, lblHelp;
    private string FileFlowsCallbackUrl;

    private FileFlows.Shared.Models.FileDropSettings Model { get; set; } = new ();
    private List<ListOption> openInOptions;
    
    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        Profile = await ProfileService.Get();
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
        
        lblTitle = Translater.Instant("Pages.FileDrop.Settings.Title");
        lblSave = Translater.Instant("Labels.Save");
        lblSaving = Translater.Instant("Labels.Saving");
        lblHelp = Translater.Instant("Labels.Help");
        Blocker.Show("Loading Settings");
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
        
        var response = await HttpHelper.Get<FileFlows.Shared.Models.FileDropSettings>("/api/file-drop/settings");
        if (response.Success)
        {
            this.Model = response.Data;
        }
        this.StateHasChanged();
        if(blocker)
            Blocker.Hide();
    }

    /// <summary>
    /// Opens the help page
    /// </summary>
    private void OpenHelp()
        => _ = App.Instance.OpenHelp("https://fileflows.com/docs/webconsole/file-drop/settings");
    
    private async Task Save()
    {
        this.Blocker.Show(lblSaving);
        this.IsSaving = true;
        try
        {
            bool valid = await this.Validate();
            if (valid == false)
                return;
            
            await HttpHelper.Put<string>("/api/file-drop/settings", this.Model);

            await ProfileService.Refresh();
        }
        finally
        {
            this.IsSaving = false;
            this.Blocker.Hide();
        }
    }

    
    /// <summary>
    /// Gets or sets Open Url In Popup
    /// </summary>
    private object BoundOpenUrlInPopup
    {
        get => Model.OpenUrlInPopup;
        set
        {
            if (value is bool v)
                Model.OpenUrlInPopup = v;
        }
    }
}