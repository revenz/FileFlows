using Avalonia;
using FileFlows.Services;

namespace FileFlows.Server.Gui.Avalon;

internal class App : Avalonia.Application
{
    public override void Initialize()
    {
        base.Initialize();
        
        var window = new MainWindow();
        var settings = ServiceLoader.Load<AppSettingsService>().Settings;
        if(settings.StartMinimized == false)
            window.Show();
    }
    

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp(bool messagebox = false)
        => (messagebox ? AppBuilder.Configure<MessageApp>() : AppBuilder.Configure<App>())
            .UsePlatformDetect();
}
