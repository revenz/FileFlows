using FileFlows.Client.Components.Dialogs;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Widgets;

public partial class RunnersComponent : ComponentBase, IDisposable
{
    /// <summary>
    /// Gets or sets the client service
    /// </summary>
    [Inject] public ClientService ClientService { get; set; }
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
        await Refresh();
        if(Runners.Count == 0)
            await NoneOnLoad.InvokeAsync();
#if(DEBUG)
        // Runners = GenerateRandomExecutors(10);
#endif
        ClientService.ExecutorsUpdated += ExecutorsUpdated;
    }
    
#if(DEBUG)
    
    public static List<FlowExecutorInfoMinified> GenerateRandomExecutors(int count)
    {
        var random = new Random();
        var executors = new List<FlowExecutorInfoMinified>();

        for (int i = 0; i < count; i++)
        {
            // Generate random file name and GUID
            string fileName = $"file-{i + 1}.mkv";
            Guid uid = Guid.NewGuid();
            int totalParts = random.Next(5, 20); // Random total parts between 5 and 20
            int currentPart = random.Next(1, totalParts + 1); // Random current part within total parts

            executors.Add(new FlowExecutorInfoMinified
            {
                Uid = uid,
                DisplayName = fileName,
                NodeName = "FileFlowsServer",
                LibraryFileUid = uid,
                LibraryFileName = $"/home/user/videos/{fileName}",
                RelativeFile = fileName,
                LibraryName = "Video Library",
                TotalParts = totalParts,
                CurrentPart = currentPart,
                CurrentPartName = $"Part {currentPart} Processing",
                CurrentPartPercent = random.Next(0, 100), // Random percentage 0-100
                LastUpdate = DateTime.UtcNow.AddSeconds(-random.Next(0, 60 * 60)), // Random last update within the last hour
                StartedAt = DateTime.UtcNow.AddMinutes(-random.Next(1, 120)), // Started between 1 and 120 minutes ago
                //ProcessingTime = TimeSpan.FromSeconds(random.Next(30, 3600)), // Random processing time 30 seconds to 1 hour
                FramesPerSecond = random.Next(20, 300), // Random FPS between 20.0 and 60.0
                //Additional = new List<string>() // Optionally populate with random additional data
                Additional = Enumerable.Range(1, 6).Select(x => new object[] { "Label", random.Next(20, 3000)}).ToArray()
            });
        }

        return executors;
    }
    #endif

    /// <summary>
    /// Refreshes the runners
    /// </summary>
    private async Task Refresh()
    {
        var result = await HttpHelper.Get<List<FlowExecutorInfoMinified>>("/api/worker");
        if (result.Success && Runners.Count == 0)
            Runners = result.Data ?? [];
    }
    

    /// <summary>
    /// Disposes of the component
    /// </summary>
    public void Dispose()
    {
        ClientService.ExecutorsUpdated -= ExecutorsUpdated;
    }
    
    /// <summary>
    /// Called when the executors are updated
    /// </summary>
    /// <param name="obj">the updated executors</param>
    private void ExecutorsUpdated(List<FlowExecutorInfoMinified> obj)
    {
        Runners = obj ?? new();
        // remove the RunnersState that are no longer in the list
        // do not add the runners that arent in the list to the list
        RunnersState = RunnersState.Where(x => Runners.Any(y => y.Uid == x.Key))
            .ToDictionary(x => x.Key, x => x.Value);

        var first = Runners.FirstOrDefault();
        if(first != null)
            RunnersState.TryAdd(first.Uid, true);
        
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
        await HttpHelper.Delete($"/api/worker/by-file/{runner.Uid}");
    }
    
    /// <summary>
    /// Opens the runner in detail
    /// </summary>
    /// <param name="runner">the runner</param>
    private async Task OpenRunner(FlowExecutorInfoMinified runner)
        => await Helpers.LibraryFileEditor.Open(Blocker, Editor, runner.LibraryFileUid, Profile);

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