using FileFlows.Client.Services.Frontend;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Widgets;

/// <summary>
/// Status Update widget
/// </summary>
public partial class StatusWidget : ComponentBase, IDisposable
{
    /// <summary>
    /// Gets or sets the frontend service
    /// </summary>
    [Inject] public FrontendService feService { get; set; }
    /// <summary>
    /// Gets or sets the paused service
    /// </summary>
    [Inject] private IPausedService PausedService { get; set; }
    
    /// <summary>
    /// Gets or sets an event callback when an update is clicked
    /// </summary>
    [Parameter] public EventCallback OnUpdatesClicked { get; set; }
    
    /// <summary>
    /// Translations
    /// </summary>
    private string lblTitle, lblPause, lblResume;

    private List<FlowExecutorInfoMinified> _executors = [];
    private UpdateInfo? _updateInfo = null;
    private SystemInfo? _sysInfo = null;

    private enum SystemStatus
    {
        Idle,
        Paused,
        Processing,
        UpdateAvailable,
        OutOfSchedule
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        lblTitle = Translater.Instant("Labels.Status");
        lblPause = Translater.Instant("Labels.Pause");
        lblResume = Translater.Instant("Labels.Resume");
        _updateInfo = feService.Dashboard.CurrentUpdatesInfo;
        PausedService.OnPausedLabelChanged += OnPausedLabelChanged;
        feService.Runner.RunnerInfoUpdated += OnExecutorsUpdated;
        feService.Dashboard.UpdateInfoUpdated += OnUpdatesUpdateInfo;
        feService.Dashboard.SystemInfoUpdated += OnSystemInfoUpdated;
        OnPausedLabelChanged(PausedService.PausedLabel);
    }

    /// <summary>
    /// Event raised when the system info has bene updated
    /// </summary>
    /// <param name="info">the updated info</param>
    private void OnSystemInfoUpdated(SystemInfo info)
    {
        _sysInfo = info;
        StateHasChanged();
    }

    /// <summary>
    /// Called when the update info changes
    /// </summary>
    /// <param name="info">the new info</param>
    private void OnUpdatesUpdateInfo(UpdateInfo info)
    {
        _updateInfo = info;
        StateHasChanged();
    }

    /// <summary>
    /// Gets the status
    /// </summary>
    /// <returns>the status</returns>
    private SystemStatus GetStatus()
    {
        if (PausedService.IsPaused)
            return SystemStatus.Paused;
        if (_executors.Count > 0)
            return SystemStatus.Processing;
        if (_sysInfo != null && _sysInfo.NodeStatuses.Any(x => x.Enabled) &&
            _sysInfo.NodeStatuses.Where(x => x.Enabled).All(x => x.OutOfSchedule))
            return SystemStatus.OutOfSchedule;
        if(_updateInfo is { HasUpdates: true })
            return SystemStatus.UpdateAvailable;

        return SystemStatus.Idle;
    }

    /// <summary>
    /// Called when the paused label is changed
    /// </summary>
    /// <param name="label">the new label</param>
    private void OnPausedLabelChanged(string label)
        => StateHasChanged();

    /// <summary>
    /// Called when the executors are updated
    /// </summary>
    /// <param name="info">the updated executgors</param>
    private void OnExecutorsUpdated(List<FlowExecutorInfoMinified> info)
    {
        _executors = info ?? [];
        StateHasChanged();
    }

    /// <summary>
    /// Gets the label to toggle the paused state
    /// </summary>
    private string lblTogglePause => PausedService.IsPaused ? lblResume : lblPause;

    /// <summary>
    /// Toggles the paused state
    /// </summary>
    private void TogglePause()
        => _ = PausedService.Toggle();
    
    /// <summary>
    /// Disposes of the component
    /// </summary>
    public void Dispose()
    {
        feService.Runner.RunnerInfoUpdated -= OnExecutorsUpdated;
        feService.Dashboard.UpdateInfoUpdated -= OnUpdatesUpdateInfo;
        feService.Dashboard.SystemInfoUpdated -= OnSystemInfoUpdated;
        PausedService.OnPausedLabelChanged -= OnPausedLabelChanged;
    }
}