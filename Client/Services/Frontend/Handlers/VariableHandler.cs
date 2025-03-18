namespace FileFlows.Client.Services.Frontend.Handlers;

/// <summary>
/// Handler for variables
/// </summary>
/// <param name="feService">the frontend service</param>
public class VariableHandler(FrontendService feService)
{
    /// <summary>
    /// Gets or sets a basic list of variables
    /// </summary>
    public Dictionary<Guid, string> VariableList { get; private set; }
    /// <summary>
    /// Gets or sets a list of variables
    /// </summary>
    public List<Variable> Variables { get; private set; }
    
    /// <summary>
    /// Event raised when the variables is updated
    /// </summary>
    public event Action<List<Variable>> VariablesUpdated; 

    /// <summary>
    /// Initializes the handler
    /// </summary>
    /// <param name="data">the initial data</param>
    public void Initialize(InitialClientData data)
    {
        VariableList = data.Variables.ToDictionary(x => x.Uid, x => x.Name);
        Variables = data.Variables;
        
        feService.Registry.Register<List<Variable>>("VariablesUpdated", (ed) =>
        {
            VariableList = ed.ToDictionary(x => x.Uid, x => x.Name);
            Variables = ed;
            VariablesUpdated?.Invoke(ed);
        });
    }
}