using FileFlows.Shared.Models.Configuration;

namespace FileFlows.WebServer.Controllers.ConfigurationControllers;

/// <summary>
/// FileServer Controller
/// </summary>
[Route("/api/configuration/file-server")]
[FileFlowsAuthorize(UserRole.Admin)]
public class FileServerController : BaseController
{
    /// <summary>
    /// Get the file server settings
    /// </summary>
    /// <returns>The file server settings</returns>
    [HttpGet]
    public async Task<FileServerModel> Get()
    {
        var settings = await ServiceLoader.Load<ISettingsService>().Get() ?? new ();

        FileServerModel model = new();
        model.FileServerDisabled = settings.FileServerDisabled;
        model.FileServerOwnerGroup = settings.FileServerOwnerGroup;
        model.FileServerFilePermissions = settings.FileServerFilePermissions;
        model.FileServerFolderPermissions = settings.FileServerFolderPermissions;
        model.FileServerAllowedPaths = settings.FileServerAllowedPaths;
        model.FileServerAllowedPathsString = model.FileServerAllowedPaths?.Any() == true
            ? string.Join("\n", model.FileServerAllowedPaths)
            : string.Empty;
        return model;
    }
    
    
    /// <summary>
    /// Saves the file server model
    /// </summary>
    /// <param name="model">the file server model</param>
    [HttpPut]
    public async Task Save([FromBody] FileServerModel model)
    {
        if (model == null)
            return;

        var service = (SettingsService)ServiceLoader.Load<ISettingsService>();
        var settings = await service.Get() ?? new ();
       
        settings.FileServerDisabled = model.FileServerDisabled;
        settings.FileServerOwnerGroup = model.FileServerOwnerGroup;
        settings.FileServerFilePermissions = model.FileServerFilePermissions;
        settings.FileServerFolderPermissions = model.FileServerFolderPermissions;
        settings.FileServerAllowedPaths = model.FileServerAllowedPathsString?.Split(["\r\n", "\r", "\n"], StringSplitOptions.RemoveEmptyEntries) ?? [];

        await service.Save(settings, await GetAuditDetails(), dontUpdateRevision: true);
    }
}