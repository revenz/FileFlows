using FileFlows.Shared.Models.Configuration;

namespace FileFlows.WebServer.Controllers.ConfigurationControllers;

/// <summary>
/// SMTP Controller
/// </summary>
[Route("/api/configuration/email")]
[FileFlowsAuthorize(UserRole.Admin)]
public class SmtpController : BaseController
{
    /// <summary>
    /// Get the smtp settings
    /// </summary>
    /// <returns>The smtp settings</returns>
    [HttpGet]
    public async Task<EmailModel> Get()
    {
        var settings = await ServiceLoader.Load<ISettingsService>().Get() ?? new ();

        EmailModel model = new();
        
        if (string.IsNullOrWhiteSpace(settings.SmtpPassword) == false)
            model.SmtpPassword = CommonVariables.DUMMY_PASSWORD;
        
        model.SmtpFrom = settings.SmtpFrom;
        model.SmtpServer = settings.SmtpServer;
        model.SmtpPort = settings.SmtpPort;
        model.SmtpSecurity = settings.SmtpSecurity;
        model.SmtpUser = settings.SmtpUser;
        model.SmtpFromAddress = settings.SmtpFromAddress;
        return model;
    }
    
    
    /// <summary>
    /// Saves the smtp model
    /// </summary>
    /// <param name="model">the smtp model</param>
    [HttpPut]
    public async Task Save([FromBody] EmailModel model)
    {
        if (model == null)
            return;

        var service = (SettingsService)ServiceLoader.Load<ISettingsService>();
        var settings = await service.Get() ?? new ();
        
        settings.SmtpFrom = model.SmtpFrom ?? string.Empty;
        if (model.SmtpPassword != CommonVariables.DUMMY_PASSWORD)
            settings.SmtpPassword = model.SmtpPassword ?? string.Empty;
        settings.SmtpServer = model.SmtpServer ?? string.Empty;
        settings.SmtpPort = model.SmtpPort;
        settings.SmtpSecurity = model.SmtpSecurity;
        settings.SmtpUser = model.SmtpUser ?? string.Empty;
        settings.SmtpFromAddress = model.SmtpFromAddress ?? string.Empty;
        
        await service.Save(settings, await GetAuditDetails(), dontUpdateRevision: true);
    }
}