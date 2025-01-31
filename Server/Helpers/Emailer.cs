using System.Text.RegularExpressions;
using FileFlows.Plugin;
using FileFlows.Services;
using FileFlows.Shared.Models;
using MailKit.Net.Smtp;
using MimeKit;
using MailKit.Security;
using MimeKit.Utils;

namespace FileFlows.Server.Helpers;

/// <summary>
/// Emailer helper that sends emails
/// </summary>
class Emailer
{
    /// <summary>
    /// Sends an email 
    /// </summary>
    /// <param name="toName">the name of person getting the email</param>
    /// <param name="toAddress">the email address of the recipient</param>
    /// <param name="subject">the subject</param>
    /// <param name="body">the body of the email</param>
    /// <returns>The final free-form text response from the server.</returns>
    internal static async Task<Result<string>> Send(string toName, string toAddress, string subject, string body)
    {
        var settings = await ServiceLoader.Load<ISettingsService>().Get();

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(settings.SmtpFrom?.EmptyAsNull() ?? "FileFlows", settings.SmtpFromAddress?.EmptyAsNull() ?? "no-reply@fileflows.local"));
        message.To.Add(new MailboxAddress(toName?.EmptyAsNull() ?? toAddress, toAddress));
        message.Subject = subject;

        message.Body = new TextPart("plain")
        {
            Text = body
        };
        
        try
        {
            using var client = new SmtpClient();

            client.ServerCertificateValidationCallback = (s, c, h, e) => true;
            await client.ConnectAsync(settings.SmtpServer, settings.SmtpPort, settings.SmtpSecurity switch
            {
                EmailSecurity.Auto => SecureSocketOptions.Auto,
                EmailSecurity.SSL => SecureSocketOptions.SslOnConnect,
                EmailSecurity.TLS => SecureSocketOptions.StartTls,
                _ => SecureSocketOptions.None   
            });

            if (string.IsNullOrWhiteSpace(settings.SmtpUser) == false)
            {
                // Note: only needed if the SMTP server requires authentication
                await client.AuthenticateAsync(settings.SmtpUser, settings.SmtpPassword);
            }

            string response = await client.SendAsync(message);
            await client.DisconnectAsync(true);
            return response;
        }
        catch(Exception ex)
        {
            return Result<string>.Fail(ex.Message);
        }
    }
    
    /// <summary>
    /// Sends an email
    /// </summary>
    /// <param name="to">the email addresses of whom to send the email to</param>
    /// <param name="subject">the subject</param>
    /// <param name="body">the body of the email</param>
    /// <param name="isHtml">if the body is HTML or plaintext</param>
    /// <returns>The final free-form text response from the server.</returns>
    internal static async Task<Result<string>> Send(string[] to, string subject, string body, bool isHtml = false)
    {
        var settings = await ServiceLoader.Load<ISettingsService>().Get();

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(settings.SmtpFrom?.EmptyAsNull() ?? "FileFlows", settings.SmtpFromAddress?.EmptyAsNull() ?? "no-reply@fileflows.local"));
        foreach(var address in to)
        {
            message.To.Add(new MailboxAddress(address, address));
        }
        message.Subject = subject;

        if (isHtml)
        {

            var builder = new BodyBuilder();

            if (body.Contains("src=\"logo.png\""))
            {
#if (DEBUG)
                var dir = "wwwroot";
#else
        var dir = Path.Combine(DirectoryHelper.BaseDirectory, "Server/wwwroot");
#endif
                var logoFile = Path.Combine(dir, "report-logo.png");
                if (File.Exists(logoFile))
                {
                    var file = await builder.LinkedResources.AddAsync(logoFile);
                    file.ContentId = Path.GetFileName(logoFile);
                    // Update the HTML to use the CID for the logo
                    body = body.Replace("src=\"logo.png\"", $"src=\"cid:{file.ContentId}\"");
                }
                else
                {
                    // If the logo file does not exist, you can remove the logo tag from the HTML
                    body = Regex.Replace(body, "<img[^>]*src=\"logo.png\"[^>]*>", string.Empty);
                }
            }

            // Embed base64 images and update the body using the BodyBuilder instance
            body = EmbedBase64Images(body, builder);
            

            builder.HtmlBody = body;
            
            message.Body = builder.ToMessageBody();
        }
        else
        {
            message.Body = new TextPart("plain")
            {
                Text = body
            };
        }
        try
        {
            using var client = new SmtpClient();

            client.ServerCertificateValidationCallback = (s, c, h, e) => true;
            await client.ConnectAsync(settings.SmtpServer, settings.SmtpPort, settings.SmtpSecurity switch
            {
                EmailSecurity.Auto => SecureSocketOptions.Auto,
                EmailSecurity.SSL => SecureSocketOptions.SslOnConnect,
                EmailSecurity.TLS => SecureSocketOptions.StartTls,
                _ => SecureSocketOptions.None   
            });

            if (string.IsNullOrWhiteSpace(settings.SmtpUser) == false)
            {
                // Note: only needed if the SMTP server requires authentication
                await client.AuthenticateAsync(settings.SmtpUser, settings.SmtpPassword);
            }

            string response = await client.SendAsync(message);
            await client.DisconnectAsync(true);
            return response;
        }
        catch(Exception ex)
        {
            return Result<string>.Fail(ex.Message);
        }
    }
    

    /// <summary>
    /// Embeds base64 images into the HTML body and adds them as linked resources to the BodyBuilder instance.
    /// </summary>
    /// <param name="body">The HTML body containing base64 image sources.</param>
    /// <param name="builder">The BodyBuilder instance to add linked resources to.</param>
    /// <returns>the updated body</returns>
    private static string EmbedBase64Images(string body, BodyBuilder builder)
    {
        var matches = Regex.Matches(body, "<img[^>]+src\\s*=\\s*['\"](?<src>data:image/png;base64,[^\"]+)['\"][^>]*>");

        int count = 0;
        Dictionary<string, string> existing = new(); 
        foreach (Match match in matches)
        {
            count++;
            string base64String = match.Groups["src"].Value;
            if (existing.TryGetValue(base64String, out var cid) == false)
            {
                byte[] bytes = Convert.FromBase64String(base64String.Split(',')[1]);

                using MemoryStream stream = new MemoryStream(bytes);
                var image = builder.LinkedResources.Add("file_" + count + ".png", stream,
                    new ContentType("image", "png"));
                image.ContentId = MimeUtils.GenerateMessageId();
                cid = image.ContentId;
                existing[base64String] = cid;
            }

            // Replace only the src attribute with the CID
            body = body.Replace(base64String, $"cid:{cid}");
            
        }

        return body;
    }
}
