using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Inputs;

/// <summary>
/// Input Language 
/// </summary>
public partial class InputLanguage : Input<List<string>>
{
    /// <summary>
    /// Gets or sets if the original language option should be included
    /// </summary>
    [Parameter] public bool OriginalLanguage { get; set; }
    
    private object[] _MutliselectValue;
    
    /// <summary>
    /// Gets or stes the value bound to the MultiSelect
    /// </summary>
    private object[] MutliselectValue
    {
        get => _MutliselectValue;
        set
        {
            _MutliselectValue = value;
            Value = value.Where(x => x != null).Select(x => x.ToString()).Where(x => x != null).ToList()!;
        }
    }

    private List<ListOption> LanguageOptions = [];
    

    /// <summary>
    /// Gets or sets the profile service
    /// </summary>
    [Inject] protected ProfileService ProfileService { get; set; }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        var profile = await ProfileService.Get();
        LanguageOptions = LanguageHelper.Languages.DistinctBy(x => x.Iso2).Select(x =>
        {
            var name = profile.UseFrench ? x.French :
                profile.UseGerman ? x.German :
                x.English;
            name = name?.EmptyAsNull() ?? x.English;
            return new ListOption()
            {
                Label = name, Value = x.Iso2
            };
        }).OrderBy(x => x.Label.ToLowerInvariant()).ToList();
        
        if(OriginalLanguage)
            LanguageOptions.Insert(0, new ()
            {
                Label = Translater.Instant("Labels.OriginalLanguage"),
                Value = "orig"
            });
    }
}