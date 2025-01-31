using System.Reflection;
using FileFlows.Services;

namespace FileFlows.Server.DefaultTemplates;

/// <summary>
/// A loader that loads default templates if github cannot be reached
/// </summary>
public class TemplateLoader : ITemplateService
{
    /// <summary>
    /// Gets a list of library templates
    /// </summary>
    /// <returns>a list of library templates</returns>
    public string[] GetLibraryTemplates()
        => GetEmbeddedResources("Library");

    /// <summary>
    /// Gets a list of flow templates
    /// </summary>
    /// <returns>a list of flow templates</returns>
    public string[] GetFlowTemplates()
        => GetEmbeddedResources("Flow");

    /// <summary>
    /// Gets a list of flow templates
    /// </summary>
    /// <returns>a list of flow templates</returns>
    public string GetFlowsJson()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resource = $"{assembly.GetName().Name}.Templates.DefaultTemplates.flows.json";
        return GetEmbeddedResourceContent(resource);
    }
    
    /// <summary>
    /// Extracts a template to a specific directory
    /// </summary>
    /// <param name="templateName">the full name of the embedded template</param>
    /// <param name="destinationPath">the destination path</param>
    public void ExtractTo(string templateName, string destinationPath)
    {
        
        // Load the assembly containing the embedded resources
        var assembly = Assembly.GetExecutingAssembly();

        string prefix = $"{assembly.GetName().Name}.Templates.DefaultTemplates.";
        string relativeName = templateName[prefix.Length..];
        relativeName = string.Join(Path.DirectorySeparatorChar, relativeName.Split('.')[1..^1]) + ".json";
        var filename = Path.Combine(destinationPath, relativeName);

        var dir = new FileInfo(filename).Directory;
        if (dir.Exists == false)
            dir.Create();
        
        Logger.Instance.ILog("Extracting template: " + filename);
        var content = GetEmbeddedResourceContent(templateName);
        File.WriteAllText(filename, content);
    }


    /// <summary>
    /// Retrieves the names of embedded resources within the specified folder.
    /// </summary>
    /// <param name="folderName">The dot-separated folder name.</param>
    /// <returns>An array of embedded resource paths within the specified folder.</returns>
    private string[] GetEmbeddedResources(string folderName)
    {
        // Load the assembly containing the embedded resources
        var assembly = Assembly.GetExecutingAssembly();

        // Get all resource names in the assembly
        var resourceNames = assembly.GetManifestResourceNames();

        // Prepare the folder prefix
        var folderPrefix = $"{assembly.GetName().Name}.Templates.DefaultTemplates.{folderName}.";

        // Filter resources based on the folder path
        return Array.FindAll(resourceNames, name => name.StartsWith(folderPrefix));
    }
    
    /// <summary>
    /// Retrieves the contents of the specified embedded resource file.
    /// </summary>
    /// <param name="resourceName">The name of the embedded resource file.</param>
    /// <returns>The contents of the embedded resource file.</returns>
    private string GetEmbeddedResourceContent(string resourceName)
    {
        // Load the assembly containing the embedded resources
        var assembly = Assembly.GetExecutingAssembly();
        
        // Load the resource stream
        using var resourceStream = assembly.GetManifestResourceStream(resourceName);
        
        if (resourceStream == null)
            return string.Empty;
        
        // Read the content of the resource
        using var reader = new StreamReader(resourceStream);
        return reader.ReadToEnd();
    }
}