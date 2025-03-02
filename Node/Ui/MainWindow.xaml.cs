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
using Logger = FileFlows.ScriptExecution.Logger;

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
        ViewModel.Version = Globals.Version;
        ViewModel.ConnectionState = Program.Manager!.CurrentStete;
        ViewModel.ConnectionText = Program.Manager!.CurrentStete.ToString();
        Program.Manager!.OnConnectionUpdated += OnConnectionUpdated;
        
        var txtServerUrl = this.FindControl<TextBox>("txtServerUrl");
        this.Opened += async (_, _) => await Task.Delay(10).ContinueWith(_ =>
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                txtServerUrl?.Focus();
            });
        });
        EventManager.Subscribe(EventNames.RUNNERS_UPDATED, (int runners) =>
        {
            ViewModel.ActiveRunners = runners;
        });
    }

    /// <inheritdoc />
    protected override string QuitMessage
    {
        get
        {
            if(ViewModel.ActiveRunners < 1)
                return base.QuitMessage;
            if (ViewModel.ActiveRunners == 1)
                return "Are you sure you want to quit?\n\nA file is currently being processed and will be aborted.";
            return $"Are you sure you want to quit?\n\n{ViewModel.ActiveRunners} files are currently being processed and will be aborted.";
        }
    }

    /// <summary>
    /// Called when the connection state chagnes
    /// </summary>
    /// <param name="state">the new connection state</param>>
    private void OnConnectionUpdated(ConnectionState state)
    {
        ViewModel.ConnectionState = state;
        ViewModel.ConnectionText = state.ToString();
    }

    protected void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
    
    private void Close_OnClick(object? sender, RoutedEventArgs e)
        => this.Close();

    private void Settings_OnClick(object? sender, RoutedEventArgs e)
    {
        IsEnabled = false;
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            try
            {
                var settingsWindow =
                    new SettingsWindow();
                await settingsWindow.ShowDialogAsync(this);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception caught in Dispatcher.UIThread: " + ex.Message);
            }
            finally
            {
                await Dispatcher.UIThread.InvokeAsync(() => { IsEnabled = true; });
            }
        });
        
    }
}