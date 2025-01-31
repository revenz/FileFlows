using System.ComponentModel.DataAnnotations;
using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Inputs;

/// <summary>
/// Input Languages 
/// </summary>
public partial class InputLanguages : Input<List<string>>
{
    /// <summary>
    /// Gets or sets if the original language option should be included
    /// </summary>
    [Parameter] public bool OriginalLanguage { get; set; }


    /// <summary>
    /// Gets or stes the value bound to the MultiSelect
    /// </summary>
    private object[] MutliselectValue { get; set; } = [];

    private List<ListOption> LanguageOptions = [];

    /// <summary>
    /// Called when the multiselect value chnages
    /// </summary>
    /// <param name="value">the new value</param>
    private void UpdateMultiselectValue(object[] value)
    {
        Value = value.Where(x => x != null).Select(x => x.ToString()).Where(x => x != null).ToList()!;
        _ = Validate();
    }
    

    /// <summary>
    /// Gets or sets the profile service
    /// </summary>
    [Inject] protected ProfileService ProfileService { get; set; }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        Logger.Instance.ILog("Value: " , this.Value);
        MutliselectValue = this.Value?.Select(object (x) => x)?.ToArray() ?? [];
        Logger.Instance.ILog("MutliselectValue: " , this.Value);
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

    public override Task<bool> Validate()
    {
        bool required = this.Validators.Any(x => x is Required);
        return base.Validate();

    }
}