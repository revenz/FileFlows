using System.Data;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;

namespace FileFlows.AvaloniaUi;

/// <summary>
/// Common base class for UI Windows
/// </summary>
public abstract class UiWindow : Window, IMessageHandler
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
            if(OperatingSystem.IsMacOS() == false) 
                HideWindow();
        }
    }

    /// <summary>
    /// Hides the window
    /// </summary>
    protected void HideWindow()
    {
        if(OperatingSystem.IsMacOS() == false) 
            Hide(); // Hide the window
    }

    /// <summary>
    /// Quits the app
    /// </summary>
    public void Quit(bool noPrompt = false)
    {
        WindowState = WindowState.Normal;
        Show();
        
        _ = Task.Run(async () =>
        {
            if (noPrompt || await Confirm("Quit", QuitMessage))
            {
                _quitting = true;
                _ = Dispatcher.UIThread.InvokeAsync(Close);
            }
        });
    }

    /// <summary>
    /// Dont prompt with confirm quit mesage
    /// </summary>
    protected virtual bool DontPromptOnQuit => false;

    /// <summary>
    /// Gets the quit message;
    /// </summary>
    protected virtual string QuitMessage => "Are you sure you want to quit?";

    private bool _quitting = false;
    
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);

        if (DontPromptOnQuit == false && _quitting == false) // Detects close button
        {
            e.Cancel = true; // Prevent closing if needed
            Quit();
        }
    }

    public void CloseWindow()
    {
        _quitting = true;
        this.Close();
    }

    /// <inheritdoc />
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
    
    /// <inheritdoc />
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