using System.Data;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;

namespace FileFlows.AvaloniaUi;

/// <summary>
/// Common base class for UI Windows
/// </summary>
public abstract class UiWindow : Window
{
    public UiWindow()
    {
        this.PropertyChanged += OnPropertyChanged;
    }

    /// <summary>
    /// Called when a property changed
    /// </summary>
    /// <param name="sender">the sener</param>
    /// <param name="e">the event</param>
    private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        // Check if the WindowState has changed to Minimized
        if (e.Property == Window.WindowStateProperty && (WindowState == WindowState.Minimized))
        {
            HideWindow();
        }
    }

    /// <summary>
    /// Hides the window
    /// </summary>
    protected void HideWindow()
    {
        Hide(); // Hide the window
    }

    /// <summary>
    /// Dont prompt with confirm quit mesage
    /// </summary>
    protected virtual bool DontPromptOnQuit => false;


    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (DontPromptOnQuit)
            return;
        
        e.Cancel = true;
        _ = Task.Run(async () =>
        {
            if(await Confirm("Quit", "Are you sure you want to quit?"))
            {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
                {
                    lifetime.Shutdown();
                }
            }
        });
    }


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