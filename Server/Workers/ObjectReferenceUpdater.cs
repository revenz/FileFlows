using FileFlows.Services;
using FileFlows.Services.FileDropServices;
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
        _ = RunAsync();
    }

    /// <inheritdoc />
    public async Task RunUpdate()
        => await RunAsync();

    /// <summary>
    /// Runs the updater asynchronously 
    /// </summary>
    internal async Task RunAsync()
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
            var libraries = await libService.GetAllAsync();
            var flows = await ServiceLoader.Load<FlowService>().GetAllAsync();

            // var dictLibraries = libraries.ToDictionary(x => x.Uid, x => x.Name);
            // var dictFlows = flows.ToDictionary(x => x.Uid, x => x.Name);
            
            Logger.Instance.ILog("Time Taken to prepare for ObjectReference rename: "+ DateTime.UtcNow.Subtract(start));
            
            foreach (var lib in libraries)
            {
                if (lib == null)
                    continue;
                if(lfService != null)
                    await lfService.UpdateLibraryName(lib.Uid, lib.Name);
            }

            foreach (var flow in flows)
            {
                if (flow == null || flow.Name == null)
                    continue;
                
                if(libService != null)
                    await libService.UpdateFlowName(flow.Uid, flow.Name);
                if(lfService != null)
                    await lfService.UpdateFlowName(flow.Uid, flow.Name);
                
            }
            Logger.Instance.ILog("Time Taken to complete for ObjectReference rename: "+ DateTime.UtcNow.Subtract(start));
        }
        finally
        {
            IsRunning = false;
        }
    }
}