using FileFlows.RemoteServices;
using FileFlows.ServerShared.Helpers;
using FileFlows.ServerShared.Services;
using FileFlows.ServerShared.Workers;

namespace FileFlows.Node.Workers;

public class ConfigCleaner: Worker
{
    public ConfigCleaner() : base(ScheduleType.Daily, 5)
    {
        CleanConfigs();
    }

    protected override void Execute() => CleanConfigs();
    
    private void CleanConfigs()
    {
        var cfgDir = new DirectoryInfo(DirectoryHelper.ConfigDirectory);
        if (cfgDir.Exists == false)
            return;
        Dictionary<int, DirectoryInfo> dirs = new();
        int max = -1;
        foreach (var dir in cfgDir.GetDirectories())
        {
            if (int.TryParse(dir.Name, out int revision) == false)
                continue;
            dirs.Add(revision, dir);
            max = Math.Max(revision, max);
        }

        var cfgService = ServiceLoader.Load<ConfigurationService>();

        int current = cfgService.CurrentConfig?.Revision ?? 0;
        foreach (var kv in dirs)
        {
            if (kv.Key == max || kv.Key == current)
                continue;
            if (current > 0 && kv.Value.CreationTimeUtc > DateTime.UtcNow.AddHours(-12))
                continue; // dont delete, something could still be using this
            try
            {
                kv.Value.Delete(true);
                Logger.ILog("Deleted old configuration from node: " + kv.Value.FullName);
            }
            catch (Exception)
            {
                // Ignored
            }
        }
    }
}