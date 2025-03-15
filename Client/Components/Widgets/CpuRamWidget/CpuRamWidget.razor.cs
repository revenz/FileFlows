using FileFlows.Client.Helpers;
using FileFlows.Client.Services.Frontend;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Widgets;

/// <summary>
/// CPU/RAM Widget
/// </summary>
public partial class CpuRamWidget : ComponentBase, IDisposable
{
    /// <summary>
    /// Gets or sets the front end service
    /// </summary>
    [Inject] public FrontendService feService { get; set; }
    
    private int _Mode = 0;
    private string Color = "yellow";
    private string Label = "CPU";
    private double CpuValue = 0;
    private double RamValue = 0;
    private double CpuMax = 0;
    private double RamMax = 0;
    private string Max;
    private string Value;
    private double[] Data = [];//[10, 20, 30, 20, 15, 16, 27, 45.34, 41.2, 38.2];
    private double[] CpuValues = [];
    private double[] MemoryValues = [];
    /// <summary>
    /// Gets or sets the Local Storage instance
    /// </summary>
    [Inject] private FFLocalStorageService LocalStorage { get; set; }
    /// <summary>
    /// The key used to store the selected mode in local storage
    /// </summary>
    private const string LocalStorageKey = "CpuRamWidget";
    /// <summary>
    /// Gets or sets the selected mode
    /// </summary>
    private int Mode
    {
        get => _Mode;
        set
        {
            _Mode = value;
            _ = LocalStorage.SetItemAsync(LocalStorageKey, value);
            SetValues();

            StateHasChanged();
        }
    }

    /// <summary>
    /// Gets if in the CPU mode
    /// </summary>
    private bool CpuMode => _Mode == 0;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        feService.Dashboard.SystemInfoUpdated += OnSystemInfoUpdated;
        _Mode = Math.Clamp(await LocalStorage.GetItemAsync<int>(LocalStorageKey), 0, 1);
        OnSystemInfoUpdated(feService.Dashboard.CurrentSystemInfo);
    }

    /// <summary>
    /// Handles the system info updated event
    /// </summary>
    /// <param name="info">the system info</param>
    private void OnSystemInfoUpdated(SystemInfo info)
    {
        if (info.CpuUsage.Length > 0)
        {
            CpuValue = info.CpuUsage.Last();
            CpuValues = info.CpuUsage.Select(x => (double)x).ToArray();
            CpuMax = info.CpuUsage.Max();
        }

        if (info.MemoryUsage.Length > 0)
        {
            RamValue = info.MemoryUsage.Last();
            MemoryValues = info.MemoryUsage.Select(x => (double)x).ToArray();
            RamMax = info.MemoryUsage.Max();
        }

        SetValues();

        StateHasChanged();
    }

    private void SetValues()
    {
        
        if (CpuMode)
        {
            Color = "yellow";
            Label = "CPU";
            Value = $"{CpuValue:F1}%";
            Max = Translater.Instant("Pages.Dashboard.Widgets.CpuRam.Peak", new { num = $"{CpuMax:F1}%" });
            Data = CpuValues.ToArray();
        }
        else
        {
            Color = "purple";
            Label = "RAM";
            Value = FileSizeFormatter.FormatSize((long)RamValue, 1);
            Max = Translater.Instant("Pages.Dashboard.Widgets.CpuRam.Peak", new { num = FileSizeFormatter.FormatSize((long)RamMax, 1) });
            Data = MemoryValues.ToArray();
        }
    }

    /// <summary>
    /// Disposes of the object
    /// </summary>
    public void Dispose()
    {
        feService.Dashboard.SystemInfoUpdated -= OnSystemInfoUpdated;
    }
}