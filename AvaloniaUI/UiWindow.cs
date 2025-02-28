using Avalonia.Controls;
using Avalonia.Threading;

namespace FileFlows.AvaloniaUi;

/// <summary>
/// Common base class for UI Windows
/// </summary>
public abstract class UiWindow : Window
{
    /// <summary>
    /// Show a confirmation prompt
    /// </summary>
    /// <param name="title">the title of the confirmation prompt</param>
    /// <param name="message">the message of the confirmation prompt</param>
    /// <returns>the confirmation result</returns>
    public async Task<bool> Confirm(string title, string message)
    {
        return await Dispatcher.UIThread.InvokeAsync(() =>
        {
            IsEnabled = false;
            var msgBox = new MessageBox(title, message, "Yes", "No", showCancel: true);
            var result = msgBox.ShowDialogAsync(this);
            IsEnabled = true;
            return result;
        });
    }
    
    /// <summary>
    /// Show a message 
    /// </summary>
    /// <param name="title"></param>
    /// <param name="message"></param>
    /// <returns>the task to await</returns>
    public async Task Message(string title, string message)
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            IsEnabled = false;
            var msgBox = new MessageBox( title, message, "Close", "", showCancel: false);
            await msgBox.ShowDialogAsync(this);
            IsEnabled = true;
            return;
        });
    }
}