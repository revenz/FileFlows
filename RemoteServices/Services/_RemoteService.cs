namespace FileFlows.RemoteServices;

/// <summary>
/// Remote service
/// </summary>
public abstract class RemoteService 
{
    private static string? _ServiceBaseUrl;
    /// <summary>
    /// Gets or sets the Base URL of the FileFlows server
    /// </summary>
    public static string ServiceBaseUrl 
    { 
        get => _ServiceBaseUrl!;
        set
        {
            if(value == null)
            {
                _ServiceBaseUrl = string.Empty;
                return;
            }

            _ServiceBaseUrl = value.EndsWith('/') ? value[..^1] : value;
        }
    }

    /// <summary>
    /// Gets or sets the Access Token
    /// </summary>
    public static string AccessToken
    {
        get => ServerGlobals.AccessToken ?? string.Empty; 
        set => ServerGlobals.AccessToken = value;
    } 

    /// <summary>
    /// Gets or sets the Node UID whose making these requests
    /// </summary>
    public static Guid NodeUid { get; set; }

}