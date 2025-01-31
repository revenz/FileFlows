using FileFlows.Services;
using FileFlows.Shared.Models;
using FlowService = FileFlows.Services.FlowService;
using LibraryFileService = FileFlows.Services.LibraryFileService;
using LibraryService = FileFlows.Services.LibraryService;

namespace FileFlows.Server.Workers;

/// <summary>
/// Worker that will update all object references if names change
/// </summary>
public class ObjectReferenceUpdater:ServerWorker, IObjectReferenceUpdater
{
    private static bool IsRunning = false;
    /// <summary>
    /// Creates a new instance of the Object Reference Updater 
    /// </summary>
    public ObjectReferenceUpdater() : base(ScheduleType.Daily, 1)
    {
        ServiceLoader.AddSpecialCase<IObjectReferenceUpdater>(this);
    }

    /// <inheritdoc />
    protected override void ExecuteActual(Settings settings)
    {
        Run();
    }

    /// <inheritdoc />
    public async Task RunUpdate()
        => await RunAsync();

    /// <summary>
    /// Runs the updater asynchronously 
    /// </summary>
    internal async Task RunAsync()
    {
        await Task.Delay(1);
        Run();
    }
    
    /// <summary>
    /// Runs the updater
    /// </summary>
    internal void Run()
    {
        if (IsRunning)
            return;
        IsRunning = true;
        try
        {
            DateTime start = DateTime.UtcNow;
            var lfService = ServiceLoader.Load<LibraryFileService>();
            var libService = ServiceLoader.Load<LibraryService>();
            //var libFiles = lfService.GetAll(null).Result;
            var libraries = libService.GetAllAsync().Result;
            var flows = ServiceLoader.Load<FlowService>().GetAllAsync().Result;

            var dictLibraries = libraries.ToDictionary(x => x.Uid, x => x.Name);
            var dictFlows = flows.ToDictionary(x => x.Uid, x => x.Name);
            
            Logger.Instance.ILog("Time Taken to prepare for ObjectReference rename: "+ DateTime.UtcNow.Subtract(start));
            

            // foreach (var lf in libFiles)
            // {
            //     bool changed = false;
            //     if (dictLibraries.ContainsKey(lf.Library.Uid) && lf.Library.Name != dictLibraries[lf.Library.Uid])
            //     {
            //         string oldName = lf.Library.Name;
            //         string newName = dictLibraries[lf.Library.Uid];
            //         lf.LibraryName = newName;
            //         Logger.Instance.ILog($"Updating Library name reference '{oldName}' to '{lf.Library.Name}' in file: {lf.Name}");
            //         changed = true;
            //     }
            //
            //     if (lf.Flow != null && lf.Flow.Uid != Guid.Empty && dictFlows.ContainsKey(lf.Flow.Uid) &&
            //         lf.Flow.Name != dictFlows[lf.Flow.Uid])
            //     {
            //         string oldname = lf.Flow.Name;
            //         lf.Flow.Name = dictFlows[lf.Flow.Uid];
            //         Logger.Instance.ILog($"Updating Flow name reference '{oldname}' to '{lf.Flow.Name}' in file: {lf.Name}");
            //         changed = true;
            //     }
            //
            //     if (changed)
            //         lfService.Update(lf).Wait();
            // }

            foreach (var lib in libraries)
            {
                lfService.UpdateLibraryName(lib.Uid, lib.Name).Wait();
            }

            foreach (var flow in flows)
            {
                libService.UpdateFlowName(flow.Uid, flow.Name).Wait();
                lfService.UpdateFlowName(flow.Uid, flow.Name).Wait();
            }
            Logger.Instance.ILog("Time Taken to complete for ObjectReference rename: "+ DateTime.UtcNow.Subtract(start));
        }
        finally
        {
            IsRunning = false;
        }
    }
}