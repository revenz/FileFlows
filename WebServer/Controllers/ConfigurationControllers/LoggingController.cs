using FileFlows.Shared.Models.Configuration;

namespace FileFlows.WebServer.Controllers.ConfigurationControllers;

/// <summary>
/// Logging Controller
/// </summary>
[Route("/api/configuration/logging")]
[FileFlowsAuthorize(UserRole.Admin)]
public class LoggingController : BaseController
{

    /// <summary>
    /// Get the logging settings
    /// </summary>
    /// <returns>The logging settings</returns>
    [HttpGet]
    public async Task<LoggingModel> Get()
    {
        var settings = await ServiceLoader.Load<ISettingsService>().Get() ?? new ();

        LoggingModel model = new();
        model.LogQueueMessages = settings.LogQueueMessages;
        model.LogFileRetention = settings.LogFileRetention;
        model.LogEveryRequest = settings.LogEveryRequest;
        model.LibraryFileLogFileRetention = settings.LibraryFileLogFileRetention;
        return model;
    }
    
    
    /// <summary>
    /// Saves the logging model
    /// </summary>
    /// <param name="model">the logging model</param>
    [HttpPut]
    public async Task Save([FromBody] LoggingModel model)
    {
        if (model == null)
            return;

        var service = (SettingsService)ServiceLoader.Load<ISettingsService>();
        var settings = await service.Get() ?? new ();
        settings.LogQueueMessages = model.LogQueueMessages;
        settings.LogFileRetention = model.LogFileRetention;
        settings.LogEveryRequest = model.LogEveryRequest;
        settings.LibraryFileLogFileRetention = model.LibraryFileLogFileRetention;
        
        await service.Save(settings, await GetAuditDetails(), dontUpdateRevision: true);
    }
}