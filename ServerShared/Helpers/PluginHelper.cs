namespace FileFlows.ServerShared.Helpers;

using System.Dynamic;
using System.Reflection;
using FileFlows.Plugin;
using FileFlows.ServerShared.Services;
using FileFlows.Shared.Models;

/// <summary>
/// This class will allow hot reloading of an plugin assembly so they can be update
/// This class should return nothing from a Plugin assembly and just common C# objects
/// </summary>
public class PluginHelper
{
#if (DEBUG)
    private static string GetPluginDirectory()
    {
        string dir = new DirectoryInfo(".").FullName;
        dir = dir.Substring(0, dir.LastIndexOf("FileFlows") - 1).Replace("\\", "/");
        if (dir.EndsWith("FileFlows/FileFlows") == false)
            dir += "/FileFlows";
        dir += "/Server/Plugins";
        return dir;
    }
#else
    private static string GetPluginDirectory() => new DirectoryInfo("Plugins").FullName;
#endif

    /// <summary>
    /// Gets a list of all the plugins directories
    /// </summary>
    /// <returns>a list of all the plugins directories</returns>
    public List<string> GetPluginDirectories()
    {

        string pluginsDir = GetPluginDirectory();

        if (Directory.Exists(pluginsDir) == false)
            Directory.CreateDirectory(pluginsDir);

        List<string> results = new List<string>();
        foreach (var subdir in new DirectoryInfo(pluginsDir).GetDirectories())
        {
            var versionDir = subdir.GetDirectories().OrderByDescending(x =>
            {
                if (Version.TryParse(x.Name, out Version? v))
                    return v;
                return new Version(0, 0);
            }).FirstOrDefault();

            if (versionDir == null)
                continue;

            results.Add(versionDir.FullName);
        }
        return results;
    }
    private string GetPluginDirectory(string assemblyName)
    {

        string pluginsDir = GetPluginDirectory();

        if (Directory.Exists(pluginsDir) == false)
            Directory.CreateDirectory(pluginsDir);

        List<string> results = new List<string>();
        var dirInfo = new DirectoryInfo(pluginsDir + "/" + assemblyName.Replace(".dll", ""));
        if (dirInfo.Exists == false)
            return string.Empty;

        var versionDir = dirInfo.GetDirectories().OrderByDescending(x =>
        {
            if (Version.TryParse(x.Name, out Version? v))
                return v;
            return new Version(0, 0);
        }).FirstOrDefault();

        if (versionDir == null)
            return string.Empty;

        return versionDir.FullName;
    }

    private Type? GetNodeType(string fullName)
    {
        var dirs = GetPluginDirectories();
        foreach (var dir in dirs)
        {
            foreach (var dll in new DirectoryInfo(dir).GetFiles("*.dll"))
            {
                try
                {
                    //var assembly = Context.LoadFromAssemblyPath(dll.FullName);
                    var assembly = Assembly.LoadFrom(dll.FullName);
                    var types = assembly.GetTypes();
                    var pluginType = types.Where(x => x.IsAbstract == false && x.FullName == fullName).FirstOrDefault();
                    if (pluginType != null)
                        return pluginType;
                }
                catch (Exception) { }
            }
        }
        return null;
    }

    /// <summary>
    /// This needs to return an instance so the FlowExecutor can use it...
    /// </summary>
    /// <param name="part">The flow part</param>
    /// <returns>an instance of the plugin node</returns>
    public Node LoadNode(FlowPart part)
    {
        var nt = GetNodeType(part.FlowElementUid);
        if (nt == null)
            return new Node();
        var node = Activator.CreateInstance(nt);
        if(node == null)
            return new Node();
        if (part.Model is IDictionary<string, object> dict)
        {
            foreach (var k in dict.Keys)
            {
                try
                {
                    if (k == "Name")
                        continue; // this is just the display name in the flow UI
                    var prop = nt.GetProperty(k, BindingFlags.Instance | BindingFlags.Public);
                    if (prop == null)
                        continue;

                    if (dict[k] == null)
                        continue;

                    var value = Converter.ConvertObject(prop.PropertyType, dict[k], Logger.Instance);
                    if (value != null)
                        prop.SetValue(node, value);
                }
                catch (Exception ex)
                {
                    Logger.Instance.ELog("Type: " + nt.Name + ", Property: " + k);
                    Logger.Instance.ELog("Failed setting property: " + ex.Message + Environment.NewLine + ex.StackTrace);
                }
            }
        }
        return (Node)node;
    }
}
