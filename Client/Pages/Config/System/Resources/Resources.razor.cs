using FileFlows.Client.Components.Editors;
using FileFlows.Client.Helpers;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Pages;

/// <summary>
/// Page for resources
/// </summary>
public partial class Resources : ListPage<Guid, Resource>
{
    /// <summary>
    /// Gets or sets the modal service
    /// </summary>
    [Inject] private IModalService ModalService { get; set; }
    
    /// <inheritdoc />
    public override string ApiUrl => "/api/resource";
    
    /// <summary>
    /// Gets if they are licensed for this page
    /// </summary>
    /// <returns>if they are licensed for this page</returns>
    protected override bool Licensed()
        => Profile.LicensedFor(LicenseFlags.AutoUpdates);

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        Layout.SetInfo(Translater.Instant("Pages.Resources.Title"), "fas fa-box-open", noPadding: true);
    }

    /// <summary>
    /// Adds a new task
    /// </summary>
    private async Task Add()
    {
        var result = await ModalService.ShowModal<ResourceEditor, Resource>(new ModalEditorOptions()
        {
            Model = new Resource()
        });
        if(result.IsFailed == false)
            await Load(result.Value.Uid);
    }

    /// <inheritdoc />
    public override async Task<bool> Edit(Resource item)
    {
        var result = await ModalService.ShowModal<ResourceEditor, Resource>(new ModalEditorOptions()
        {
            Uid = item.Uid
        });
        if(result.IsFailed == false)
            await Load(result.Value.Uid);
        return false;
    }
    
    /// <summary>
    /// Gets the icon for a task
    /// </summary>
    /// <param name="resource">the task</param>
    /// <returns>the icon</returns>
    private string GetIcon(Resource resource)
    {
        if (resource == null || string.IsNullOrEmpty(resource.MimeType))
            return "fas fa-file"; // Fallback icon
        
        // Get the file extension based on MIME type
        var extension = resource.MimeType switch
        {
            "image/jpeg" => "jpg",
            "image/png" => "png",
            "image/gif" => "gif",
            "image/svg+xml" => "svg",
        
            "audio/mpeg" => "mp3",
            "audio/wav" => "wav",
            "audio/ogg" => "ogg",
            "audio/aac" => "aac",
        
            "video/mkv" => "mkv",
            "video/mp4" => "mp4",
            "video/webm" => "webm",
            "video/ogg" => "ogg",
        
            "application/pdf" => "pdf",
            "application/zip" => "zip",
            "application/vnd.ms-excel" => "xls",
            "application/msword" => "doc",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => "xlsx",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => "docx",
        
            "text/plain" => "txt",
            "text/csv" => "csv",
            "text/html" => "html",
        
            _ => null
        };

        // Use the helper for known extensions or fall back on Font Awesome icons
        if (extension != null)
            return IconHelper.GetImage(extension);

        return resource.MimeType switch
        {
            "image/" => "fas fa-file-image",
            "audio/" => "fas fa-file-audio",
            "video/" => "fas fa-file-video",
            "application/" => "fas fa-file-alt",
            "text/" => "fas fa-file-alt",
            _ => "fas fa-file" // General fallback icon
        };
    }
}