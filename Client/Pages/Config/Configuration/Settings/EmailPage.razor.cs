using FileFlows.Client.Components;
using FileFlows.Client.Services.Frontend;
using FileFlows.Client.Shared;
using FileFlows.Plugin;
using FileFlows.Shared.Models.Configuration;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client.Pages.Config.Configuration.Settings;

/// <summary>
/// Email Page
/// </summary>
public partial class EmailPage : InputRegister
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

    private string lblSaving, lblEmailDescription;
    /// <summary>
    /// The help URL
    /// </summary>
    private const string HelpUrl = "https://fileflows.com/docs/webconsole/config/settings/email";

    private EmailModel Model { get; set; } = new ();
    
    /// <summary>
    /// the SMTP security types
    /// </summary>
    private readonly List<ListOption> SmtpSecurityTypes = new()
    {
        new() { Label = "None", Value = EmailSecurity.None },
        new() { Label = "Auto", Value = EmailSecurity.Auto },
        new() { Label = "TLS", Value = EmailSecurity.TLS },
        new() { Label = "SSL", Value = EmailSecurity.SSL },
    };

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        Profile = feService.Profile.Profile;
        Layout.SetInfo(Translater.Instant("Pages.Settings.Labels.Email"), "fas fa-envelope", noPadding: true);
        lblSaving = Translater.Instant("Labels.Saving");
        lblEmailDescription = Translater.Instant("Pages.Settings.Labels.EmailDescription");
        
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
        
        var response = await HttpHelper.Get<EmailModel>("/api/configuration/email");
        if (response.Success)
        {
            this.Model = response.Data;
            if (this.Model.SmtpPort is < 1 or > 65535)
                this.Model.SmtpPort = 25;
        }

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
            
            await HttpHelper.Put<string>("/api/configuration/email", this.Model);
        }
        finally
        {
            this.IsSaving = false;
            this.Blocker.Hide();
        }
    }
    
    /// <summary>
    /// Gets or sets the type of email security to use
    /// </summary>
    private object SmtpSecurity
    {
        get => Model.SmtpSecurity;
        set
        {
            if (value is EmailSecurity security)
                Model.SmtpSecurity = security;
        }
    }
}
