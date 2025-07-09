using FileFlows.Client.Services.Frontend;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Common;

/// <summary>
/// Flow radio group
/// </summary>
/// <typeparam name="TItem">The type of value bound to the Value</typeparam>
public partial class FlowRadioGroup<TItem> : ComponentBase
{
    /// <summary>
    /// Gets or sets the frontend service
    /// </summary>
    [Inject]
    protected FrontendService feService { get; set; }
    
    /// <summary>
    /// Gets or sets the selected item
    /// </summary>
    [Parameter]
    public TItem Value { get; set; }

    /// <summary>
    /// Event called when the value changes
    /// </summary>
    [Parameter] 
    public EventCallback<TItem> ValueChanged { get; set; }

    /// <summary>
    /// Gets or sets the label prefix to apply to item labels
    /// </summary>
    [Parameter] public string LabelPrefix { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the child content
    /// </summary>
    [Parameter] public RenderFragment ChildContent { get; set; }
    
    /// <summary>
    /// Gets or sets the wizard contaning this
    /// </summary>
    [CascadingParameter] public FlowWizard FlowWizard { get; set; }
    

    /// <summary>
    /// Represents a collection of tabs.
    /// </summary>
    private List<FlowRadioGroupItem<TItem>> Items = new();

    /// <summary>
    /// Gets if the user is licensed for a level
    /// </summary>
    /// <param name="level">the level</param>
    /// <returns>true if licensed, otherwise false</returns>
    protected bool LicensedFor(LicenseLevel level)
        => feService.Profile.Profile.LicenseLevel >= level;
    
    /// <summary>
    /// Adds an item for rendering
    /// </summary>
    public void AddItem(FlowRadioGroupItem<TItem> item)
    {
        if (Items.Contains(item) == false)
        {
            Items.Add(item);
            StateHasChanged();
        }
    }

    /// <summary>
    /// Selects a new value
    /// </summary>
    /// <param name="item">the item</param>
    private void Select(FlowRadioGroupItem<TItem> item)
    {
        if (LicensedFor(item.LicenseLevel) == false)
            return;
        
        var itemValue = item.Value;
        Value = itemValue;
        ValueChanged.InvokeAsync(itemValue);
    }

    /// <summary>
    /// Dbl Click a new value
    /// </summary>
    /// <param name="item">the item</param>
    private void DblClick(FlowRadioGroupItem<TItem> item)
    {
        if (LicensedFor(item.LicenseLevel) == false)
            return;
        
        var itemValue = item.Value;
        Value = itemValue;
        ValueChanged.InvokeAsync(itemValue);
        if(FlowWizard is { NonWizard: true })
            FlowWizard.Finish();
    }

    private Dictionary<LicenseLevel, string> LicenseTranslations = [];

    /// <summary>
    /// Gets the translated message for the required license message
    /// </summary>
    /// <param name="level">the license level</param>
    /// <returns>the translated message for the required license message</returns>
    private string GetLicenseTranslation(LicenseLevel level)
    {
        var translation = LicenseTranslations.GetValueOrDefault(level);
        if (string.IsNullOrEmpty(translation))
        {
            translation = Translater.Instant("Labels.LicenseRequired." + level);
            LicenseTranslations[level] = translation;
        }

        return translation;
    }
}