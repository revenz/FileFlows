using System.Text;
using FileFlows.Client.Components;
using FileFlows.Client.Components.Dialogs;
using FileFlows.Client.Models;
using FileFlows.Client.Services.Frontend;
using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client.Pages.Config.Configuration.Settings;

public partial class SecurityPage : InputRegister
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

    private string lblTitle, lblSave, lblSaving, lblHelp, mdSecurityDescription;


    private SettingsUiModel Model { get; set; } = new ();

    // indicates if the page has rendered or not
    private DateTime firstRenderedAt = DateTime.MaxValue;

    /// <summary>
    /// Required validator
    /// </summary>
    private readonly List<Validator> RequiredValidator = new()
    {
        new Required()
    };


    /// <summary>
    /// The security options
    /// </summary>
    private List<ListOption> SecurityOptions;
    
    /// <summary>
    /// Gets or sets the security
    /// </summary>
    private object Security
    {
        get => Model.Security;
        set
        {
            if (value is SecurityMode mode)
            {
                Model.Security = mode;
            }
        }
    }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        Profile = feService.Profile.Profile;
        lblTitle = Translater.Instant("Pages.Settings.Fields.Security.Title");
        lblSave = Translater.Instant("Labels.Save");
        lblSaving = Translater.Instant("Labels.Saving");
        lblHelp = Translater.Instant("Labels.Help");
        mdSecurityDescription = RenderMarkdown("Pages.Settings.Fields.Security.Description");
        
        InitSecurityModes();
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
    /// Initiate the security modes avaiable
    /// </summary>
    private void InitSecurityModes()
    {
        SecurityOptions = new();
        if (Profile.LicensedFor(LicenseFlags.UserSecurity) == false)
        {
            Model.Security = SecurityMode.Off;
            return;
        }

        SecurityOptions = new()
        {
            new() { Label = $"Enums.{nameof(SecurityMode)}.{nameof(SecurityMode.Off)}", Value = SecurityMode.Off },
            new() { Label = $"Enums.{nameof(SecurityMode)}.{nameof(SecurityMode.Local)}", Value = SecurityMode.Local }
        };
        if (Profile.LicensedFor(LicenseFlags.SingleSignOn) == false)
        {
            if(Model.Security == SecurityMode.OpenIdConnect)
                Model.Security = SecurityMode.Off;
            return;
        }

        SecurityOptions.Add(
            new()
            {
                Label = $"Enums.{nameof(SecurityMode)}.{nameof(SecurityMode.OpenIdConnect)}",
                Value = SecurityMode.OpenIdConnect
            });
    }

    /// <summary>
    /// Renders markdown to HTML
    /// </summary>
    /// <param name="text">the text to render</param>
    /// <returns>the HTML</returns>
    private string RenderMarkdown(string text)
    {
        text = Translater.TranslateIfNeeded(text);
        List<string> lines = new();
        foreach (var t in text.Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(t))
            {
                lines.Add(string.Empty);
                continue;
            }

            string html = Markdig.Markdown.ToHtml(t).Trim();
            if (html.StartsWith("<p>") && html.EndsWith("</p>"))
                html = html[3..^4].Trim();
            html.Replace("<a ", "<a onclick=\"ff.openLink(event);return false;\" ");
            lines.Add(html);
        }

        return string.Join("\n", lines);

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
        
        var response = await HttpHelper.Get<SettingsUiModel>("/api/settings/ui-settings");
        if (response.Success)
        {
            this.Model = response.Data;

            if (Model != null && string.IsNullOrWhiteSpace(Model.AccessToken))
                Model.AccessToken = Guid.NewGuid().ToString("N");
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
            
            await HttpHelper.Put<string>("/api/settings/ui-settings", this.Model);
            if (Model.Security == SecurityMode.Off)
                await feService.Profile.ClearAccessToken();

            InitSecurityModes();
        }
        finally
        {
            this.IsSaving = false;
            this.Blocker.Hide();
        }
    }

    /// <summary>
    /// Opens the help page
    /// </summary>
    private void OpenHelp()
        => _ = App.Instance.OpenHelp("https://fileflows.com/docs/webconsole/config/security");
}
