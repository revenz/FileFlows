using System.Diagnostics;
using FileFlows.Server.Workers;
using FileFlows.ServerShared.Models.StatisticModels;
using FileHelper = FileFlows.ServerShared.Helpers.FileHelper;
using LibraryFileService = FileFlows.Services.LibraryFileService;
using StatisticService = FileFlows.Services.StatisticService;

namespace FileFlows.WebServer.Controllers;

/// <summary>
/// System Controller
/// </summary>
[Route("/api/system")]
[FileFlowsAuthorize]
public class SystemController:BaseController
{
    /// <summary>
    /// Opens a URL in the host OS
    /// </summary>
    /// <param name="url">the URL to open</param>
    [HttpPost("open-url")]
    public void OpenUrl([FromQuery]string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;
        if (url.ToLowerInvariant()?.StartsWith("http") != true)
            return; // dont allow
        if (Globals.UsingWebView == false)
            return; // only open if using WebView
        
        if (OperatingSystem.IsWindows())
            Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
        else if (OperatingSystem.IsMacOS())
            Process.Start("open", url);
        else
            Process.Start(new ProcessStartInfo("xdg-open", url));
    }
    
    /// <summary>
    /// Pauses the system
    /// </summary>
    /// <param name="duration">duration in minutes to pause for, any number less than 1 will resume</param>
    [HttpPost("pause")]
    [FileFlowsAuthorize(UserRole.PauseProcessing)]
    public async Task Pause([FromQuery] int duration)
    {
        var service = ServiceLoader.Load<PausedService>();
        await service.Pause(duration, await GetAuditDetails());
    }


    /// <summary>
    /// Gets the system information for the FileFlows server,
    /// which includes memory and CPU usage
    /// </summary>
    /// <returns></returns>
    [HttpGet("info")]
    public SystemInfo GetSystemInfo()
        => ServiceLoader.Load<DashboardService>().GetSystemInfo();

    /// <summary>
    /// Gets history CPU data of system information
    /// </summary>
    /// <param name="since">data since a date</param>
    /// <returns>the history CPU data</returns>
    [HttpGet("history-data/cpu")]
    public IEnumerable<SystemValue<float>> GetCpuData([FromQuery] DateTime? since = null)
    {
        var SystemMonitor = ServiceLoader.Load<ISystemMonitorService>();
        if (since != null)
            return SystemMonitor.CpuUsage.Where(x => x.Time > since);
        var data = SystemMonitor.CpuUsage;
        return EaseData(data);
    }
    
    /// <summary>
    /// Gets history memory data of system information
    /// </summary>
    /// <param name="since">data since a date</param>
    /// <returns>the history memory data</returns>
    [HttpGet("history-data/memory")]
    public IEnumerable<SystemValue<float>> GetMemoryData([FromQuery] DateTime? since = null)
    {
        var SystemMonitor = ServiceLoader.Load<ISystemMonitorService>();
        if (since != null)
            return SystemMonitor.MemoryUsage.Where(x => x.Time > since);
        var data = SystemMonitor.MemoryUsage;
        return EaseData(data);
    }
    

    private IEnumerable<SystemValue<T>> EaseData<T>(IEnumerable<SystemValue<T>> data)
    {
        List<SystemValue<T>> eased = new();
        var dtCutoff = DateTime.UtcNow.AddMinutes(-5);
        var recent = data.Where(x => x.Time > dtCutoff);
        var older = data.Where(x => x.Time <= dtCutoff)
            .GroupBy(x => new DateTime(x.Time.Year, x.Time.Month, x.Time.Day, x.Time.Hour, x.Time.Minute, 0))
            .ToDictionary(x => x.Key, x => x.ToList());
        foreach (var old in older)
        {
            double max = old.Value.Max(x => Convert.ToDouble(x.Value));
            eased.Add(new ()
            {
                Time = old.Key,
                Value = (T)Convert.ChangeType(max, typeof(T))
            });
        }
        eased.AddRange(recent);
        return eased;
    }
    
    /// <summary>
    /// Gets history library processing time data
    /// </summary>
    /// <returns>history library processing time data</returns>
    [HttpGet("history-data/library-processing-time")]
    public async Task<object> GetLibraryProcessingTime()
    {
        var data = (await ServiceLoader.Load<LibraryFileService>().GetLibraryProcessingTimes()).ToArray();
        var dict = data.Select(x => new
        {
            x.Library,
            Value = (x.OriginalSize / 1_000_000d) / x.Seconds
        }).OrderBy(x => x.Value).GroupBy(x => x.Library, x=> x);
        
        return dict.Where(x => x.Count() > 10).Select(x =>
        {
            var list = x.ToList();
            int length = list.Count;
            var median = list[length / 2];
            var lq = list[length / 4];
            var hq = list[length / 4 * 3];
            return new
            {
                x = x.Key?.EmptyAsNull() ?? "Unknown", 
                y = new [] { (int)list[0].Value, (int)lq.Value, (int)median.Value,(int) hq.Value,(int) list[length -1].Value }
            };
        });
    }

    /// <summary>
    /// Gets heat map data for the processing times of the system
    /// </summary>
    /// <returns></returns>
    [HttpGet("history-data/processing-heatmap")]
    public List<HeatmapData> GetProcessingHeatMap()
        => ServiceLoader.Load<StatisticService>().GetHeatMap(Globals.STAT_PROCESSING_TIMES_HEATMAP);

    /// <summary>
    /// Restarts FileFlows server
    /// </summary>
    [HttpPost("restart")]
    [FileFlowsAuthorize(UserRole.Admin)]
    public void Restart()
    {
        if (Globals.IsDocker == false)
        {
            string script = Path.Combine(DirectoryHelper.BaseDirectory, "Server",
                "restart." + (Globals.IsWindows ? "bat" : "sh"));
            if (Globals.IsLinux)
                FileHelper.MakeExecutable(script);
            
            var psi = new ProcessStartInfo(script);
            psi.ArgumentList.Add(Process.GetCurrentProcess().Id.ToString());
            psi.WorkingDirectory = Path.Combine(DirectoryHelper.BaseDirectory, "Server");
            psi.UseShellExecute = true;
            psi.CreateNoWindow = true;
#if(!DEBUG)
            Process.Start(psi);
#endif
        }

        // docker is easy, just stop it and it should auto restart
        WorkerManager.StopWorkers();
        Environment.Exit(99);
    }
}