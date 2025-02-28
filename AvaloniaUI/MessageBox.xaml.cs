using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FileFlows.AvaloniaUi;

/// <summary>
/// Represents a message box window with a title, message, and configurable buttons.
/// </summary>
public class MessageBox : Window
{
    /// <summary>
    /// A task completion source to track the dialog result.
    /// </summary>
    private TaskCompletionSource<bool>? _tcs;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageBox"/> class.
    /// </summary>
    /// <param name="title">The title of the message box.</param>
    /// <param name="message">The message to display in the message box.</param>
    /// <param name="confirmText">The text for the confirm button. Defaults to "OK".</param>
    /// <param name="cancelText">The text for the cancel button. Defaults to "Cancel".</param>
    /// <param name="showCancel">Determines whether the cancel button is shown. Defaults to false.</param>
    public MessageBox(string title, string message, string confirmText = "OK", string cancelText = "Cancel", bool showCancel = false)
    {
        InitializeComponent();

        this.Title = title;
        this.FindControl<TextBlock>("MessageText")!.Text = message;

        var confirmButton = this.FindControl<Button>("ConfirmButton");
        var cancelButton = this.FindControl<Button>("CancelButton");

        confirmButton!.Content = confirmText;
        cancelButton!.Content = cancelText;

        confirmButton.Click += (_, _) => CloseDialog(true);
        cancelButton.Click += (_, _) => CloseDialog(false);

        cancelButton.IsVisible = showCancel;
    }

    /// <summary>
    /// Loads the XAML layout for the message box.
    /// </summary>
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// Closes the dialog and sets the result.
    /// </summary>
    /// <param name="result">The result to return when the dialog is closed.</param>
    private void CloseDialog(bool result)
    {
        _tcs?.TrySetResult(result);
        Close();
    }

    /// <summary>
    /// Displays the message box as a dialog asynchronously.
    /// </summary>
    /// <param name="owner">The owner window of the message box.</param>
    /// <returns>A task that resolves to true if the confirm button was clicked, otherwise false.</returns>
    public Task<bool> ShowDialogAsync(Window owner)
    {
        _tcs = new TaskCompletionSource<bool>();
        this.ShowDialog(owner);
        return _tcs.Task;
    }
}
