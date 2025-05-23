using FileFlows.Client.Components.Dialogs;
using FileFlows.Client.Components.Editors;
using FileFlows.Client.Services.Frontend;

namespace FileFlows.Client.Services;

/// <summary>
/// Service that monitors the system's paused status.
/// </summary>
public interface IPausedService
{
    /// <summary>
    /// Gets the label representing the paused state
    /// </summary>
    string PausedLabel { get;  }
    /// <summary>
    /// Gets if the system is paused
    /// </summary>
    bool IsPaused { get; }
    /// <summary>
    /// Gets if the system is paused indefinitely
    /// </summary>
    bool PausedIndefinitely { get; }
    /// <summary>
    /// Toggles the paused system
    /// </summary>
    Task Toggle();
    /// <summary>
    /// Pauses the system.
    /// </summary>
    Task Pause();

    /// <summary>
    /// Resumes the system.
    /// </summary>
    Task Resume();

    /// <summary>
    /// Occurs when the paused label changes.
    /// </summary>
    event Action<string>? OnPausedLabelChanged;

    /// <summary>
    /// Occurs when the system is paused.
    /// </summary>
    event Action? OnPaused;

    /// <summary>
    /// Occurs when the system resumes from a paused state.
    /// </summary>
    event Action? OnResume;
}

/// <summary>
/// Service that monitors the systems paused status
/// </summary>
public class PausedService : IPausedService, IDisposable
{
    private SystemInfo SystemInfo = new ();
    private TimeSpan TimeDiff;
    private string lblPause, lblPaused, lblPausedWithTime;
    
    /// <summary>
    /// The confirm service
    /// </summary>
    private readonly MessageService _message;
    
    /// <summary>
    /// Gets or sets the modal service
    /// </summary>
    private IModalService ModalService { get; set; }
    
    /// <inheritdoc />
    public string PausedLabel { get; private set; }
    
    /// <summary>
    /// Gets if the system is paused indefinitely
    /// </summary>
    public bool PausedIndefinitely { get; private set; }

    /// <inheritdoc />
    public bool IsPaused => SystemInfo?.IsPaused == true;

    private bool translated = false;
    private FrontendService feService;
    /// <summary>
    /// Constructs an instance of the paused worker
    /// </summary>
    public PausedService(FrontendService feService, IModalService modalService, MessageService messageService)
    {
        this.feService = feService;
        ModalService = modalService;
        _message = messageService;
        var bkgTask = new BackgroundTask(TimeSpan.FromMilliseconds(1_000), () => _ = DoWork());
        bkgTask.Start();
        SystemInfo = feService.Dashboard.CurrentSystemInfo;
        feService.Dashboard.SystemInfoUpdated += OnSystemInfoUpdated;
        
        // translated a little later on
        lblPause = "Pause Processing";
        lblPaused = "Resume Processing";
        lblPausedWithTime = "Pause for";
        PausedLabel = IsPaused ? lblPaused : lblPause;
        PausedIndefinitely = IsPaused;
    }

    private void OnSystemInfoUpdated(SystemInfo info)
    {
        TimeDiff = DateTime.UtcNow - info.CurrentTime;
        SystemInfo = info;
    }

    /// <summary>
    /// Do the work to update the paused state
    /// </summary>
    private Task DoWork()
    {
        if (translated == false && Translater.InitDone)
        {
            translated = true;
            lblPause = Translater.Instant("Labels.Pause");
            lblPaused = Translater.Instant("Labels.Paused");
            lblPausedWithTime = Translater.Instant("Labels.PausedWithTime");
        }

        UpdateTime();
        OnPausedLabelChanged?.Invoke(PausedLabel);
        return Task.CompletedTask;
    }
    /// <summary>
    /// Update the Pause Label
    /// </summary>
    private void UpdateTime()
    {
        if (SystemInfo.IsPaused == false)
        {
            PausedLabel = lblPause;
            PausedIndefinitely = false;
            return;
        }

        if (SystemInfo.PausedUntil > SystemInfo.CurrentTime.AddYears(1))
        {
            PausedLabel = lblPaused;
            PausedIndefinitely = true;
            return;
        }
        
        PausedIndefinitely = false;
        var pausedToLocal = SystemInfo.PausedUntil.Add(TimeDiff);
        var time = pausedToLocal.Subtract(DateTime.UtcNow);
        PausedLabel = lblPausedWithTime + " " + time.ToString(@"h\:mm\:ss");
    }
    
    Task<RequestResult<SystemInfo>> GetSystemInfo() => HttpHelper.Get<SystemInfo>("/api/system/info");

    /// <inheritdoc />
    public async Task Toggle()
    {
        bool paused = SystemInfo.IsPaused;
        int duration = 0;
        bool abort = false;
        if (paused == false)
        {
            var result = await ModalService.ShowModal<PausePrompt, PauseResult>(new ModalEditorOptions());
            if (result.IsFailed)
                return;
            duration = result.Value.Duration;
            abort = result.Value.Abort;
        }

        await SetPausedState(duration, abort);
    }

    /// <inheritdoc />
    public async Task Pause()
    {
        var result = await ModalService.ShowModal<PausePrompt, PauseResult>(new ModalEditorOptions());
        if (result.Success(out var value))
            await SetPausedState(value.Duration, value.Abort);
    }

    /// <inheritdoc />
    public Task Resume()
        => SetPausedState(0, false);

    /// <summary>
    /// Pauses the system for the given amount of seconds
    /// </summary>
    /// <param name="duration">the duration in seconds</param>
    /// <param name="abort">if the files curently processing should be aborted</param>
    private async Task SetPausedState(int duration, bool abort)
    {
        if (duration == 0)
        {
            if (await _message.Confirm("Dialogs.ResumeDialog.Title", "Dialogs.ResumeDialog.Message") == false)
                return;
        }
        await HttpHelper.Post($"/api/system/pause?duration={duration}&abort={abort}");
        var systemInfoResult = await GetSystemInfo();
        if (systemInfoResult.Success)
        {
            TimeDiff = DateTime.UtcNow - systemInfoResult.Data.CurrentTime;
            SystemInfo = systemInfoResult.Data;
            this.UpdateTime();
            
            if(duration == 0)
                OnResume?.Invoke();
            else
                OnPaused?.Invoke();
            OnPausedLabelChanged?.Invoke(this.PausedLabel);

        }
    }

    /// <inheritdoc />
    public event Action<string>? OnPausedLabelChanged;
    /// <inheritdoc />
    public event Action? OnPaused;
    /// <inheritdoc />
    public event Action? OnResume;

    /// <summary>
    /// Disposes of the service
    /// </summary>
    public void Dispose()
    {
        feService.Dashboard.SystemInfoUpdated -= OnSystemInfoUpdated;
    }
}