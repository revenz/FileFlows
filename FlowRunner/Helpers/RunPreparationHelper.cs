using FileFlows.ServerShared.Helpers;

namespace FileFlows.FlowRunner.Helpers;

/// <summary>
/// Helper to prepare a flow run
/// </summary>
public class RunPreparationHelper
{
    
    /// <summary>
    /// Downloads the scripts being used
    /// </summary>
    /// <param name="workingDirectory">the working directory</param>
    /// <param name="configDirectory">the source configuration directory</param>
    internal static void DownloadScripts(string workingDirectory, string configDirectory)
    {
        if (Directory.Exists(workingDirectory) == false)
            Directory.CreateDirectory(workingDirectory);
        
        DirectoryHelper.CopyDirectory(
            Path.Combine(configDirectory, "Scripts"),
            Path.Combine(workingDirectory, "Scripts"));
    }
    
    /// <summary>
    /// Downloads the plugins being used
    /// </summary>
    /// <param name="runInstance">the run instance running this</param>
    internal static void DownloadPlugins(RunInstance runInstance)
    {
        var dir = Path.Combine(runInstance.Properties.ConfigDirectory, "Plugins");
        if (Directory.Exists(dir) == false)
            return;
        runInstance.Properties.Logger?.ILog("Downloading plugins to: " + dir);
        foreach (var sub in new DirectoryInfo(dir).GetDirectories())
        {
            string dest = Path.Combine(runInstance.Properties.WorkingDirectory, sub.Name);
            #if(DEBUG)
            try
            {
                DirectoryHelper.CopyDirectory(sub.FullName, dest);
            }
            catch(Exception ex) when (ex.Message.Contains("another process"))
            {
                // Ignored
            }
            #else
            DirectoryHelper.CopyDirectory(sub.FullName, dest);
            #endif
        }
    }
}