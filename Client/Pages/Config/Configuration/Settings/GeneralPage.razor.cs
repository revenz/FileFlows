using FileFlows.Client.Models;
using FileFlows.Client.Components.Dialogs;
using Microsoft.AspNetCore.Components;
using FileFlows.Client.Components;
using FileFlows.Client.Services.Frontend;
using FileFlows.Client.Shared;
using FileFlows.Shared.Models.Configuration;
using Microsoft.JSInterop;

namespace FileFlows.Client.Pages.Config.Configuration.Settings;

/// <summary>
/// Page for system settings
/// </summary>
public partial class GeneralPage : InputRegister
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

    private string lblSaving, lblTestingDatabase;

    private GeneralModel Model { get; set; } = new ();
    
    /// <summary>
    /// The help URL
    /// </summary>
    private const string HelpUrl = "https://fileflows.com/docs/webconsole/config/settings/general";

    /// <summary>
    /// The language options
    /// </summary>
    private List<IconListOption> LanguageOptions = new ();
    // indicates if the page has rendered or not
    private DateTime firstRenderedAt = DateTime.MaxValue;

    /// <summary>
    /// Gets or sets the language
    /// </summary>
    private object Language
    {
        get => Model.Language;
        set
        {
            if (value is string lang)
            {
                Model.Language = lang;
            }
        }
    }
    private string initialLannguage;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        Profile = feService.Profile.Profile;
        Layout.SetInfo(Translater.Instant("Pages.Settings.Labels.General"), "fas fa-cogs");
        lblSaving = Translater.Instant("Labels.Saving");
        lblTestingDatabase = Translater.Instant("Pages.Settings.Messages.Database.TestingDatabase");
        
        LanguageOptions = Profile.LanguageOptions?.Select(x =>
            new IconListOption()
            {
                Label = x.Label,
                Value = x.Value,
                IconUrl = $"/icons/flags/{x.Value}.svg"
            }
        ).ToList();
        
        Blocker.Show("Loading Settings");
        try
        {
            await Refresh();
            initialLannguage = Model.Language;
        }
        finally
        {
            Blocker.Hide();
        }
    }

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
            firstRenderedAt = DateTime.UtcNow;
        base.OnAfterRender(firstRender);
    }

    /// <summary>
    /// Loads the settings
    /// </summary>
    /// <param name="blocker">if the blocker should be shown or not</param>
    private async Task Refresh(bool blocker = true)
    {
        if(blocker)
            Blocker.Show();
        
        var response = await HttpHelper.Get<GeneralModel>("/api/configuration/general");
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
            
            await HttpHelper.Put<string>("/api/configuration/general", this.Model);
            
            if (initialLannguage != Model.Language)
            {
                // need to do a full page reload
                NavigationManager.NavigateTo(NavigationManager.Uri, forceLoad: true);
            }
        }
        finally
        {
            this.IsSaving = false;
            this.Blocker.Hide();
        }
    }


    /// <summary>
    /// When the user changes the telemetry value
    /// </summary>
    /// <param name="disabled">if the switch is disabled</param>
    private async Task OnTelemetryChange(bool disabled)
    {
        if (firstRenderedAt < DateTime.UtcNow.AddSeconds(-1) && disabled)
        {
            if (await Confirm.Show("Pages.Settings.Messages.DisableTelemetryConfirm.Title",
                    "Pages.Settings.Messages.DisableTelemetryConfirm.Message",
                    false) == false)
            {
                Model.DisableTelemetry = false;
            }
        }
    }
}
