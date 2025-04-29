using FileFlows.Client.Services.Frontend;
using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Inputs;

/// <summary>
/// Input Language 
/// </summary>
public partial class InputLanguage : Input<string>
{
    /// <summary>
    /// Gets or sets if the original language option should be included
    /// </summary>
    [Parameter] public bool OriginalLanguage { get; set; }
    
    private List<KeyValuePair<string, string>> LanguageOptions = [];
    

    /// <summary>
    /// Gets or sets the profile service
    /// </summary>
    [Inject] protected FrontendService feService { get; set; }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        var profile = feService.Profile.Profile;
        LanguageOptions = LanguageHelper.Languages.DistinctBy(x => x.Iso2).Select(x =>
        {
            var name = profile.UseFrench ? x.French :
                profile.UseGerman ? x.German :
                x.English;
            name = name?.EmptyAsNull() ?? x.English;
            return new KeyValuePair<string, string>(name, x.Iso2);
        }).OrderBy(x => x.Key.ToLowerInvariant()).ToList();

        if (OriginalLanguage)
            LanguageOptions.Insert(0, new(
                Translater.Instant("Labels.OriginalLanguage"),
                Value = "orig"
            ));
    }
}