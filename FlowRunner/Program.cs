using System.Globalization;
using System.Reflection;
using FileFlows.FlowRunner.JsonRpc;
using FileFlows.Shared.Models;

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
    public static async Task Main(string[] args)
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        
        AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainOnAssemblyResolve;


        int exitCode = (int) await Run(args[0]);
        Console.WriteLine("Exit Code: " + exitCode);
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
        if (requestedAssembly.Name == "FileFlows.Common")
        {
            instance?.LogInfo("Forcing use of already loaded FileFlows.Common.");
            return typeof(FileFlows.Common.Globals).Assembly;
        }
        
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

    /// <summary>
    /// Used for debugging to capture full log and test the full log update method
    /// </summary>
    /// <param name="pipeName">the pipeName</param>
    /// <returns>the exit code </returns>
    public static async Task<FileStatus> Run(string pipeName)
    {
        JsonRpcClient jsonRpcClient = new ();
        try
        {
            if(await jsonRpcClient.Initialize(pipeName) == false)
                throw new Exception("Failed to initialize RPC Client");
            instance = new RunInstance(new (jsonRpcClient));
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return FileStatus.ProcessingFailed;
        }

        return await instance.Run();
    }
    
#if(DEBUG)
    private static bool assemblyResolverDone = false;
    /// <summary>
    /// Used for debugging to capture full log and test the full log update method
    /// </summary>
    /// <param name="pipeName">the pipeName</param>
    /// <returns>the exit code </returns>
    public static FileStatus RunInternal(string pipeName)
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

        try
        {
            var task = Run(pipeName);
            task.Wait();
            return task.Result;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            throw;
        }
    }
    #endif
    
    

}