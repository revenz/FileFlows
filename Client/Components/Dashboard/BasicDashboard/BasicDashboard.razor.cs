using System.Timers;
using FileFlows.Client.Components.Dialogs;
using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace FileFlows.Client.Components.Dashboard;

/// <summary>
/// Basic/legacy dashboard 
/// </summary>
public partial class BasicDashboard
{
    private string lblLog, lblCancel, lblCurrentStep, lblNode, lblFile, lblProcessingTime, lblWorkingFile, lblLibrary;

    const string ApiUrl = "/api/worker";
    private bool Refreshing = false;
    public readonly List<FlowExecutorInfo> Workers = new List<FlowExecutorInfo>();

    public readonly List<LibraryFile> Upcoming = new List<LibraryFile>();
    private bool _needsRendering = false;

    private ConfigurationStatus ConfiguredStatus = ConfigurationStatus.Flows | ConfigurationStatus.Libraries;
    [Inject] public IJSRuntime jSRuntime { get; set; }
    [CascadingParameter] public Blocker Blocker { get; set; }
    [CascadingParameter] Editor Editor { get; set; }
    
    [CascadingParameter] private Pages.Dashboard Dashboard { get; set; }

    public IJSObjectReference jsFunctions;

    private string lblOverall, lblCurrent, lblPauseLabel, lblAddWidget;
    private Timer AutoRefreshTimer;

    private SystemInfo SystemInfo = new SystemInfo();
    public delegate void Disposing();
    public event Disposing OnDisposing;

    private List<ListOption> Dashboards;
    private Guid ActiveDashboardUid;
    private readonly List<WidgetUiModel> Widgets = new List<WidgetUiModel>();
    private IJSObjectReference jsCharts;
    
    protected override async Task OnInitializedAsync()
    {
        Logger.Instance.ILog("basic dashboard!");

        // Setup auto refresh timer
        // Don't auto reset as that results in multiple requests pending if they are being returned slow
        // Instead, Start() is called again once the previous refresh finishes
        AutoRefreshTimer = new Timer();
        AutoRefreshTimer.Elapsed += AutoRefreshTimerElapsed;
        AutoRefreshTimer.Interval = 5_000;
        AutoRefreshTimer.AutoReset = false;
        AutoRefreshTimer.Start();

#if (DEMO)
        ConfiguredStatus = ConfigurationStatus.Flows | ConfigurationStatus.Libraries;
#else
        ConfiguredStatus = App.Instance.FileFlowsSystem.ConfigurationStatus;
#endif
        lblOverall = Translater.Instant("Pages.Dashboard.Fields.Overall");
        lblCurrent = Translater.Instant("Pages.Dashboard.Fields.Current");
        lblAddWidget = Translater.Instant("Pages.Dashboard.Labels.AddWidget");
        
        lblLog = Translater.Instant("Labels.Log");
        lblCancel = Translater.Instant("Labels.Cancel");
        lblCurrentStep = Translater.Instant("Pages.Dashboard.Fields.CurrentStep");
        lblNode= Translater.Instant("Pages.Dashboard.Fields.Node");
        lblFile = Translater.Instant("Pages.Dashboard.Fields.File");
        lblProcessingTime = Translater.Instant("Pages.Dashboard.Fields.ProcessingTime");
        lblLibrary = Translater.Instant("Pages.Dashboard.Fields.Library");
        lblWorkingFile = Translater.Instant("Pages.Dashboard.Fields.WorkingFile");
        
        jsCharts = await jSRuntime.InvokeAsync<IJSObjectReference>("import", $"./scripts/Charts/FFChart.js");
        
        await GetJsFunctionObject();
        await this.Refresh();
    }

