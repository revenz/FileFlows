using FileFlows.Shared;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FileFlows.Client.Components.Common;

/// <summary>
/// A flow table button that can appear in a toolbar and/or a context menu
/// </summary>
public partial class FlowTableButton : ComponentBase, IDisposable
{
    /// <summary>
    /// Gets or sets the flow table that this button is contained within 
    /// </summary>
    [CascadingParameter] FlowTableBase Table { get; set; }
    
    /// <summary>
    /// Gets or sets the label for this button
    /// </summary>
    [Parameter] public string Label { get; set; }

    /// <summary>
    /// Gets or sets the icon for this button
    /// </summary>
    [Parameter]
    public string Icon { get; set; }
    
    /// <summary>
    /// Gets or sets  if the label should be hidden
    /// </summary>
    [Parameter]
    public bool HideLabel { get; set; }

    /// <summary>
    /// Gets or sets if this button is disabled
    /// </summary>
    [Parameter] public bool Disabled { get; set; }

    private bool _Enabled = true;
    /// <summary>
    /// Gets if this button is enabled
    /// </summary>
    public bool Enabled
    {
        get
        {
            if (Disabled) return false;
            return _Enabled;
        }
    }

    /// <summary>
    /// Gets or sets if the button is visible
    /// </summary>
    [Parameter] public bool Visible { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the area where this button will be shown
    /// </summary>
    [Parameter] public ButtonArea Area { get; set; }

    /// <summary>
    /// Gets or sets the event that is called when the button is clicked
    /// </summary>
    [Parameter] public EventCallback Clicked { get; set; }

    /// <summary>
    /// Gets or sets if this button is enabled only when one item is selected in the datalist
    /// </summary>
    /// 
    [Parameter] public bool SelectedOne { get; set; }
    /// <summary>
    /// Gets or sets if this button is enabled only when one or more items are selected in the datalist
    /// </summary>
    [Parameter] public bool SelectedOneOrMore { get; set; }

    /// <summary>
    /// Clicks the button and fires then Clicked event
    /// </summary>
    public virtual async Task OnClick()
    {
        await this.Clicked.InvokeAsync();
    }
    
    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (this.Table != null)
        {
            this.Table.AddButton(this);
            this.Table.SelectionChanged += Table_SelectionChanged;
        }
        Table_SelectionChanged(null);
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        Label = Translater.TranslateIfNeeded(Label ?? string.Empty); 
    }


    /// <summary>
    /// Called when the table selection has changed
    /// </summary>
    /// <param name="items">the newly selected items</param>
    private void Table_SelectionChanged(List<object> items)
    {
        bool current = this.Enabled;
        var count = items?.Count ?? 0;
        if (this.SelectedOne)
            this._Enabled = count == 1;
        else if (this.SelectedOneOrMore)
            this._Enabled = count > 0;
        else
            this._Enabled = true;
        if (current != this.Enabled)
            this.StateHasChanged();
    }

    /// <summary>
    /// Disposes of the button
    /// </summary>
    public void Dispose()
    {
        if (this.Table != null)
            this.Table.SelectionChanged -= Table_SelectionChanged;
    }

}


/// <summary>
/// An area where a button is shown
/// </summary>
public enum ButtonArea
{
    /// <summary>
    /// Default, shown anywhere
    /// </summary>
    Anywhere = 0,
    /// <summary>
    /// Only shown in toolbar
    /// </summary>
    Toolbar = 1,
    /// <summary>
    /// Only shown in context menu
    /// </summary>
    ContextMenu = 2
}