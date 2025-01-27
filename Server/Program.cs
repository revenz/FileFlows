using FileFlows.Server.Cli;
using FileFlows.Services;
using FileFlows.Shared.Helpers;

namespace FileFlows.Server;

/// <summary>
/// Main entry point for server
/// </summary>
public class Program
{
    /// <summary>
    /// General cache used by the server
    /// </summary>
    internal static CacheStore GeneralCache = new ();

    [STAThread] // need for Photino.net on windows
    public static void Main(string[] args)
    {
#if(DEBUG)
        FixTranslations(Path.Combine("..", "Client", "wwwroot", "i18n"));
        var pluginsDir = Path.Combine("..", "..", "FileFlowsPlugins");
        if (Directory.Exists(pluginsDir))
        {
            foreach (var pluginDir in Directory.GetDirectories(pluginsDir))
            {
                FixTranslations(Path.Combine(pluginDir, "i18n"));
            }
        }
#endif
        
        if (CommandLine.Process(args))
            return;

        Application app = new Application();
        ServerShared.Services.SharedServiceLoader.Loader = type => ServiceLoader.Provider.GetRequiredService(type);
        app.Run(args);
    }


#if DEBUG
    private static void FixTranslations(string directory)
    {
        if (Directory.Exists(directory) == false)
            return;
        FileFlows.Services.ServiceHelpers.TranslationFileHelper.SyncWithEnglish(directory);
        var jsonFiles = Directory.GetFiles(directory, "*.json");

        foreach (var jsonFile in jsonFiles)
        {
            if (jsonFile.Contains("plugins.", StringComparison.InvariantCultureIgnoreCase))
                continue;
            var jsonContent = File.ReadAllText(jsonFile);
            var reorderedJson = Translater.ReorderJson(jsonContent);
            File.WriteAllText(jsonFile, reorderedJson);
        }
    }
#endif
    
}