    private System.Threading.Mutex mutexJsFunctions = new ();
    public async Task<IJSObjectReference> GetJsFunctionObject()
    {
        if (jsFunctions != null)
            return jsFunctions;
        mutexJsFunctions.WaitOne();
        if (jsFunctions != null)
            return jsFunctions; // in case was fetched while mutex was locked
        try
        {
            jsFunctions = await jSRuntime.InvokeAsync<IJSObjectReference>("import", "./scripts/Dashboard.js");
            return jsFunctions;
        }
        finally
        {
            mutexJsFunctions.ReleaseMutex();
        }
    }

    /// <summary>
    /// Gets if this is the active dashboard
    /// </summary>
    public bool IsActive => Dashboard.ActiveDashboardUid == Guid.Empty || App.Instance.IsMobile;

    public void Dispose()
    {
        OnDisposing?.Invoke();

        _ = jsCharts?.InvokeVoidAsync("disposeAll");
        
        if (jsFunctions != null)
        {
            try
            {
                _ = jsFunctions.InvokeVoidAsync("DestroyAllCharts", this.Workers);
            }
            catch (Exception) { }
        }
        if (AutoRefreshTimer != null)
        {
            AutoRefreshTimer.Stop();
            AutoRefreshTimer.Elapsed -= AutoRefreshTimerElapsed;
            AutoRefreshTimer.Dispose();
            AutoRefreshTimer = null;
        }
    }

    void AutoRefreshTimerElapsed(object sender, ElapsedEventArgs e)
    {
        _ = Refresh();
    }


    async Task Refresh()
    {
        if (Refreshing || IsActive == false) 
            return;
        Refreshing = true;
        try
        {
            RequestResult<List<FlowExecutorInfo>> result = await GetData();
            if (result.Success)
            {
                this.Workers.Clear();
                if (result.Data.Any())
                {
                    foreach(var worker in result.Data)
                    {
                        if (worker.NodeName == "FileFlowsServer")
                            worker.NodeName = Translater.Instant("Pages.Nodes.Labels.FileFlowsServer");
                    }
                    this.Workers.AddRange(result.Data);
                }
                await WaitForRender();
                try
                {
                    await jsFunctions.InvokeVoidAsync("InitChart", this.Workers, this.lblOverall, this.lblCurrent);
                }
                catch(Exception) { }
            }
        }
        catch (Exception)
        {
        }
        finally
        {
            Refreshing = false;
            AutoRefreshTimer.Start();
        }
    }

    async Task<RequestResult<List<FlowExecutorInfo>>> GetData()
    {
#if (DEMO)
        return new RequestResult<List<FlowExecutorInfo>>
        {
            Success = true,
            Data = new List<FlowExecutorInfo>
            {
                new FlowExecutorInfo
                {
                    LibraryFile = new LibraryFile { Name = "DemoFile.mkv" },
                    LibraryPath = @"C:\Videos",
                    CurrentPart = 1,
                    CurrentPartName = "Curren Flow Part",
                    CurrentPartPercent = 50,
                    LastUpdate = DateTime.Now,
                    Log = "Test Log",
                    NodeName = "Remote Processing Node",
                    NodeUid = Guid.NewGuid(),
                    Library = new ObjectReference
                    {
                        Name = "Demo Library",
                        Uid = Guid.NewGuid()
                    },
                    RelativeFile = "DemoFile.mkv",
                    StartedAt = DateTime.Now.AddMinutes(-1),
                    TotalParts = 5,
                    WorkingFile = "tempfile.mkv",
                    Uid = Guid.NewGuid()
                }
            }
        };
#else
        return await HttpHelper.Get<List<FlowExecutorInfo>>(ApiUrl);
#endif
        
    }


    private async Task WaitForRender()
    {
        _needsRendering = true;
        StateHasChanged();
        while (_needsRendering)
        {
            await Task.Delay(50);
        }
    }

    protected override void OnAfterRender(bool firstRender)
    {
        _needsRendering = false;
    }


