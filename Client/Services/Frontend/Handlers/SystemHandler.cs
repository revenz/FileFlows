namespace FileFlows.Client.Services.Frontend.Handlers;

/// <summary>
/// System handler
/// </summary>
/// <param name="feService">the frontend service</param>
public class SystemHandler(FrontendService feService)
{
    /// <summary>
    /// Called when an update is pending
    /// </summary>
    public event Action<bool>? OnUpdatePending;
    
    /// <summary>
    /// Gets or sets if an update is pending
    /// </summary>
    public bool UpdatePending { get; private set; }
    
    
    /// <summary>
    /// Called when the server is upgrading
    /// </summary>
    public event Action<bool>? OnUpgrading;
    
    /// <summary>
    /// Gets or sets if an update is pending
    /// </summary>
    public bool Upgrading { get; private set; }
    
    /// <summary>
    /// Gets if the upgrade event was recieved from the server
    /// </summary>
    public bool ReceivedUpgradingEvent { get; private set; }

    /// <summary>
    /// Initializes the handler
    /// </summary>
    /// <param name="data">the initial client data</param>
    public void Initialize(InitialClientData data)
    {
        feService.Registry.Register<bool>("UpdatePending", (ed) =>
        {
            UpdatePending = true;
            OnUpdatePending?.Invoke(true);
        });
        
        feService.Registry.Register<bool>("Upgrading", (ed) =>
        {
            ReceivedUpgradingEvent = true;
        });
    }

    /// <summary>
    /// Triggers the upgrading event
    /// </summary>
    public void TriggerUpgrading()
    {
        ReceivedUpgradingEvent = false;
        Upgrading = true;
        OnUpgrading?.Invoke(true);
    }

    /// <summary>
    /// Triggers the system was ugpraded
    /// </summary>
    public void TriggerUpgraded()
    {
        Upgrading = false;
        OnUpgrading?.Invoke(false);
    }
}