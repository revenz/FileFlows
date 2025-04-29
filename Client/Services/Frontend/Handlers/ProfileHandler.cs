namespace FileFlows.Client.Services.Frontend.Handlers;

/// <summary>
/// Profile handler data
/// </summary>
/// <param name="feService">The frontend service</param>
/// <param name="localStorageService">the local storage service</param>
public class ProfileHandler(FrontendService feService, FFLocalStorageService localStorageService)
{
    public Profile Profile { get; private set; }

    /// <summary>
    /// Initializes the handler
    /// </summary>
    /// <param name="data">the initial data</param>
    public void Initialize(InitialClientData data)
    {
        Profile = data.Profile;
        Translater.Init(data.LanguageJson);
        
        feService.Registry.Register<string>("LanguageUpdated", (json) =>
        {
            if(string.IsNullOrWhiteSpace(json) == false)
                Translater.Init(json);
        });
    }

    /// <summary>
    /// Clears the access token
    /// </summary>
    public async Task ClearAccessToken()
    {
        await localStorageService.SetAccessToken(null);
        HttpHelper.Client.DefaultRequestHeaders.Authorization = null;
    }
}