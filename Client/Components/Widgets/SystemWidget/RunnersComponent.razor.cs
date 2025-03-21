using FileFlows.Client.Components.Dialogs;
using FileFlows.Client.Services.Frontend;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Widgets;

public partial class RunnersComponent : ComponentBase, IDisposable
{
    /// <summary>
    /// Gets or sets the frontend service
    /// </summary>
    [Inject] public FrontendService feService { get; set; }
    /// <summary>
    /// Gets or sets the blocker
    /// </summary>
    [CascadingParameter] public Blocker Blocker { get; set; }
    /// <summary>
    /// Gets or sets the editor
    /// </summary>
    [CascadingParameter] Editor Editor { get; set; }
    /// <summary>
    /// Gets or sets the users profile
    /// </summary>
    [CascadingParameter] public Profile Profile { get; set; }
    
    /// <summary>
    /// Callback when there are no runners when loading
    /// </summary>
    [Parameter] public EventCallback NoneOnLoad { get; set; }
    
    /// <summary>
    /// The runners
    /// </summary>
    private List<FlowExecutorInfoMinified> Runners = new ();
    
    /// <summary>
    /// Gets or sets the mode
    /// </summary>
    private int Mode { get; set; }

    /// <summary>
    /// The expanded/collapsed state of the runners
    /// </summary>
    private Dictionary<Guid, bool> RunnersState = new();

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        Runners = feService.Dashboard.CurrentExecutorInfoMinified;
        if(Runners.Count == 0)
            await NoneOnLoad.InvokeAsync();
        feService.Dashboard.RunnerInfoUpdated += ExecutorsUpdated;
    }
    

    /// <summary>
    /// Disposes of the component
    /// </summary>
    public void Dispose()
    {
        feService.Dashboard.RunnerInfoUpdated-= ExecutorsUpdated;
    }
    
    /// <summary>
    /// Called when the executors are updated
    /// </summary>
    /// <param name="obj">the updated executors</param>
    private void ExecutorsUpdated(List<FlowExecutorInfoMinified> obj)
    {
        Logger.Instance.ILog("ExecutorsUpdated Received!", obj);
        Runners = obj ?? new();
        // remove the RunnersState that are no longer in the list
        // do not add the runners that arent in the list to the list
        RunnersState = RunnersState.Where(x => Runners.Any(y => y.Uid == x.Key))
            .ToDictionary(x => x.Key, x => x.Value);

        var first = Runners.FirstOrDefault();
        if(first != null)
            RunnersState.TryAdd(first.Uid, true);
        Logger.Instance.ILog("ExecutorsUpdated RunnersState!", RunnersState);
        
        StateHasChanged();
    }
    
    /// <summary>
    /// Formats a <see cref="TimeSpan"/> value based on its duration.
    /// </summary>
    /// <param name="processingTime">The <see cref="TimeSpan"/> representing the processing time.</param>
    /// <returns>A formatted string representation of the time.</returns>
    private string FormatProcessingTime(TimeSpan processingTime)
    {
        if (processingTime.TotalDays >= 1)
            return processingTime.ToString(@"d\.hh\:mm\:ss");
        if (processingTime.TotalHours >= 1)
            return processingTime.ToString(@"h\:mm\:ss");
        
        return processingTime.ToString(@"m\:ss");
    }
    
    /// <summary>
    /// Cancels a runner
    /// </summary>
    /// <param name="runner">the runner to cancel</param>
    private async Task Cancel(FlowExecutorInfoMinified runner)
    {
        if (await Confirm.Show("Labels.Cancel",
                Translater.Instant("Pages.Dashboard.Messages.CancelMessage", new { runner.RelativeFile })) == false)
            return; // rejected the confirmation
        await HttpHelper.Delete($"/api/library-file/abort", new ReferenceModel<Guid>()
        {
            Uids = [runner.Uid]
        });
    }
    
    /// <summary>
    /// Opens the runner in detail
    /// </summary>
    /// <param name="runner">the runner</param>
    private async Task OpenRunner(FlowExecutorInfoMinified runner)
        => await Helpers.LibraryFileEditor.Open(Blocker, Editor, runner.LibraryFileUid, Profile, feService);

    /// <summary>
    /// Toggle the expand state of a runner
    /// </summary>
    /// <param name="runner">the runner to toggle</param>
    private void ToggleExpand(FlowExecutorInfoMinified runner)
    {
        if (RunnersState.TryAdd(runner.Uid, true) == false)
            RunnersState[runner.Uid] = !RunnersState[runner.Uid];
    }
    
    
    /// <summary>
    /// Defines the priority order for sorting specific keys before the rest.
    /// </summary>
    private static readonly Dictionary<string, int> PriorityMap = new()
    {
        { "encoder", 0 },
        { "decoder", 1 },
        { "bitrate", 2 },
        { "eta", 3 },
        { "speed", 4 },
        { "fps", 5 }
    };

    /// <summary>
    /// Returns the sorted additional information entries.
    /// Sorting is done lazily when the enumeration is accessed.
    /// </summary>
    /// <param name="runner">The runner containing additional information.</param>
    /// <returns>A lazily evaluated enumerable of sorted additional info.</returns>
    private static IEnumerable<object[]> GetSortedAdditional(FlowExecutorInfoMinified runner)
    {
        if (runner?.Additional == null)
            yield break;

        foreach (var item in runner.Additional
                     .Where(item => item.Length >= 2) // Ensure at least two elements
                     .OrderBy(item => PriorityMap.TryGetValue(item[0]?.ToString()?.ToLowerInvariant() ?? "", out var index) ? index : int.MaxValue)
                     .ThenBy(item => item[0]?.ToString(), StringComparer.OrdinalIgnoreCase))
        {
            yield return item;
        }
    }
}