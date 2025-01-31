using System.Collections.Concurrent;
using System.Diagnostics;
using FileFlows.Services;
using FileFlows.ServerShared.Workers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Workers;

/// <summary>
/// Worker that monitors system information
/// </summary>
public class SystemMonitor:Worker, ISystemMonitorService
{
    public static readonly FixedSizedQueue<SystemValue<float>> _CpuUsage = new (500);
    public static readonly FixedSizedQueue<SystemValue<float>> _MemoryUsage = new (500);

    private NodeService _nodeService;
    
    /// <inheritdoc />
    public float[] LatestCpuUsage 
    {
        get
        {
            lock (_CpuUsage) // Ensure thread safety if CpuUsage can be accessed concurrently
            {
                return _CpuUsage.Reverse().Take(30).Select(x => x.Value).Reverse().ToArray();
            }
        }
    }
    
    /// <inheritdoc />
    public long[] LatestMemoryUsage 
    {
        get
        {
            lock (_MemoryUsage) // Ensure thread safety if CpuUsage can be accessed concurrently
            {
                return _MemoryUsage.Reverse().Take(30).Select(x => (long)x.Value).Reverse().ToArray();
            }
        }
    }
    /// <inheritdoc />
    public SystemValue<float>[] CpuUsage 
    {
        get
        {
            lock (_CpuUsage) // Ensure thread safety if CpuUsage can be accessed concurrently
            {
                return _CpuUsage.ToArray();
            }
        }
    }
    
    /// <inheritdoc />
    public SystemValue<float>[] MemoryUsage 
    {
        get
        {
            lock (_MemoryUsage) // Ensure thread safety if CpuUsage can be accessed concurrently
            {
                return _MemoryUsage.ToArray();
            }
        }
    }

    /// <summary>
    /// Database service
    /// </summary>
    private DatabaseService dbService;

    /// <summary>
    /// The settings service
    /// </summary>
    private SettingsService settingsService;
    
    public SystemMonitor() : base(ScheduleType.Second, 3, quiet: true)
    {
        ServiceLoader.AddSpecialCase<ISystemMonitorService>(this);
        dbService = ServiceLoader.Load<DatabaseService>();
        _nodeService = ServiceLoader.Load<NodeService>();
        settingsService = (SettingsService)ServiceLoader.Load<ISettingsService>();
    }

    protected override void Execute()
    {
        var taskCpu = GetCpu();
        long memoryUsage = GC.GetTotalMemory(true);

        _MemoryUsage.Enqueue(new()
        {
            Value = memoryUsage
        });

        Task.WaitAll(taskCpu);//, taskOpenDatabaseConnections, taskTempStorage);
        _CpuUsage.Enqueue(new ()
        {
            Value = taskCpu.Result
        });
        

        var nodes = _nodeService.GetAllAsync().Result;

        var settings = settingsService.Get().Result;
        var service = ServiceLoader.Load<IClientService>();
        service.UpdateSystemInfo(new()
        {
            CpuUsage = LatestCpuUsage,
            MemoryUsage = LatestMemoryUsage,
            IsPaused = settings.IsPaused,
            PausedUntil = settings.PausedUntil,
            NodeStatuses = nodes.Select(x => new NodeStatus
            {
                Uid = x.Uid,
                Name = x.Name,
                Version = x.Version,
                Enabled = x.Enabled,
                OutOfSchedule = TimeHelper.InSchedule(x.Schedule) == false,
                ScheduleResumesAtUtc = TimeHelper.UtcDateUntilInSchedule(x.Schedule)
            }).ToList()
        });
    }

    private async Task<float> GetCpu()
    {
        await Task.Delay(1);
        List<float> records = new List<float>();
        int max = 7;
        for (int i = 0; i <= max; i++)
        {
            var startTime = DateTime.UtcNow;
            var startCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            await Task.Delay(100);

            stopWatch.Stop();
            var endTime = DateTime.UtcNow;
            var endCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;

            var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
            var totalMsPassed = (endTime - startTime).TotalMilliseconds;
            var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);

            records.Add((float)(cpuUsageTotal * 100));
            if (i == max)
                break;
            await Task.Delay(1000);
        }

        return records.Max();
    }
}


/// <summary>
/// A queue of fixed size
/// </summary>
/// <typeparam name="T">the type to queue</typeparam>
public class FixedSizedQueue<T> : ConcurrentQueue<T>
{
    private readonly object syncObject = new object();

    /// <summary>
    /// Gets or sets the max queue size
    /// </summary>
    public int Size { get; private set; }

    /// <summary>
    /// Constructs an instance of a fixed size queue
    /// </summary>
    /// <param name="size">the size of the queue</param>
    public FixedSizedQueue(int size)
    {
        Size = size;
    }

    /// <summary>
    /// Adds a item to the queue
    /// </summary>
    /// <param name="obj">the item to add</param>
    public new void Enqueue(T obj)
    {
        base.Enqueue(obj);
        lock (syncObject)
        {
            while (base.Count > Size)
            {
                base.TryDequeue(out _);
            }
        }
    }
}