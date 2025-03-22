using System.Collections.Concurrent;
using FileFlows.ServerShared.Interfaces;

namespace FileFlows.RemoteServices;

/// <summary>
/// Service Loader
/// </summary>
public static class ServiceLoader
{
    /// <summary>
    /// Gets the service provider for accessing registered services.
    /// </summary>
    public static CustomServiceProvider Provider { get; private set; }
    
    /// <summary>
    /// Gets or sets a special case services
    /// </summary>
    private static ConcurrentDictionary<Type, object> SpecialServices = new();

    /// <summary>
    /// Adds a special case
    /// </summary>
    /// <param name="service">the service to add</param>
    public static void AddSpecialCase<T>(T service) where T : class
    {
        SpecialServices[typeof(T)] = service;
    }

    /// <summary>
    /// Configures and initializes the services.
    /// </summary>
    static ServiceLoader()
    {
        // Add to WebServer to if needed
        Provider = new CustomServiceProvider()
            // .AddSingleton<IFlowRunnerService>(() => new FlowRunnerService())
            //.AddSingleton<ILibraryFileService>(() => new LibraryFileService())
            .AddSingleton<ILogService>(() => new LogService())
            .AddSingleton<INodeService>(() => new NodeService())
            .AddSingleton<ISettingsService>(() => new SettingsService())
            .AddSingleton<IStatisticService>(() => new StatisticService())
            .AddSingleton<IVariableService>(() => new VariableService())
            .AddSingleton<INotificationService>(() => new NotificationService())
            .AddSingleton<IDistributedCacheService>(() => new RemoteDistributedCacheService())
            .AddSingleton(() => new EmailService())
            .AddSingleton(() => new HardwareInfoService())
            .AddSingleton(() => new ConfigurationService())
            .BuildServiceProvider(); // Build the service provider
    }
    
    /// <summary>
    /// Loads the specified service.
    /// </summary>
    /// <typeparam name="T">The type of service to load.</typeparam>
    /// <returns>The loaded service instance.</returns>
    public static T Load<T>() where T : class
    {
        if(SpecialServices.TryGetValue(typeof(T), out var specialService))
            return (T)specialService;
        
        var service = Provider.GetService<T>(); // Get the required service instance
        if (service == null)
            throw new Exception($"Service '{typeof(T).Name}' not registered.");
        return service;
    }   
}