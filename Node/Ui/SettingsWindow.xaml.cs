using System;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using FileFlows.NodeClient;
using FileFlows.RemoteServices;

namespace FileFlows.Node.Ui;

/// <summary>
/// Settings Window
/// </summary>
public partial class SettingsWindow: FileFlows.AvaloniaUi.UiWindow
{
    /// <summary>
    /// A task completion source to track the dialog result.
    /// </summary>
    private TaskCompletionSource<bool>? _tcs;
    
    private SettingsViewModel ViewModel;
    
    /// <inheritdoc />
    protected override bool DontPromptOnQuit => true;

    /// <summary>
    /// Main window constructor
    /// </summary>
    public SettingsWindow()
    {
        ViewModel = new SettingsViewModel();
        base.DataContext = ViewModel;
        InitializeComponent();

        ViewModel.ServerUrl = AppSettings.Instance.ServerUrl ?? $"http://{Environment.MachineName}:19200";
        ViewModel.AccessToken = AppSettings.Instance.AccessToken ?? string.Empty;
        ViewModel.StartMinimized = AppSettings.Instance.StartMinimized;
        
        var txtServerUrl = this.FindControl<TextBox>("txtServerUrl");
        this.Opened += async (_, _) => await Task.Delay(10).ContinueWith(_ =>
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                txtServerUrl?.Focus();
            });
        });
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

    protected void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void Close_OnClick(object? sender, RoutedEventArgs e)
    {
        this.Close();
        _tcs?.TrySetResult(true);
    }

    private void Register_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel.Validate().Failed(out var error))
        {
            _ = Message("Error", error);
            return;
        }
        
        AppSettings.Instance.ServerUrl = ViewModel.ServerUrl.Trim();
        AppSettings.Instance.StartMinimized = ViewModel.StartMinimized;
        AppSettings.Instance.AccessToken = ViewModel.AccessToken.Trim();
        AppSettings.Instance.Save();

        IsEnabled = false;

        Task.Run(async () =>
        {
            try
            {
                var result = await Program.Manager!.Register();
                if (result.Success == false)
                {
                    await Message("Failed", result.Message);
                    return;
                }
            }
            catch (Exception ex)
            {
                await Message("Failed", ex.Message);
                return;
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsEnabled = true;
                this.Close();
            });
        });
    }
}