namespace FileFlows.Shared.Models.Configuration;

/// <summary>
/// SMTP Model
/// </summary>
public class EmailModel
{
    /// <summary>
    /// Gets or sets the email server address
    /// </summary>
    [Encrypted]
    public string SmtpServer { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the email server port
    /// </summary>
    public int SmtpPort { get; set; }
    /// <summary>
    /// Gets or sets the email server security
    /// </summary>
    public EmailSecurity SmtpSecurity { get; set; }

    /// <summary>
    /// Gets or sets the name this is sent from
    /// </summary>
    [Encrypted]
    public string SmtpFrom { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email address this is sent from
    /// </summary>
    [Encrypted]
    public string SmtpFromAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email server user
    /// </summary>
    [Encrypted]
    public string SmtpUser { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the email server password
    /// </summary>
    [Encrypted]
    public string SmtpPassword { get; set; } = string.Empty;
}