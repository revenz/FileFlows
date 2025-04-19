using System.Text.RegularExpressions;
using FileFlows.Client.Components.Dialogs;
using FileFlows.Client.Components.Editors;
using FileFlows.Client.Services.Frontend;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Widgets;

public partial class RunnersComponent : ComponentBase, IDisposable
{
    /// <summary>
    /// Gets or sets the modal service
    /// </summary>
    [Inject] private IModalService ModalService { get; set; }
    /// <summary>
    /// Gets or sets the frontend service
    /// </summary>
    [Inject] public FrontendService feService { get; set; }
    /// <summary>
    /// Gets or sets the blocker
    /// </summary>
    [CascadingParameter] public Blocker Blocker { get; set; }
    
    /// <summary>
    /// Gets or sets the message service
    /// </summary>
    [Inject] MessageService Message { get; set; }
    /// <summary>
    /// Gets or sets the editor
    /// </summary>
    [CascadingParameter] Editor Editor { get; set; }
    
    /// <summary>
    /// Callback when there are no runners when loading
    /// </summary>
    [Parameter] public EventCallback NoneOnLoad { get; set; }
    
    /// <summary>
    /// Gets or sets if this is a minimal view
    /// </summary>
    [Parameter] public bool Minimal { get; set; }
    
    /// <summary>
    /// The runners
    /// </summary>
    private List<ProcessingLibraryFile> Runners = new ();
    
    /// <summary>
    /// Gets or sets the mode
    /// </summary>
    private int Mode { get; set; }

    /// <summary>
    /// Translation strings
    /// </summary>
    private string lblAborting;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        lblAborting = Translater.Instant("Labels.Aborting");
        Runners = feService.Files.Processing;
        if(Runners.Count == 0)
            await NoneOnLoad.InvokeAsync();
        feService.Files.ProcessingUpdated += OnProcessingUpdated;
    }
    

    /// <summary>
    /// Disposes of the component
    /// </summary>
    public void Dispose()
    {
        feService.Files.ProcessingUpdated -= OnProcessingUpdated;
    }
    
    /// <summary>
    /// Called when the executors are updated
    /// </summary>
    /// <param name="obj">the updated executors</param>
    private void OnProcessingUpdated(List<ProcessingLibraryFile> obj)
    {
        Runners = obj;
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
    private async Task Cancel(ProcessingLibraryFile runner)
    {
        if (await Message.Confirm("Labels.Cancel",
                Translater.Instant("Labels.CancelMessage", new { runner.DisplayName })) == false)
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
    private async Task OpenRunner(ProcessingLibraryFile runner)
        => await ModalService.ShowModal<FileViewer>(new ModalEditorOptions()
        {
            Uid = runner.Uid
        });
    
    
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
    private IEnumerable<object[]> GetSortedAdditional(ProcessingLibraryFile runner)
    {
        if (runner?.Additional == null)
            yield break;

        // Define a HashSet for efficient lookups
        var validPrefixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ETA", "Speed" };
        
        foreach (var item in runner.Additional
                     .Where(item =>
                     {
                         if (item.Length < 2)
                             return false;  // Ensure at least two elements
                         if (Minimal == false)
                             return true;
                         return validPrefixes.Contains(item[0]?.ToString());
                     })
                     .OrderBy(item => PriorityMap.TryGetValue(item[0]?.ToString()?.ToLowerInvariant() ?? "", out var index) ? index : int.MaxValue)
                     .ThenBy(item => item[0]?.ToString(), StringComparer.OrdinalIgnoreCase))
        {
            yield return item;
        }
    }
}