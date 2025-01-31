using FileFlows.WebServer.Authentication;
using Microsoft.AspNetCore.Mvc;
using FileFlows.Services;
using FileFlows.ServerShared.Models.StatisticModels;
using FileFlows.Shared.Models;

namespace FileFlows.WebServer.Controllers;

/// <summary>
/// Status controller
/// </summary>
[Route("/api/statistics")]
[FileFlowsAuthorize]
public class StatisticsController : Controller
{
    /// <summary>
    /// Records a running total statistic
    /// </summary>
    /// <param name="name">the name of the statistic</param>
    /// <param name="value">the value of the statistic</param>
    [HttpPost("record-running-total")]
    public Task RecordRunningTotals([FromQuery] string name, [FromQuery] string value)
        => new StatisticService().RecordRunningTotal(name, value);
    
    /// <summary>
    /// Records a average 
    /// </summary>
    /// <param name="name">the name of the statistic</param>
    /// <param name="value">the value of the statistic</param>
    [HttpPost("record-average")]
    public Task RecordAverage([FromQuery] string name, [FromQuery] int value)
        => new StatisticService().RecordAverage(name, value);

    /// <summary>
    /// Gets statistics by name
    /// </summary>
    /// <returns>the matching statistics</returns>
    [HttpGet("running-totals/{name}")]
    public Dictionary<string, long> GetRunningTotals([FromRoute] string name)
        => new StatisticService().GetRunningTotals(name);


    /// <summary>
    /// Gets average statistics by name
    /// </summary>
    /// <returns>the matching statistics</returns>
    [HttpGet("average/{name}")]
    public Dictionary<int, int> GetAverage([FromRoute] string name)
        => new StatisticService().GetAverage(name);

    /// <summary>
    /// Gets storage saved
    /// </summary>
    /// <returns>the storage saved</returns>
    [HttpGet("storage-saved-raw")]
    public object GetStorageSavedRaw([FromQuery] int days)
        => new StatisticService().GetStorageSaved(days == 31 ? Globals.STAT_STORAGE_SAVED_MONTH : Globals.STAT_STORAGE_SAVED);
    
    /// <summary>
    /// Gets storage saved
    /// </summary>
    /// <returns>the storage saved</returns>
    [HttpGet("storage-saved")]
    public object GetStorageSaved()
    {
        var data = new StatisticService().GetStorageSaved();
        data = data.OrderByDescending(x => x.OriginalSize - x.FinalSize).ToList();
        const int MAX = 5;
        if (data.Count > MAX)
        {
            var other = new StorageSavedData
            {
                Library = "Other",
                TotalFiles = data.Skip(MAX - 1).Sum(x => x.TotalFiles),
                FinalSize = data.Skip(MAX - 1).Sum(x => x.FinalSize),
                OriginalSize = data.Skip(MAX - 1).Sum(x => x.OriginalSize)
            };
            data = data.Take(MAX - 1).ToList();
            data.Add(other);
        }
        var total = new StorageSavedData
        {
            Library = "Total",
            TotalFiles = data.Sum(x => x.TotalFiles),
            FinalSize = data.Sum(x => x.FinalSize),
            OriginalSize = data.Sum(x => x.OriginalSize)
        };
        data.Add(total);
        
        return new
        {
            series = new object[]
            {
                new { name = "Final Size", data = data.Select(x => x.FinalSize).ToArray() },
                new { name = "Savings", data = data.Select(x =>
                {
                    var change = x.OriginalSize - x.FinalSize;
                    if (change > 0)
                        return change;
                    return 0;
                }).ToArray() },
                new { name = "Increase", data = data.Select(x =>
                {
                    var change = x.OriginalSize - x.FinalSize;
                    if (change > 0)
                        return 0;
                    return change * -1;
                }).ToArray() }
            },
            labels = data.Select(x => x.Library.Replace("###TOTAL###", "Total")).ToArray(),
            items = data.Select(x => x.TotalFiles).ToArray()
        };
    }

    /// <summary>
    /// Clears statistics for
    /// </summary>
    /// <param name="name">Optional. The name for which DbStatistics should be cleared.</param>
    /// <returns>the response</returns>
    [HttpPost("clear")]
    public async Task<IActionResult> Clear([FromQuery] string? name = null)
    {
        try
        {
            await new StatisticService().Clear(name);
            return Ok();
        }
        catch (Exception ex)
        {
            Logger.Instance.WLog("Failed clearing statistics: " + ex.Message);
            return BadRequest();
        }
    }
}

