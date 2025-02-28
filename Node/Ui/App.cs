using Avalonia.Controls;

namespace FileFlows.Node.Ui;

/// <summary>
/// Node App
/// </summary>
public class App : FileFlows.AvaloniaUi.App
{
    /// <inheritdoc />
    protected override Window CreateMainWindow()
        => new MainWindow();

    /// <inheritdoc />
    protected override string GetServerUrl()
        => AppSettings.Instance.ServerUrl;

    /// <inheritdoc />
    protected override string GetLoggingDirectory()
        => DirectoryHelper.LoggingDirectory;
}