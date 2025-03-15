using FileFlows.Client.Helpers;
using FileFlows.Client.Services.Frontend;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Widgets;

/// <summary>
/// File Overview Widget
/// </summary>
public partial class FileOverviewWidget : ComponentBase, IDisposable
{
    /// <summary>
    /// Gets or sets the frontend service
    /// </summary>
    [Inject] public FrontendService feService { get; set; }
    
    /// <summary>
    /// Gets if this is for the files processed, or the storage saved
    /// </summary>
    [Parameter] public bool IsFilesProcessed { get; set; }
    /// <summary>
    /// Gets or sets the Local Storage instance
    /// </summary>
    [Inject] private FFLocalStorageService LocalStorage { get; set; }
    
    /// <summary>
    /// The key used to store the selected mode in local storage
    /// </summary>
    private string LocalStorageKey;
    
    private string lblWeek, lblMonth;
    
    private int _Mode = 0;
    private string Color = "green";
    private string Label = string.Empty;
    private string Icon = string.Empty;
    private string Total;
    private string Average;
    private double[] Data = [];
    private FileOverviewData? CurrentData;
    /// <summary>
    /// Gets or sets the selected mode
    /// </summary>
    private int Mode
    {
        get => _Mode;
        set
        {
            _Mode = value;
            if(LocalStorageKey != null)
                _ = LocalStorage.SetItemAsync(LocalStorageKey, value);
            SetValues();

            StateHasChanged();
        }
    }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        lblWeek = Translater.Instant("Labels.WeekShort");
        lblMonth = Translater.Instant("Labels.MonthShort");
        Color = IsFilesProcessed ? "green" : "blue";
        LocalStorageKey = IsFilesProcessed ? "FileOverviewWidgetFiles-FilesProcessed" : "FileOverviewWidgetStorage-StorageSaved";
        _Mode = Math.Clamp(await LocalStorage.GetItemAsync<int>(LocalStorageKey), 0, 1);
        Label = Translater.Instant("Pages.Dashboard.Widgets.FilesOverview." + (IsFilesProcessed ? "FilesProcessed" : "StorageSaved"));
        Icon = IsFilesProcessed ? "far fa-checked-circle" : "fas fa-hdd";
        
        CurrentData = feService.Dashboard.CurrentFileOverData;
        feService.Dashboard.FileOverviewDataUpdated += OnFileOverviewUpdated;
        if (feService != null)
            SetValues();
    }

    /// <summary>
    /// Called when the file overview is updated
    /// </summary>
    /// <param name="data">the updated data</param>
    private void OnFileOverviewUpdated(FileOverviewData data)
    {
        CurrentData = data;
        SetValues();
        StateHasChanged();
    }

    /// <summary>
    /// Sets the value based on the data
    /// </summary>
    private void SetValues()
    {
        var dataset = Mode switch
        {
            0 => CurrentData.Last7Days,
            1 => CurrentData.Last31Days,
            _ => CurrentData.Last24Hours
        };

        if (dataset.Count == 0)
            return;
        
        if (IsFilesProcessed)
        {
            int total = dataset.Sum(x => x.Value.FileCount);
            Total = total.ToString("N0");
            // average
            switch (Mode)
            {
                case 0:
                    Average = Translater.Instant("Pages.Dashboard.Widgets.FilesOverview.PerDay",
                        new { num = Math.Round(total / 7d) });
                    break;
                case 1:
                    Average = Translater.Instant("Pages.Dashboard.Widgets.FilesOverview.PerDay",
                        new { num = Math.Round(total / 31d) });
                    break;
                default:
                    Average = Translater.Instant("Pages.Dashboard.Widgets.FilesOverview.PerHour",
                        new { num = Math.Round(total / 24d) });
                    break;
            }
            Data = dataset.Select(x => x.Value.FileCount).Select(x => (double)x).ToArray();
        }
        else
        {
            long total = dataset.Sum(x => x.Value.StorageSaved);
            Total = FileSizeFormatter.FormatSize(Math.Max(0, total), 1);
            // average
            switch (Mode)
            {
                case 0:
                    Average = Translater.Instant("Pages.Dashboard.Widgets.FilesOverview.PerDay",
                        new { num = FileSizeFormatter.FormatSize(Math.Max(0, (long)Math.Round(total / 7d)), 1) });
                    break;
                case 1:
                    Average = Translater.Instant("Pages.Dashboard.Widgets.FilesOverview.PerDay",
                        new { num = FileSizeFormatter.FormatSize(Math.Max(0, (long)Math.Round(total / 31d)), 1) });
                    break;
                default:
                    Average = Translater.Instant("Pages.Dashboard.Widgets.FilesOverview.PerHour",
                        new { num = FileSizeFormatter.FormatSize(Math.Max(0, (long)Math.Round(total / 24d)), 1) });
                    break;
            }
            Data = dataset.Select(x => x.Value.StorageSaved).Select(x => (double)x).ToArray();
        }
    }

    /// <summary>
    /// Disposes of the object
    /// </summary>
    public void Dispose()
    {
        feService.Dashboard.FileOverviewDataUpdated -= OnFileOverviewUpdated;
    }
}