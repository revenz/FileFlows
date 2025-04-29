using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components;

/// <summary>
/// Container with title and body
/// </summary>
public partial class ViContainer
{
    /// <summary>
    /// Gets or sets the title
    /// </summary>
    [Parameter]
    public string Title { get; set; }

    /// <summary>
    /// Gets or sets the Icon to show
    /// </summary>
    [Parameter]
    public string Icon { get; set; }
    
    /// <summary>
    /// Gets or sets if this is a page view
    /// </summary>
    [Parameter]public bool PageView { get; set; }

    /// <summary>
    /// Gets or sets if this takes up the entire width
    /// </summary>
    [Parameter]
    public bool FullWidth { get; set; }

    /// <summary>
    /// Gets or sets if the title show always be shown
    /// </summary>
    [Parameter]
    public bool AlwaysShowTitle { get; set; }


    /// <summary>
    /// Gest or sets the head fragment
    /// </summary>
    [Parameter]
    public RenderFragment Head { get; set; }
    /// <summary>
    /// Gets or sets the left head fragment
    /// </summary>
    [Parameter]
    public RenderFragment HeadLeft { get; set; }

    /// <summary>
    /// Gets or sets the body to render
    /// </summary>
    [Parameter]
    public RenderFragment Body { get; set; }

    /// <summary>
    /// Gets or sets if this container can be maxmised
    /// </summary>
    [Parameter] public bool Maximise { get; set; }

    /// <summary>
    /// Gets or sets event callback when the maximise is changed
    /// </summary>
    [Parameter] public EventCallback<bool> OnMaximised { get; set; }

    /// <summary>
    /// Gets or sets if this should flex
    /// </summary>
    [Parameter]
    public bool Flex { get; set; }
    
    /// <summary>
    /// Gets or sets additional class names to add to the ViContainer
    /// </summary>
    [Parameter] public string ClassName { get; set; }

    /// <summary>
    /// Gets or sets if this is maximised
    /// </summary>
    private bool IsMaximised { get; set; }

    private Blocker Blocker;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        this.IsMaximised = false;
    }

    /// <summary>
    /// Toggles if this is maxmimised
    /// </summary>
    private void ToggleMaximise()
    {
        this.IsMaximised = !this.IsMaximised;
        OnMaximised.InvokeAsync(this.IsMaximised);
    }

    /// <summary>
    /// Shows the blocker
    /// </summary>
    public void ShowBlocker(string message = null)
    {
        Blocker?.Show(message);
        StateHasChanged();
        
    }
    
    /// <summary>
    /// Hides the blocker
    /// </summary>
    public void HideBlocker()
    {
        Blocker?.Hide();
        StateHasChanged();
    }
}