    private async Task LogClicked(FlowExecutorInfo worker)
    {
        Blocker.Show();
        string log = string.Empty;
        string url = $"{ApiUrl}/{worker.LibraryFile.Uid}/log?lineCount=200";
        try
        {
            var logResult = await GetLog(url);
            if (logResult.Success == false || string.IsNullOrEmpty(logResult.Data))
            {
                Toast.ShowError( Translater.Instant("Pages.Dashboard.ErrorMessages.LogFailed"));
                return;
            }
            log = logResult.Data;
        }
        finally
        {
            Blocker.Hide();
        }

        List<ElementField> fields = new List<ElementField>();
        fields.Add(new ElementField
        {
            InputType = FormInputType.LogView,
            Name = "Log",
            Parameters = new Dictionary<string, object> {
                { nameof(Components.Inputs.InputLogView.RefreshUrl), url },
                { nameof(Components.Inputs.InputLogView.RefreshSeconds), 3 },
            }
        });

        await Editor.Open(new()
        {
            TypeName = "Pages.Dashboard", Title = worker.LibraryFile.Name, Fields = fields, Model = new { Log = log },
            Large = true, ReadOnly = true
        });
    }

    private async Task<RequestResult<string>> GetLog(string url)
    {
#if (DEMO)
        return new RequestResult<string>
        {
            Success = true,
            Data = @"2021-11-27 11:46:15.0658 - Debug -> Executing part:
2021-11-27 11:46:15.1414 - Debug -> node: VideoFile
2021-11-27 11:46:15.8442 - Info -> Video Information:
ffmpeg version 4.1.8 Copyright (c) 2000-2021 the FFmpeg developers
built with gcc 9 (Ubuntu 9.3.0-17ubuntu1~20.04)
configuration: --disable-debug --disable-doc --disable-ffplay --enable-shared --enable-avresample --enable-libopencore-amrnb --enable-libopencore-amrwb --enable-gpl --enable-libass --enable-fontconfig --enable-libfreetype --enable-libvidstab --enable-libmp3lame --enable-libopus --enable-libtheora --enable-libvorbis --enable-libvpx --enable-libwebp --enable-libxcb --enable-libx265 --enable-libxvid --enable-libx264 --enable-nonfree --enable-openssl --enable-libfdk_aac --enable-postproc --enable-small --enable-version3 --enable-libbluray --enable-libzmq --extra-libs=-ldl --prefix=/opt/ffmpeg --enable-libopenjpeg --enable-libkvazaar --enable-libaom --extra-libs=-lpthread --enable-libsrt --enable-nvenc --enable-cuda --enable-cuvid --enable-libnpp --extra-cflags='-I/opt/ffmpeg/include -I/opt/ffmpeg/include/ffnvcodec -I/usr/local/cuda/include/' --extra-ldflags='-L/opt/ffmpeg/lib -L/usr/local/cuda/lib64 -L/usr/local/cuda/lib32/'
libavutil      56. 22.100 / 56. 22.100
libavcodec     58. 35.100 / 58. 35.100
libavformat    58. 20.100 / 58. 20.100
libavdevice    58.  5.100 / 58.  5.100
libavfilter     7. 40.101 /  7. 40.101
libavresample   4.  0.  0 /  4.  0.  0
libswscale      5.  3.100 /  5.  3.100
libswresample   3.  3.100 /  3.  3.100
libpostproc    55.  3.100 / 55.  3.100"
        };
#endif
        return await HttpHelper.Get<string>(url);
    }

    private async Task CancelClicked(FlowExecutorInfo worker)
    {
        if (await Confirm.Show("Labels.Cancel",
            Translater.Instant("Pages.Dashboard.Messages.CancelMessage", worker)) == false)
            return; // rejected the confirm

#if (!DEMO)
        Blocker.Show();
        try
        {
            await HttpHelper.Delete($"{ApiUrl}/{worker.Uid}?libraryFileUid={worker.LibraryFile.Uid}");
            await Task.Delay(1000);
            await Refresh();
        }
        finally
        {
            Blocker.Hide();
        }
#endif
    }
}