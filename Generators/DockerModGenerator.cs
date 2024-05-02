using System.Text.Json;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace FileFlowsScriptRepo.Generators;

/// <summary>
/// Generates the DockerMod repo data
/// </summary>
public class DockerModGenerator: Generator
{

    /// <summary>
    /// Generates the repository json file
    /// </summary>
    public static void Run()
    {
        var mods = GetMods();
        
        string json = JsonSerializer.Serialize(mods, new JsonSerializerOptions() {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new DataConverter() }            
        });   
        File.WriteAllText(Path.Combine(GetProjectRootDirectory(), "docker-mods.json"), json);
        Console.WriteLine("Done");
    }

    /// <summary>
    /// Gets all the DockerMods
    /// </summary>
    /// <returns>a list of DockerMods</returns>
    internal static List<DockerMod> GetMods()
    {
        var modFiles = Directory.GetFiles(Path.Combine(GetProjectRootDirectory(), "DockerMods"), "*.sh", 
            SearchOption.AllDirectories);

        List<DockerMod> mods = new();
        foreach (var file in modFiles)
        {
            try
            {
                var mod = ParseDockerMod(file);
                if (mod == null)
                    continue;
                mod.Code = null; // we don't want the code here
                mods.Add(mod);
            }
            catch (Exception)
            {
                // Ignored
            }
        }

        return mods;
    }
    

    /// <summary>
    /// Parses DockerMod data from the given file path.
    /// </summary>
    /// <param name="filePath">The path to the file containing DocerkMod.</param>
    /// <returns>The parsed DocerkMod.</returns>
    static DockerMod? ParseDockerMod(string filePath)
    {
        string content = File.ReadAllText(filePath);
        var match = Regex.Match(content, "(?s)# [-]{60,}(.*?)# [-]{60,}");
        if (match?.Success != true)
            return null;

        var head = match.Value;
        content = content.Replace(head, string.Empty).Trim();
        
        string yaml = string.Join("\n", head.Split('\n').Where(x => x.StartsWith("# -----") == false)
            .Select(x => x[2..]));
        string code = content;
        
        // Deserialize YAML to DockerMod object
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .Build();

        var mod = deserializer.Deserialize<DockerMod>(yaml);
        mod.Code = code;
        mod.FileName = new FileInfo(filePath).Name;
        return mod;
    }
}

/// <summary>
/// DockerMod
/// </summary>
class DockerMod
{
    /// <summary>
    /// Name of the mod.
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// Description of the mod.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Author of the mod.
    /// </summary>
    public string? Author { get; set; }
    
    /// <summary>
    /// Revision number of the mod.
    /// </summary>
    public int Revision { get; set; }
    
    /// <summary>
    /// Icon associated with the mod.
    /// </summary>
    public string? Icon { get; set; }
    
    /// <summary>
    /// Code of the mod.
    /// </summary>
    public string? Code { get; set; }
    
    /// <summary>
    /// Gets or sets the file name
    /// </summary>
    public string? FileName { get; set; }
    
    /// <summary>
    /// Gets or sets if this is a default script and will be pre-selected during initial configuration
    /// </summary>
    public bool Default { get; set; }
}