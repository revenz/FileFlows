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

namespace FileFlows.Node.Ui;

/// <summary>
/// Main window for the UI
/// </summary>
public partial class MainWindow : FileFlows.AvaloniaUi.UiWindow
{
    private MainWindowViewModel ViewModel;

    /// <summary>
    /// Main window constructor
    /// </summary>
    public MainWindow()
    {
        ViewModel = new MainWindowViewModel(this);
        base.DataContext = ViewModel;
        InitializeComponent();

        ViewModel.ServerUrl = AppSettings.Instance.ServerUrl ?? $"http://{Environment.MachineName}:19200";
        ViewModel.AccessToken = AppSettings.Instance.AccessToken ?? string.Empty;
        ViewModel.StartMinimized = AppSettings.Instance.StartMinimized;
        ViewModel.Version = Globals.Version;
        
        var txtServerUrl = this.FindControl<TextBox>("txtServerUrl");
        this.Opened += async (_, _) => await Task.Delay(10).ContinueWith(_ =>
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                txtServerUrl?.Focus();
            });
        });
    }
    
    
    protected void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
    
    private void Close_OnClick(object? sender, RoutedEventArgs e)
        => this.Close();


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
                    await Message("Failed", result.Message);
            }
            catch (Exception ex)
            {
                await Message("Failed", ex.Message);
            }

            await Dispatcher.UIThread.InvokeAsync(() => { IsEnabled = true; });
        });
    }
}