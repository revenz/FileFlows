using Avalonia;
using Avalonia.Controls;

namespace FileFlows.Server.Gui.Avalon;

/// <summary>
/// FileFlows Server App
/// </summary>
public class App : FileFlows.AvaloniaUi.App
{
    /// <inheritdoc />
    protected override Window CreateMainWindow()
        => new MainWindow();

    /// <inheritdoc />
    protected override string GetServerUrl()
        => WebServer.WebServerApp.ServerUrl;

    /// <inheritdoc />
    protected override string GetLoggingDirectory()
        => DirectoryHelper.LoggingDirectory;
    
    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        =>  AppBuilder.Configure<App>().UsePlatformDetect();
}