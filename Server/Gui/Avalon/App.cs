using Avalonia;
using Avalonia.Controls;
using FileFlows.AvaloniaUi;
using FileFlows.Services;
using FileFlows.WebServer;

namespace FileFlows.Server.Gui.Avalon;

/// <summary>
/// FileFlows Server App
/// </summary>
public class App : FileFlows.AvaloniaUi.App
{
    /// <inheritdoc />
    protected override UiWindow CreateMainWindow()
        => new MainWindow();

    /// <inheritdoc />
    protected override string GetServerUrl()
        => WebServerApp.ServerUrl.ToLowerInvariant().StartsWith("https")
            ? $"https://{Environment.MachineName.ToLower()}:{WebServerApp.Port}/"
            : $"http://{Environment.MachineName.ToLower()}:{WebServerApp.Port}/";

    /// <inheritdoc />
    protected override string GetLoggingDirectory()
        => DirectoryHelper.LoggingDirectory;
    
    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        =>  AppBuilder.Configure<App>().UsePlatformDetect();

    /// <inheritdoc />
    protected override bool GetInitialStartMinimized()
        => ServiceLoader.Load<AppSettingsService>().Settings.StartMinimized;
}