using FileFlows.Client.Helpers;
using FileFlows.Client.Services.Frontend;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Widgets;

public partial class LibrarySavingsComponent : ComponentBase, IDisposable
{
    /// <summary>
    /// Gets or sets the frontend service
    /// </summary>
    [Inject] public FrontendService feService { get; set; }

    private double TotalPercent = 0;
    private string TotalSavings = string.Empty;
    private string OriginalSize = string.Empty;
    
    private List<StorageSavedData> TotalData = new();
    private List<StorageSavedData> MonthData = new();

    private string lblNoSavings;

    public List<StorageSavedData> Data => Mode == 0 ? MonthData : TotalData;
    
    /// <summary>
    /// Gets the mode
    /// </summary>
    private int _Mode = 0;
    /// <summary>
    /// Gets or sets the selected mode
    /// </summary>
    [Parameter] public int Mode
    {
        get => _Mode;
        set => _Mode = value;
    }

    /// <summary>
    /// Sets the mode
    /// </summary>
    /// <param name="mode"></param>
    public void SetMode(int mode)
    {
        _Mode = mode;
        SetValues();
        StateHasChanged();
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        lblNoSavings = Translater.Instant("Pages.Dashboard.Widgets.LibrarySavings.NoSavings");
        //feService.Dashboard.FileOverviewDataUpdated += OnFileOverviewUpdated;

        TotalData = feService.Dashboard.StorageSavedTotalData;
        MonthData = feService.Dashboard.StorageSavedMonthData;
        SetValues();
    }

    // /// <summary>
    // /// Called when the file overview is updated
    // /// </summary>
    // /// <param name="data">the updated data</param>
    // private void OnFileOverviewUpdated(FileOverviewData data)
    // {
    //     _ = InvokeAsync(async () =>
    //     {
    //         TotalData = await feService.GetLibrarySavingsAllData();
    //         MonthData = await feService.GetLibrarySavingsMonthData();
    //         SetValues();
    //         StateHasChanged();
    //     });
    // }

    /// <summary>
    /// Sets the value based on the data
    /// </summary>
    private void SetValues()
    {
        var data = Data;
        if (data == null || data.Count == 0)
        {
            TotalSavings = lblNoSavings;
            OriginalSize = string.Empty;
            TotalPercent = 0;
            return;
        }
        
        long original = data.Sum(x => x.OriginalSize);
        long final = data.Sum(x => x.FinalSize);
        long saved = original - final;
        TotalPercent = Math.Round(saved * 100f / original, 1);
        TotalSavings = final > original ? lblNoSavings : FileSizeFormatter.FormatSize(saved, 1);
        OriginalSize = FileSizeFormatter.FormatSize(original, 1);
    }

    /// <summary>
    /// Disposes of the object
    /// </summary>
    public void Dispose()
    {
        //feService.Dashboard.FileOverviewDataUpdated -= OnFileOverviewUpdated;
    }
}