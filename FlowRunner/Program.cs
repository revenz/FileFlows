using System.Globalization;
using System.Reflection;

namespace FileFlows.FlowRunner;

/// <summary>
/// Flow Runner
/// </summary>
public class Program
{
    private static RunInstance instance; 
    /// <summary>
    /// Main entry point for the flow runner
    /// </summary>
    /// <param name="args">the command line arguments</param>
    public static void Main(string[] args)
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        
        AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainOnAssemblyResolve;

        instance = new();
        int exitCode = instance.Run(args);
        instance.LogInfo("Exit Code: " + exitCode);
        Environment.ExitCode = exitCode;
    }
    
    /// <summary>
    /// Resolves assembly loading issues by attempting to load the required assembly from a specified path.
    /// This event is triggered when the runtime cannot resolve an assembly during execution.
    /// </summary>
    /// <param name="sender">The source of the event, typically the <see cref="AppDomain"/>.</param>
    /// <param name="args">The <see cref="ResolveEventArgs"/> that contains the details about the assembly being requested.</param>
    /// <returns>
    /// The <see cref="Assembly"/> that resolves the request, or <c>null</c> if the assembly could not be resolved.
    /// </returns>
    private static Assembly? CurrentDomainOnAssemblyResolve(object? sender, ResolveEventArgs args)
    {
        // Check if the assembly being requested is 'FileFlows.Plugin'
        var requestedAssembly = new AssemblyName(args.Name);
        if (requestedAssembly.Name == "FileFlows.Plugin")
        {
            // Specify the path where the correct version of the assembly is located
            string assemblyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FileFlows.Plugin.dll");

            // Load and return the correct version of the assembly
            if (File.Exists(assemblyPath))
            {
                instance?.LogInfo("FileFlows.Plugin.dll loaded manually");
                return Assembly.LoadFrom(assemblyPath);
            }
        }
        instance?.LogInfo("Not manually loading: " + requestedAssembly.Name);

        // Return null if not resolved
        return null;
    }

#if(DEBUG)
    private static bool assemblyResolverDone = false;
    /// <summary>
    /// Used for debugging to capture full log and test the full log update method
    /// </summary>
    /// <param name="args">the args</param>
    /// <returns>the exit code and full log</returns>
    public static (int ExitCode, string Log) RunWithLog(string[] args)
    {
        if (assemblyResolverDone == false)
        {
            assemblyResolverDone = true;
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                if (args.Name.StartsWith("FileFlows.Plugin"))
                    return typeof(Plugin.IPlugin).Assembly;
                if (args.Name.StartsWith("FileFlows.Common"))
                    return typeof(FileFlows.Common.ILogger).Assembly;
                return null;
            };
        }

        RunInstance instance = new();
        int exitCode = instance.Run(args);
        return (exitCode,instance.Logger.ToString());
    }
    #endif
    
    

}