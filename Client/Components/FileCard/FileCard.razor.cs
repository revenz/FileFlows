using System.Text.RegularExpressions;
using FileFlows.Client.Components.Editors;
using FileFlows.Client.Helpers;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components;

/// <summary>
/// File Card
/// </summary>
public partial class FileCard : ComponentBase
{
    /// <summary>
    /// Gets or sets the modal service
    /// </summary>
    [Inject] private IModalService ModalService { get; set; }
    
    /// <summary>
    /// Gets or sets the message service
    /// </summary>
    [Inject] private MessageService Message { get; set; }
    
    /// <summary>
    /// Gets or sets the model
    /// </summary>
    [Parameter]
    public LibraryFileMinimal Model { get; set; }
    
    /// <summary>
    /// Gets or sets if this is in a table and shouldnt have a border drawn
    /// </summary>
    [Parameter]
    public bool InTable { get; set; }
    
    /// <summary>
    /// Gets or sets if this is a minimal view
    /// </summary>
    [Parameter] public bool Minimal { get; set; }
    
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
    
    private static readonly Regex VideoCodecRegex = new(
        @"\b(hevc|h[\.\- ]?26[45]|av1|vp[89]|mpeg[\- ]?2|mpeg[\- ]?4|xvid|divx|wmv|theora|prores|dnxhd|vc\-1|rv\d+)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex AudioCodecRegex = new(
        @"\b(aac|ac[\- ]?3|eac[\- ]?3|dts(:?-hd|:?-ma)?|flac|mp3|opus|vorbis|truehd|pcm|alac|wav|wma|atmos|amr|mka)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex AudioLayoutRegex = new(
        @"\b(mono|dual mono|stereo|2\.0|2ch|2 channels|5\.1|6\.1|7\.1|7ch|8ch|surround|quadraphonic|3\.0|4\.0|5\.0|6\.0|7\.0)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ResolutionRegex = new(
        @"\b(720p|1080p|1440p|2160p|4k|8k|\d{3,4}x\d{3,4})\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    /// <summary>
    /// Translations
    /// </summary>
    private string lblAborting, lblInternalProcessingNode, lblManualLibrary;

    private List<KeyValuePair<string, string>> Traits = [];

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        lblAborting = Translater.Instant("Labels.Aborting");
        lblInternalProcessingNode = Translater.Instant("Labels.InternalProcessingNode");
        lblManualLibrary = Translater.Instant("Labels.ManualLibrary");
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        Traits = (Model?.Traits ?? []).Select(x =>
        {
            string icon = VideoCodecRegex.IsMatch(x) ? "fas fa-video"
                : AudioCodecRegex.IsMatch(x) ? "fas fa-volume-up"
                : AudioLayoutRegex.IsMatch(x) ? "fas fa-headphones"
                : ResolutionRegex.IsMatch(x) ? "fas fa-tv"
                : "fas fa-tag";

            return new  KeyValuePair<string, string>(x, icon);
        }).ToList();
    }

    /// <summary>
    /// Opens the file for viewing
    /// </summary>
    private void OpenFile()
    {
        _ = ModalService.ShowModal<FileViewer>(new ModalEditorOptions()
        {
            Uid = Model.Uid
        });
    }

    /// <summary>
    /// Gets the thumbnail url
    /// </summary>
    /// <returns>the thumbnail url</returns>
    private string GetThumbUrl()
        => IconHelper.GetThumbnail(Model.Uid, Model.DisplayName, Model.Extension == null);
    
    /// <summary>
    /// Gets the extension image
    /// </summary>
    /// <returns>the extension image</returns>
    private string GetExtensionImage()
        => IconHelper.GetExtensionImage(Model.DisplayName);
    
    
    /// <summary>
    /// Humanizes a date, eg 11 hours ago
    /// </summary>
    /// <param name="dateUtc">the date</param>
    /// <returns>the humanized date</returns>
    protected string DateString(DateTime? dateUtc)
    {
        if (dateUtc == null) return string.Empty;
        if (dateUtc.Value.Year < 2020) return string.Empty; // fixes 0000-01-01 issue
        // var localDate = new DateTime(date.Value.Year, date.Value.Month, date.Value.Day, date.Value.Hour,
        //     date.Value.Minute, date.Value.Second);

        return FormatHelper.HumanizeDate(dateUtc.Value);
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
    /// Gets the icon class
    /// </summary>
    /// <returns>the icon class</returns>
    private string GetIconClass()
    {
        var runner = Model as ProcessingLibraryFile;
        if (runner?.HasThumbnail == true)
            return "icon thumbnail";
        if (Model.Status is FileStatus.Processed)
            return "icon thumbnail";
        return "icon";
    }

    /// <summary>
    /// Gets the icon source
    /// </summary>
    /// <returns>the icon source</returns>
    private string GetIconSource()
    {
        var runner = Model as ProcessingLibraryFile;
        if (runner?.HasThumbnail == true)
        {
            return IconHelper.GetThumbnail(runner.Uid,
                runner.DisplayName + runner.Extension, runner.IsDirectory);
        }

        if (Model.Status is FileStatus.Processed)
            return GetThumbUrl();
        
        return GetExtensionImage();
    }
}