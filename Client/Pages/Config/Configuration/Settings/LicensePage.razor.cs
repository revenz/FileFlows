using System.Text;
using FileFlows.Client.Components;
using FileFlows.Client.Services.Frontend;
using FileFlows.Shared.Models.Configuration;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client.Pages.Config.Configuration.Settings;

public partial class LicensePage : InputRegister
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

    private string lblTitle, lblSaving;
    
    /// <summary>
    /// The help URL
    /// </summary>
    private const string HelpUrl = "https://fileflows.com/docs/webconsole/config/license";

    private LicenseModel Model { get; set; } = new ();

    private string LicenseFlagsString = string.Empty;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        Profile = feService.Profile.Profile;
        lblTitle = Translater.Instant("Pages.Settings.Labels.License");
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
        
        var response = await HttpHelper.Get<LicenseModel>("/api/configuration/license");
        if (response.Success)
        {
            this.Model = response.Data;
            LicenseFlagsString = LicenseFlagsToString(Model.LicenseFlags);
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
            
            await HttpHelper.Put<string>("/api/configuration/license", this.Model);
            
            await Refresh();
        }
        finally
        {
            this.IsSaving = false;
            this.Blocker.Hide();
        }
    }

    private bool IsLicensed => string.IsNullOrEmpty(Model?.LicenseStatus) == false && Model.LicenseStatus != "Unlicensed" && Model.LicenseStatus != "Invalid";


    /// <summary>
    /// Enumerates through the specified enum flags and returns a comma-separated string
    /// containing the names of the enum values that are present in the given flags.
    /// </summary>
    /// <param name="myValue">The enum value with flags set.</param>
    /// <returns>A comma-separated string of the enum values present in the given flags.</returns>
    string LicenseFlagsToString(LicenseFlags myValue)
    {
        List<string> components = new();

        foreach (LicenseFlags enumValue in Enum.GetValues(typeof(LicenseFlags)))
        {
            if (enumValue == LicenseFlags.NotLicensed)
                continue;
            if (myValue.HasFlag(enumValue))
            {
                components.Add(SplitWordsOnCapitalLetters(enumValue.ToString()));
            }
        }

        return string.Join("\n", components.OrderBy(x => x.ToLowerInvariant()));
    }
    
    /// <summary>
    /// Splits a given input string into separate words whenever a capital letter is encountered.
    /// </summary>
    /// <param name="input">The input string to be split.</param>
    /// <returns>A new string with spaces inserted before each capital letter (except the first one).</returns>
    string SplitWordsOnCapitalLetters(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        StringBuilder sb = new StringBuilder();
        foreach (char c in input)
        {
            if (char.IsUpper(c) && sb.Length > 0)
                sb.Append(' ');
            sb.Append(c);
        }

        return sb.ToString();
    }
}
