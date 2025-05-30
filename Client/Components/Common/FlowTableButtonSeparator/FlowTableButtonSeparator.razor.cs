using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Common;

public partial class FlowTableButtonSeparator
{
    [CascadingParameter] FlowTableBase Table { get; set; }
    /// <summary>
    /// Gets or sets if the button is visible
    /// </summary>
    [Parameter] public bool Visible { get; set; } = true;

    /// <summary>
    /// Gets or sets if this is visible on mobile
    /// </summary>
    [Parameter] public bool Mobile { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the area where this button will be shown
    /// </summary>
    [Parameter] public ButtonArea Area { get; set; }
    
    protected override void OnInitialized()
    {
        if (this.Table != null)
        {
            this.Table.AddButtonSeperator(this);
        }
    }
}