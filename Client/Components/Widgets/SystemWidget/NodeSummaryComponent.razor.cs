using FileFlows.Client.Services.Frontend;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Widgets;

/// <summary>
/// Node summary component
/// </summary>
public partial class NodeSummaryComponent : ComponentBase, IDisposable
{
    /// <summary>
    /// Gets or sets the front end service
    /// </summary>
    [Inject] public FrontendService feService { get; set; }
    /// <summary>
    /// The data
    /// </summary>
    private List<NodeStatusSummary> Data = new();

    /// <summary>
    /// Translation strings
    /// </summary>
    private string lblSchedule, lblOperatingSystem, lblArchitecture, lblMemory, lblStatus, lblInternalProcessingNode, lblRunners, lblPriority;
    
    /// <inheritdoc />
    protected override void OnInitialized()
    {
        lblSchedule = Translater.Instant("Labels.Schedule");
        lblOperatingSystem = Translater.Instant("Labels.OperatingSystem");
        lblArchitecture = Translater.Instant("Labels.Architecture");
        lblMemory = Translater.Instant("Labels.Memory");
        lblStatus = Translater.Instant("Labels.Status");
        lblRunners = Translater.Instant("Pages.Nodes.Labels.Runners");
        lblPriority = Translater.Instant("Pages.ProcessingNode.Fields.Priority");
        lblInternalProcessingNode = Translater.Instant("Labels.InternalProcessingNode");
        Data = feService.Node.NodeStatusSummaries;
        feService.Node.NodeStatusUpdated += OnNodeStatusSummaryUpdated;
    }

    /// <summary>
    /// Raised when the node status summaries are updated
    /// </summary>
    /// <param name="data">the updated data</param>
    private void OnNodeStatusSummaryUpdated(List<NodeStatusSummary> data)
    {
        Data = data;
        StateHasChanged();
    }

    /// <summary>
    /// Disposes of the component
    /// </summary>
    public void Dispose()
    {
        feService.Node.NodeStatusUpdated-= OnNodeStatusSummaryUpdated;
    }

    /// <summary>
    /// Gets the default icon
    /// </summary>
    /// <param name="item">the node</param>
    /// <returns>the default icon</returns>
    private string GetDefaultIcon(NodeStatusSummary item)
    {
        if (item.OperatingSystem == OperatingSystemType.Mac)
            return "svg:apple";
        if (item.OperatingSystem == OperatingSystemType.Docker)
            return "svg:docker";
        if (item.HardwareInfo?.OperatingSystem?.Contains("ubuntu", StringComparison.InvariantCultureIgnoreCase) == true)
            return "svg:distros/ubuntu";
        if (item.HardwareInfo?.OperatingSystem?.Contains("fedora", StringComparison.InvariantCultureIgnoreCase) == true)
            return "svg:distros/fedora";
        if (item.HardwareInfo?.OperatingSystem?.Contains("pop!", StringComparison.InvariantCultureIgnoreCase) == true)
            return "svg:distros/popos";
        if (item.HardwareInfo?.OperatingSystem?.Contains("debian", StringComparison.InvariantCultureIgnoreCase) == true)
            return "svg:distros/debian";
        if (item.HardwareInfo?.OperatingSystem?.Contains("arch", StringComparison.InvariantCultureIgnoreCase) == true)
            return "svg:distros/arch";
        if (item.OperatingSystem == OperatingSystemType.Windows)
            return "svg:windows";
        if (item.OperatingSystem == OperatingSystemType.Linux)
            return "svg:linux";
        return "fas fa-desktop";
    }
}