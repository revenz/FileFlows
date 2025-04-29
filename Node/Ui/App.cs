using Avalonia.Controls;
using FileFlows.AvaloniaUi;

namespace FileFlows.Node.Ui;

/// <summary>
/// Node App
/// </summary>
public class App : FileFlows.AvaloniaUi.App
{
    /// <inheritdoc />
    protected override UiWindow CreateMainWindow()
        => new MainWindow();

    /// <inheritdoc />
    protected override string GetServerUrl()
        => AppSettings.Instance.ServerUrl;

    /// <inheritdoc />
    protected override string GetLoggingDirectory()
        => DirectoryHelper.LoggingDirectory;

    /// <inheritdoc />
    protected override bool GetInitialStartMinimized()
        => AppSettings.Instance.StartMinimized;
}