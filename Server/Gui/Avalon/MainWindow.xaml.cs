using Avalonia.Interactivity;
using FileFlows.AvaloniaUi;
using FileFlows.Services;
using FileFlows.WebServer;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Runtime.InteropServices;

namespace FileFlows.Server.Gui.Avalon;

/// <summary>
/// Main window for Server application
/// </summary>
public class MainWindow : UiWindow
{ 
    public MainWindow()
    {
        InitializeComponent();
        
        var dc = new MainWindowViewModel(this)
        {
            CustomTitle = Globals.IsWindows
        };
        
        DataContext = dc;
    }


    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }


    /// <summary>
    /// Launches the server URL in a browser
    /// </summary>
    public void Launch()
    {
        string url = $"http://{Environment.MachineName}:{WebServerApp.Port}/";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            Process.Start("open", url);
        else
            Process.Start(new ProcessStartInfo("xdg-open", url));
    }


    /// <summary>
    /// The open button was clicked
    /// </summary>
    /// <param name="sender">the sender</param>
    /// <param name="e">the event</param>
    private void BtnOpen_OnClick(object? sender, RoutedEventArgs e)
        => Launch();

    public void Minimize()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            this.Hide();
        else   
            this.WindowState = WindowState.Minimized;
    }
}

public class MainWindowViewModel
{ 
    /// <summary>
    /// Gets or sets if a custom title should be rendered
    /// </summary>
    public bool CustomTitle { get; set; }
    private MainWindow Window { get; set; }
    public string ServerUrl { get; set; }
    public string Version { get; set; }

    /// <summary>
    /// Gets or sets if the app should start minimized
    /// </summary>
    public bool StartMinimized
    {
        get => AppSettingsService.Settings.StartMinimized;
        set
        {
            if (AppSettingsService.Settings.StartMinimized != value)
            {
                AppSettingsService.Settings.StartMinimized = value;
                AppSettingsService.Save();
            }
        } 
    }

    private AppSettingsService AppSettingsService;

    public MainWindowViewModel(MainWindow window)
    {
        AppSettingsService = ServiceLoader.Load<AppSettingsService>();
        this.Window = window;
        this.ServerUrl = WebServerApp.ServerUrl.ToLowerInvariant().StartsWith("https")
            ? $"https://{Environment.MachineName.ToLower()}:{WebServerApp.Port}/"
            : $"http://{Environment.MachineName.ToLower()}:{WebServerApp.Port}/";
        this.Version = Globals.Version;
    }
}