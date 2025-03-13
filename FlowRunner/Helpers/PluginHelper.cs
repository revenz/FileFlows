using System.Reflection;
using FileFlows.Plugin;

namespace FileFlows.FlowRunner.Helpers;

/// <summary>
/// Plugin Helper
/// </summary>
public class PluginHelper
{
    /// <summary>
    /// Invokes a method in a plugin
    /// </summary>
    /// <param name="runInstance">the run instance executing this</param>
    /// <param name="nodeParameters">the node parameters being used in the flow</param>
    /// <param name="plugin">the name of the plugin to invoke</param>
    /// <param name="method">the method in the plugin to invoke</param>
    /// <param name="args">the arguments to pass into the method</param>
    /// <returns>the result from the invoked method</returns>
    internal static object PluginMethodInvoker(RunInstance runInstance, NodeParameters nodeParameters, string plugin, string method, object[] args)
    {
        var dll = new DirectoryInfo(runInstance.Properties.WorkingDirectory).GetFiles(plugin + ".dll", SearchOption.AllDirectories).FirstOrDefault();
        if (dll == null)
        {
            runInstance.Properties.Logger.ELog("Failed to locate plugin: " + plugin);
            return null;
        }

        try
        {
            //var assembly = Context.LoadFromAssemblyPath(dll.FullName);
            var assembly = Assembly.LoadFrom(dll.FullName);
            var type = assembly.GetTypes().FirstOrDefault(x => x.Name == "StaticMethods");
            if (type == null)
            {
                runInstance.Properties.Logger.ELog("No static methods found in plugin: " + plugin);
                return null;
            }

            var methodInfo = type.GetMethod(method, BindingFlags.Public | BindingFlags.Static);
            if (methodInfo == null)
            {
                runInstance.Properties.Logger.ELog($"Method not found in plugin: {plugin}.{method}");
                return null;
            }

            var result = methodInfo.Invoke(null, new[]
            {
                nodeParameters
            }.Union(args ?? new object[] { }).ToArray());
            return result;
        }
        catch (Exception ex)
        {
            runInstance.Properties.Logger.ELog($"Error executing plugin method [{plugin}.{method}]: " + ex.Message);
            return null;
        }
    }
}