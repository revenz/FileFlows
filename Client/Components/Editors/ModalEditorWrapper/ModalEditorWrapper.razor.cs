using FileFlows.Client.Services.Frontend;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Editors;

public partial class ModalEditorWrapper : ComponentBase
{
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
    
    /// <summary>
    /// Gets or sets the frontend service
    /// </summary>
    [Inject]
    protected FrontendService feService { get; set; }

    /// <summary>
    /// Gets or sets the navigation manager used
    /// </summary>
    [Inject]
    protected NavigationManager NavigationManager { get; set; }
    
    /// <summary>
    /// Gets or sets the modal editor
    /// </summary>
    [Parameter] public ModalEditor Modal { get; set; }

    /// <summary>
    /// Gets or sets if the init is done
    /// </summary>
    protected bool InitDone => Modal.InitDone;

    /// <summary>
    /// Gets or sets if this is readonly 
    /// </summary>
    protected bool ReadOnly => Modal.ReadOnly;

    /// <summary>
    /// Translation strings
    /// </summary>
    protected string lblClose, lblCancel, lblSave, lblSaving, lblHelp;

    private Editor _Editor;

    /// <summary>
    /// Gets or sets if the editor is being saved
    /// </summary>
    protected bool IsSaving => Modal.IsSaving;

    /// <summary>
    /// Gets the help URL
    /// </summary>
    protected string HelpUrl => Modal.HelpUrl;

    /// <summary>
    /// Gets the title
    /// </summary>
    protected string Title => Modal.Title;

    /// <summary>
    /// Gets or sets the container
    /// </summary>
    public ViContainer Container
    {
        get => Modal.Container; 
        set => Modal.Container = value;
    }

    /// <summary>
    /// Gets or sets the editor
    /// </summary>
    public Editor Editor
    {
        get => _Editor;
        set
        {
            if (_Editor != value && value != null)
            {
                _Editor = value;
                StateHasChanged();
            }
        }
    }

    /// <summary>
    /// Closes the dialog
    /// </summary>
    public void Close()
        => Modal.Close();

    /// <summary>
    /// Cancels the dialog
    /// </summary>
    public void Cancel()
        => Modal.Cancel();

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();

        lblSave = Translater.Instant("Labels.Save");
        lblSaving = Translater.Instant("Labels.Saving");
        lblCancel = Translater.Instant("Labels.Cancel");
        lblClose = Translater.Instant("Labels.Close");
        lblHelp = Translater.Instant("Labels.Help");
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
            _ = Load();
    }


    private async Task Load()
    {
        Container.ShowBlocker();

        try
        {
            await Modal.Load();
        }
        finally
        {
            Container.HideBlocker();
        }
    }

    /// <summary>
    /// Opens the help page
    /// </summary>
    protected void OpenHelp()
        => _ = App.Instance.OpenHelp(HelpUrl);


    /// <summary>
    /// Saves the modal
    /// </summary>
    /// <returns>a task to await</returns>
    protected virtual Task Save() => Modal.Save();
}