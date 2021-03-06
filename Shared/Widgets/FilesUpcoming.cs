using FileFlows.Shared.Models;

namespace FileFlows.Shared.Widgets;

/// <summary>
/// Widget for upcoming files
/// </summary>
public class FilesUpcoming:WidgetDefinition
{
    /// <summary>
    /// The Widget Definition UID
    /// </summary>
    public static readonly Guid WD_UID = new ("1a545039-e37f-43a7-a3db-2b0640d83905");
    
    /// <summary>
    /// Gets the UID 
    /// </summary>
    public override Guid Uid => WD_UID;

    /// <summary>
    /// Gets the URL
    /// </summary>
    public override string Url => "/api/library-file/upcoming";

    /// <summary>
    /// Gets the Name
    /// </summary>
    public override string Name => "Upcoming";
    
    /// <summary>
    /// Gets the Icon
    /// </summary>
    public override string Icon => "fas fa-hourglass-start";

    /// <summary>
    /// Gets the type of Widget
    /// </summary>
    public override WidgetType Type => WidgetType.LibraryFileTable;

    /// <summary>
    /// Gets any flags 
    /// </summary>
    public override int Flags => 0;
}