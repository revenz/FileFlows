using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Common;

/// <summary>
/// Component for common from page buttons
/// </summary>
public partial class FlowFormPageButtons : ComponentBase
{
    /// <summary>
    /// Gets or sets if the form is saving
    /// </summary>
    [Parameter] public bool IsSaving { get; set; }
    
    /// <summary>
    /// Gets or sets the callback for when save is clicked
    /// </summary>
    [Parameter] public Func<Task> OnSave { get; set; }
    
    /// <summary>
    /// Gets or sets the Help URL page
    /// </summary>
    [Parameter] public string HelpUrl { get; set; }
    
    /// <summary>
    /// Gets or sets a option description content
    /// </summary>
    [Parameter] public RenderFragment Description { get; set; }
    
    /// <summary>
    /// Translations
    /// </summary>
    private string lblSave, lblSaving, lblHelp;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        lblSave = Translater.Instant("Labels.Save");
        lblSaving = Translater.Instant("Labels.Saving");
        lblHelp = Translater.Instant("Labels.Help");
    }

    /// <summary>
    /// Save button clicked
    /// </summary>
    private void Save()
        => OnSave?.Invoke();

    /// <summary>
    /// Help button clicked
    /// </summary>
    private void OpenHelp()
        => _ = App.Instance.OpenHelp(HelpUrl);
}