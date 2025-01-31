namespace FileFlows.WebServer;

/// <summary>
/// Extension Methods
/// </summary>
public static class ExtensionMethods
{
    
    /// <summary>
    /// Gets the actual IP address of the request
    /// </summary>
    /// <param name="Request">the request</param>
    /// <returns>the actual IP Address</returns>
    public static string GetActualIP(this HttpRequest Request)
    {
        try
        {
            foreach (string header in new[] { "True-Client-IP", "CF-Connecting-IP", "HTTP_X_FORWARDED_FOR" })
            {
                if (Request.Headers.ContainsKey(header) && string.IsNullOrEmpty(Request.Headers[header]) == false)
                {
                    string? ip = Request.Headers[header].FirstOrDefault()
                        ?.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)?.FirstOrDefault();
                    if (string.IsNullOrEmpty(ip) == false)
                        return ip;
                }
            }

            return Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
}