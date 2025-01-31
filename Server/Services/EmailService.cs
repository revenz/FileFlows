using FileFlows.Plugin;
using FileFlows.Server.Helpers;
using FileFlows.Services;

namespace FileFlows.Server.Services;

/// <summary>
/// Email Service
/// </summary>
public class EmailService : IEmailService
{
    /// <inheritdoc />
    public async Task<Result<string>> Send(string toName, string toAddress, string subject, string body)
        => await Emailer.Send(toName, toAddress, subject, body);

    /// <inheritdoc />
    public async Task<Result<string>> Send(string[] to, string subject, string body, bool isHtml = false)
        => await Emailer.Send(to, subject, body, isHtml);
}