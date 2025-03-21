using System.Text.Json.Serialization;
using FileFlows.Client.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client.Pages;

/// <summary>
/// Page for reports
/// </summary>
public partial class Reports : ListPage<Guid, ReportUiModel>
{
    /// <summary>
    /// Gets or sets the report form editor component
    /// </summary>
    private Editor ReportFormEditor { get; set; }
    
    /// <inheritdoc />
    public override string ApiUrl => "/api/report";

    protected override void OnInitialized()
    {
        Profile = feService.Profile.Profile;
        base.OnInitialized(false);
        Data = feService.Report.ReportDefinitions.Select(x => new ReportUiModel()
        {
            Uid = x.Uid,
            Icon = x.Icon,
            Name = Translater.Instant($"Reports.{x.Type}.Name"),
            Description = Translater.Instant($"Reports.{x.Type}.Description")
        }).OrderBy(x => x.Name.ToLowerInvariant()).ToList();
    }

    /// <summary>
    /// Launches the report
    /// </summary>
    /// <param name="rd">the report definition</param>
    private Task Launch(ReportUiModel rd)
        => Edit(rd);

    /// <inheritdoc />
    public override Task<bool> Edit(ReportUiModel rd)
    {
        NavigationManager.NavigateTo($"/report/{rd.Uid}");
        return Task.FromResult(true);
    }
}

/// <summary>
/// Report UI Model
/// </summary>
public class ReportUiModel : ReportDefinition
{
    /// <summary>
    /// Gets or sets the name of the report
    /// </summary>
    [JsonIgnore]
    public string Name { get; set; }
    /// <summary>
    /// Gets or sets the description of the report
    /// </summary>
    [JsonIgnore]
    public string Description { get; set; }
}