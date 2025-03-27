using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components;

/// <summary>
/// Processing node
/// </summary>
public partial class ProcessingNodeElement : ComponentBase
{
    /// <summary>
    /// Gets or sets the processing node
    /// </summary>
    [Parameter] public NodeStatusSummary Node { get; set; }
    
    /// <summary>
    /// Gets or sets if this is rendered on the list page
    /// </summary>
    [Parameter] public bool ListPage { get; set; }
    
    /// <summary>
    /// Translation strings
    /// </summary>
    private string lblSchedule, lblOperatingSystem, lblArchitecture, lblMemory, lblVersion, lblStatus, lblInternalProcessingNode, lblRunners, lblPriority;
    
    /// <inheritdoc />
    protected override void OnInitialized()
    {
        lblSchedule = Translater.Instant("Labels.Schedule");
        lblOperatingSystem = Translater.Instant("Labels.OperatingSystem");
        lblArchitecture = Translater.Instant("Labels.Architecture");
        lblMemory = Translater.Instant("Labels.Memory");
        lblVersion = Translater.Instant("Labels.Version");
        lblStatus = Translater.Instant("Labels.Status");
        lblRunners = Translater.Instant("Pages.Nodes.Labels.Runners");
        lblPriority = Translater.Instant("Pages.ProcessingNode.Fields.Priority");
        lblInternalProcessingNode = Translater.Instant("Labels.InternalProcessingNode");
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

    /// <summary>
    /// Called when the enable state changes
    /// </summary>
    /// <param name="state">the new state</param>
    private void EnableChanged(bool state)
    {
        if(Node.Enabled == state)
            return;
        Node.Enabled = state;
        _ = HttpHelper.Put<ProcessingNode>($"/api/node/state/{Node.Uid}?enable={state}");
    }
}