using FileFlows.Client.Services.Frontend;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Editors;

/// <summary>
/// Modal Editor base class
/// </summary>
public abstract class ModalEditor : ComponentBase, IModal
{
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
    /// Gets or sets if the init is done
    /// </summary>
    protected bool InitDone { get; private set; }

    /// <summary>
    /// Translation strings
    /// </summary>
    protected string lblTitle, lblClose, lblCancel, lblSave, lblSaving, lblHelp;

    private Editor _Editor;

    /// <summary>
    /// Gets or sets if the editor is being saved
    /// </summary>
    protected bool IsSaving { get; set; }

    /// <summary>
    /// Gets the help URL
    /// </summary>
    protected virtual string HelpUrl { get; }

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


    /// <inheritdoc />
    [Parameter]
    public TaskCompletionSource<object> TaskCompletionSource { get; set; }

    /// <inheritdoc />
    [Parameter]
    #pragma warning disable BL0007
    public abstract IModalOptions Options { get; set; }
    #pragma warning restore BL0007

    /// <summary>
    /// Closes the dialog
    /// </summary>
    public void Close()
    {
        TaskCompletionSource.TrySetCanceled(); // Set result when closing
    }

    /// <summary>
    /// Cancels the dialog
    /// </summary>
    public void Cancel()
    {
        TaskCompletionSource.TrySetCanceled(); // Indicate cancellation
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();

        lblSave = Translater.Instant("Labels.Save");
        lblSaving = Translater.Instant("Labels.Saving");
        lblCancel = Translater.Instant("Labels.Cancel");
        lblClose = Translater.Instant("Labels.Close");
        lblHelp = Translater.Instant("Labels.Help");

        InitDone = true;
    }

    /// <summary>
    /// Required validator
    /// </summary>
    protected readonly List<Validator> RequiredValidator = [new Required()];

    /// <summary>
    /// Opens the help page
    /// </summary>
    protected void OpenHelp()
        => _ = App.Instance.OpenHelp(HelpUrl);

    /// <summary>
    /// Gets the model passed in 
    /// </summary>
    /// <typeparam name="T">the type of model to get</typeparam>
    /// <returns>the model or null if a new instance</returns>
    protected T? GetModel<T>() where T : class
    {
        if (Options is ModalEditorOptions options == false)
            return null;
        return options.Model as T;
    }

    /// <summary>
    /// Gets the model UID 
    /// </summary>
    /// <returns>the model UID or empty GUID if none set</returns>
    protected Guid GetModelUid()
        => (Options as ModalEditorOptions)?.Uid ?? Guid.Empty;
}

/// <summary>
/// Generic modal ediotr options
/// </summary>
public class ModalEditorOptions : IModalOptions
{
    /// <summary>
    /// Gets or sets the model being edited
    /// </summary>
    public object Model { get; set; }
    
    /// <summary>
    /// Gest or sets the UID of the model being opened
    /// </summary>
    public Guid Uid { get; set; }
}