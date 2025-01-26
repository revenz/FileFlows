using System.Text;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Common;

/// <summary>
/// Flow Wizard component
/// </summary>
public partial class FlowWizard : ComponentBase
{
    /// <summary>
    /// Gets or sets the content of the wizard
    /// </summary>
    [Parameter] public RenderFragment ChildContent { get; set; }
    /// <summary>
    /// Gets or sets the active page
    /// </summary>
    public FlowWizardPage ActivePage { get; internal set; }
    
    /// <summary>
    /// Gets or sets an event when a page changes
    /// </summary>
    [Parameter] public EventCallback<int> OnPageChanged { get; set; }
    
    /// <summary>
    /// Gets or sets if this wizard is a modal popup
    /// </summary>
    [Parameter] public bool Modal { get; set; }
    
    /// <summary>
    /// Gets or sets if this can be cancled, and if the Cancel button should be shown
    /// </summary>
    [Parameter] public bool Cancelable { get; set; }
    
    /// <summary>
    /// Gets or sets if this wizard isnt a next/previous/finish wizard, but a list of wizard pages
    /// where each page is effectively a selected group and next submits that selected group
    /// </summary>
    [Parameter] public bool NonWizard { get; set; }
    /// <summary>
    /// Gets or sets the selected page index
    /// </summary>
    [Parameter]
    public int SelectedPageIndex { get; set; }

    /// <summary>
    /// Event called when the selected page index changes
    /// </summary>
    [Parameter] 
    public EventCallback<int> SelectedPageIndexChanged { get; set; }
    
    /// <summary>
    /// Gets or sets the finish button label
    /// </summary>
    [Parameter] public string FinishButtonLabel { get; set; }
    
    /// <summary>
    /// Gets or sets an event when a finish is clicked
    /// </summary>
    [Parameter] public EventCallback OnFinish { get; set; }
    
    /// <summary>
    /// Gets or sets an event when a cancel is clicked
    /// </summary>
    [Parameter] public EventCallback OnCancel { get; set; }
    
    /// <summary>
    /// Gets or sets if the pages cannot be changed
    /// </summary>
    [Parameter] public bool DisableChanging { get; set; }
    
    /// <summary>
    /// Gets or sets if the finish button is disabled
    /// </summary>
    [Parameter] public bool FinishDisabled { get; set; }
    
    /// <summary>
    /// Gets or sets the wizards blocker
    /// </summary>
    private Blocker Blocker { get; set; }

    /// <summary>
    /// Represents a collection of pages.
    /// </summary>
    private List<FlowWizardPage> Pages = new();

    /// <summary>
    /// Adds a page to the collection.
    /// </summary>
    /// <param name="page">The page to add.</param>
    internal void AddPage(FlowWizardPage page)
    {
        if (Pages.Contains(page) == false)
        {
            Pages.Add(page);
            if (ActivePage == null)
                ActivePage = page;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Selects a page
    /// </summary>
    /// <param name="page">The page to select.</param>
    private async Task SelectPage(FlowWizardPage page)
    {
        if (DisableChanging || page.Disabled) return;
        if (await CheckAdvancing(page) == false)
            return;
        
        ActivePage = page;
        SelectedPageIndex = Pages.IndexOf(page);
        await SelectedPageIndexChanged.InvokeAsync(SelectedPageIndex);
        await OnPageChanged.InvokeAsync(SelectedPageIndex);
        StateHasChanged();
    }
 
    /// <summary>
    /// Checks if the page is allowed to be advanced
    /// </summary>
    /// <param name="page">the page we are advancing to</param>
    /// <returns>true if can go to this page</returns>
    private async Task<bool> CheckAdvancing(FlowWizardPage page)
    {
        if (page == null || ActivePage == null)
            return true;
        if (ActivePage?.OnPageAdvanced == null)
            return true;
        bool advancing = Pages.IndexOf(page) > Pages.IndexOf(ActivePage);
        if (advancing == false)
            return true;
        return await ActivePage.OnPageAdvanced.Invoke();
    }

    /// <summary>
    /// Called when the visibility of a page has changed
    /// </summary>
    internal void PageVisibilityChanged()
        => StateHasChanged();

    /// <inheritdoc/>
    protected override Task OnParametersSetAsync()
    {
        SelectFirstPage();
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Sets the first visible page as the active page.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public void SelectFirstPage()
    {
        if (DisableChanging) return;
        
        if (ActivePage == null)
        {
            ActivePage = Pages.FirstOrDefault(x => x.Visible);
            OnPageChanged.InvokeAsync(Pages.IndexOf(ActivePage));
            StateHasChanged();
        }
    }


    /// <summary>
    /// Selects the previous page
    /// </summary>
    private void Previous()
    {
        int index = Pages.IndexOf(ActivePage);
        while (true)
        {
            index--;
            if (index < 0)
                return;
            if (Pages[index].Visible)
                break;
        }
        
        _ = SelectPage(Pages[index]);
    }
    
    /// <summary>
    /// Selects the next page
    /// </summary>
    private void Next()
    {
        if (ActivePage?.NextDisabled == true)
            return;
        
        int index = Pages.IndexOf(ActivePage);
        while (true)
        {
            index++;
            if (index >= Pages.Count)
                return;
            if (Pages[index].Visible)
                break;
        }
        _ = SelectPage(Pages[index]);
    }

    /// <summary>
    /// Completes the wizard
    /// </summary>
    private void Finish()
        => OnFinish.InvokeAsync();
    
    /// <summary>
    /// Cancels the wizard
    /// </summary>
    private void Cancel()
        => OnCancel.InvokeAsync();

    /// <summary>
    /// Triggers a state has change event
    /// </summary>
    public void TriggerStateHasChanged()
        => StateHasChanged();

    /// <summary>
    /// Shows the wizards blocker
    /// </summary>
    /// <param name="message">Optional message to show in the blocker</param>
    public void ShowBlocker(string message = "")
        => Blocker.Show(message);

    /// <summary>
    /// Hides the wizards blocker
    /// </summary>
    public void HideBlocker()
        => Blocker.Hide();
}