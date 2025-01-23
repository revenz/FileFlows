using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Common;

/// <summary>
/// A page show in a flow wizard
/// </summary>
public partial class FlowWizardPage : ComponentBase
{
    /// <summary>
    /// Gets or sets the <see cref="FlowWizard"/> component containing this page.
    /// </summary>
    [CascadingParameter]
    FlowWizard Wizard { get; set; }

    /// <summary>
    /// Gets or sets the icon associated with the page.
    /// </summary>
    [Parameter] public string Icon { get; set; }
    
    /// <summary>
    /// Gets or sets if this is indented/a child page
    /// </summary>
    [Parameter] public bool Indented { get; set; }

    /// <summary>
    /// Gets or sets the content of the page.
    /// </summary>
    [Parameter] public RenderFragment ChildContent { get; set; }
    
    /// <summary>
    /// Gets or sets a method to call to check if this page can be advanced
    /// </summary>
    [Parameter] public Func<Task<bool>>? OnPageAdvanced { get; set; }


#pragma warning disable BL0007
    private bool _Visible = true;
    
    /// <summary>
    /// Gets or sets a value indicating whether the page is visible.
    /// </summary>
    [Parameter]
    public bool Visible
    {
        get => _Visible;
        set
        {
            if (_Visible == value)
                return;
            _Visible = value;
            Wizard?.PageVisibilityChanged();
            this.StateHasChanged();
        }
    }

    private string _Title;

    /// <summary>
    /// Gets or sets the title of the page.
    /// </summary>
    [Parameter] public string Title
    {
        get => _Title;
        set { _Title = Translater.TranslateIfNeeded(value); }
    }

    
    private string _Description;

    /// <summary>
    /// Gets or sets the description of the page.
    /// </summary>
    [Parameter] public string Description
    {
        get => _Description;
        set { _Description = Translater.TranslateIfNeeded(value); }
    }

    private bool _Disabled;

    /// <summary>
    /// Gets or sets if this page is disabled
    /// </summary>
    [Parameter]
    public bool Disabled
    {
        get => _Disabled;
        set
        {
            if (_Disabled == value)
                return;
            _Disabled = value;
            StateHasChanged();
            Wizard?.TriggerStateHasChanged();
        }
    }

    private bool _NextDisabled;
    
    /// <summary>
    /// Gets or sets if the next button is disabled on this page
    /// </summary>
    [Parameter]
    public bool NextDisabled
    {
        get => _NextDisabled;
        set
        {
            if (_NextDisabled == value)
                return;
            _NextDisabled = value;
            StateHasChanged();
            Wizard?.TriggerStateHasChanged();
        }
    }

    private bool _Invalid;

    /// <summary>
    /// Gets or sets if this page is invalid
    /// </summary>
    [Parameter]
    public bool Invalid
    {
        get => _Invalid;
        set
        {
            if (_Invalid == value)
                return;
            _Invalid = value;
            StateHasChanged();
            Wizard?.TriggerStateHasChanged();
        }
    }
#pragma warning restore BL0007


    /// <summary>
    /// Initializes the page when it is first rendered.
    /// </summary>
    protected override void OnInitialized()
    {
        Wizard.AddPage(this);
    }

    /// <summary>
    /// Determines whether the current tab page active.
    /// </summary>
    /// <returns><c>true</c> if the current page is active; otherwise, <c>false</c>.</returns>
    private bool IsActive() => this.Wizard.ActivePage == this;

    /// <summary>
    /// Triggers state has changed in the tab
    /// </summary>
    public void TriggerStateHasChanged()
        => StateHasChanged();
}
