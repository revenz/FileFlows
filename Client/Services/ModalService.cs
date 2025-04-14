using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client.Services;

/// <summary>
/// Options for a modal
/// </summary>
public interface IModalOptions
{
    
}
/// <summary>
/// Interface for the modal service, allowing for opening and closing modals.
/// </summary>
public interface IModalService
{
    /// <summary>
    /// Shows a modal dialog of type <typeparamref name="T"/> with the specified options.
    /// </summary>
    /// <typeparam name="T">The type of the modal dialog to show. It must implement <see cref="IModal{TResult, TOptions}"/>.</typeparam>
    /// <typeparam name="U">The return type from the dialog.</typeparam>
    /// <param name="options">The options to pass to the modal dialog.</param>
    /// <returns>A task that represents the asynchronous operation, containing the result of the modal dialog.</returns>
    Task<Result<U>> ShowModal<T, U>(IModalOptions options) where T : IModal, new();

    /// <summary>
    /// Retrieves a list of currently active modal dialogs.
    /// </summary>
    /// <returns>A list of active modals implementing <see cref="IModal"/>.</returns>
    List<IModal> GetActiveModals();

    /// <summary>
    /// Event that is triggered when the collection of active modals changes.
    /// </summary>
    event Action OnModalsChanged;
}

public interface IModal : IComponent
{
    /// <summary>
    /// Gets or sets the completion task
    /// </summary>
    TaskCompletionSource<object> TaskCompletionSource { get; set; }
    
    /// <summary>
    /// Gets or sets the options for this modal
    /// </summary>
    IModalOptions Options { get; set; }

    /// <summary>
    /// Closes the modal.
    /// </summary>
    void Close();

    /// <summary>
    /// Cancels the modal.
    /// </summary>
    void Cancel();
}

/// <summary>
/// Implementation of the modal service.
/// </summary>
public class ModalService : IModalService
{
    private readonly List<IModal> activeModals = new();
    private readonly IJSRuntime jsRuntime;
    private DotNetObjectReference<ModalService> dotNetRef;

    /// <inheritdoc />    
    public event Action OnModalsChanged;

    public ModalService(IJSRuntime jsRuntime)
    {
        this.jsRuntime = jsRuntime;
        dotNetRef = DotNetObjectReference.Create(this);
        _ = jsRuntime.InvokeVoidAsync("ff.modalServiceHandleEscape", dotNetRef);
    }

    /// <inheritdoc />
    public async Task<Result<V>> ShowModal<T, V>(IModalOptions options) where T : IModal, new()
    {
        var modal = new T
        {
            Options = options // Set the options directly
        };

        // Create a TaskCompletionSource to manage modal result
        modal.TaskCompletionSource = new ();

        activeModals.Add(modal);
        OnModalsChanged?.Invoke(); // Notify about the new modal
        try
        {
            var result = await modal.TaskCompletionSource.Task;

            if (modal.TaskCompletionSource.Task.Status == TaskStatus.Canceled)
                return Result<V>.Fail("Canceled");

            return (V)result;
        }
        catch (Exception ex)
        {
            return Result<V>.Fail(ex.Message);
        }
        finally
        {
            activeModals.Remove(modal);
            OnModalsChanged?.Invoke();
        }
    }

    /// <inheritdoc />
    public List<IModal> GetActiveModals() => activeModals.ToList();
    
    /// <summary>
    /// Handles the escape key being pushed
    /// </summary>
    [JSInvokable]
    public void OnEscapePressed()
    {
        if (activeModals.Count == 0)
            return;

        // Get the last modal (top-most)
        var topModal = activeModals[^1];
        topModal.Cancel();
    }

    /// <summary>
    /// Disposes of hte service
    /// </summary>
    /// <returns>the value task</returns>
    public ValueTask DisposeAsync()
    {
        dotNetRef?.Dispose();
        return ValueTask.CompletedTask;
    }
}
