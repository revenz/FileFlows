using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace FileFlows.Client.Components.Common;

/// <summary>
/// Context menu
/// </summary>
public partial class FlowContextMenu : IDisposable
{
    /// <summary>
    /// Gets or sets the items in the context menu
    /// </summary>
    [Parameter]
    public List<FlowContextMenuItem> Items { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the child content
    /// </summary>
    [Parameter] public RenderFragment ChildContent { get; set; }
    
    /// <summary>
    /// Gets or sets a css classes to add to the wrapper div 
    /// </summary>
    [Parameter] public string CssClass { get; set; }
    
    /// <summary>
    /// Gets or sets if the context menu is visible
    /// </summary>
    public bool Visible { get; set; }

    /// <summary>
    /// Gets or sets an event that is shown just prior to opening a context menu
    /// </summary>
    [Parameter] public EventCallback PreShow { get; set; }
    
    private ElementReference menuRef;


    private double xPos, yPos;
    [Inject] IJSRuntime jsRuntime { get; set; }
    protected override void OnInitialized()
    {
        App.Instance.OnDocumentClick += OnDocumentClick;
        App.Instance.OnWindowBlur += OnWindowBlur;
    }

    public void SetItems(List<FlowContextMenuItem> items)
    {
        Items = items ?? new();
    }

    void Clicked(FlowContextMenuItem item)
    {
        this.Visible = false;
        item.OnClick?.Invoke();
    }

    async Task ShowContextMenu(MouseEventArgs e)
    {
        await this.PreShow.InvokeAsync();
        if (this.Items?.Any() != true)
            return;

        await ShowAt(e.ClientX, e.ClientY);
        // this.xPos = e.ClientX;
        // this.yPos = e.ClientY;
        // this.Visible = true;
    }

    public void Dispose()
    {
        App.Instance.OnDocumentClick -= OnDocumentClick;
        App.Instance.OnWindowBlur -= OnWindowBlur;
    }

    void OnDocumentClick()
    {
        Visible = false;
        StateHasChanged();
    }
    void OnWindowBlur()
    {
        Visible = false;
        StateHasChanged();
    }

    async Task ShowAt(double xPos, double yPos)
    {
        this.xPos = xPos;
        this.yPos = yPos;

        await PreShow.InvokeAsync();

        var viewport = await jsRuntime.InvokeAsync<Size>("ff.getViewportSize");
        var menuSize = await jsRuntime.InvokeAsync<Size?>("ff.getElementSize", menuRef);

        const int padding = 20;

        if (menuSize != null)
        {
            if (xPos + menuSize.Width > viewport.Width - padding)
                this.xPos = viewport.Width - menuSize.Width - padding;

            if (yPos + menuSize.Height > viewport.Height - padding)
                this.yPos = viewport.Height - menuSize.Height - padding;

            // Clamp to keep on screen
            this.xPos = Math.Max(padding, this.xPos);
            this.yPos = Math.Max(padding, this.yPos);
        }

        Visible = true;
        StateHasChanged();
    }

    public class Size
    {
        public double Width { get; set; }
        public double Height { get; set; }
    }

}

/// <summary>
/// An item in a context menu
/// </summary>
public class FlowContextMenuItem
{
    /// <summary>
    /// Gets or sets if this is a separator
    /// </summary>
    public bool Separator { get; set; }
    /// <summary>
    /// Gets or sets the label
    /// </summary>
    public string Label { get; set; }
    /// <summary>
    /// Gets or sets the icon
    /// </summary>
    public string Icon { get; set; }
    /// <summary>
    /// Gets or sets the onclick action
    /// </summary>
    public Action OnClick { get; set; }
    
    /// <summary>
    /// Gets or sets if this ia help button
    /// </summary>
    public bool IsHelpButton { get; set; }
}