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
        var modFiles = Directory.GetFiles(Path.Combine(GetProjectRootDirectory(), "DockerMods"), "*.sh", 
            SearchOption.AllDirectories);

        List<DockerMod> mods = new();
        foreach (var file in modFiles)
        {
            try
            {
                var mod = ParseDockerMod(file);
                mod.Code = null; // we don't want the code here
                mods.Add(mod);
            }
            catch (Exception)
            {
            }
        }
        
        string json = JsonSerializer.Serialize(mods, new JsonSerializerOptions() {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new DataConverter() }            
        });   
        File.WriteAllText(Path.Combine(GetProjectRootDirectory(), "docker-mods.json"), json);
        Console.WriteLine("Done");
    }
    

    /// <summary>
    /// Parses DockerMod data from the given file path.
    /// </summary>
    /// <param name="filePath">The path to the file containing DocerkMod.</param>
    /// <returns>The parsed DocerkMod.</returns>
    static DockerMod ParseDockerMod(string filePath)
    {
        string content = File.ReadAllText(filePath);
        int index = content.IndexOf("# -----------------------------", 104, StringComparison.Ordinal);
        string yaml = string.Join("\n", content[..index].Split('\n').Skip(1).Select(x => x[2..]));
        index = content.IndexOf("#!/", StringComparison.Ordinal);
        string code = content[index..];
        
        // Deserialize YAML to DockerMod object
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var mod = deserializer.Deserialize<DockerMod>(yaml);
        mod.Code = code;
        return mod;
    }
}

class DockerMod
{
    /// <summary>
    /// Name of the mod.
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Description of the mod.
    /// </summary>
    public string Description { get; set; }
    
    /// <summary>
    /// Author of the mod.
    /// </summary>
    public string Author { get; set; }
    
    /// <summary>
    /// Revision number of the mod.
    /// </summary>
    public int Revision { get; set; }
    
    /// <summary>
    /// Icon associated with the mod.
    /// </summary>
    public string Icon { get; set; }
    
    /// <summary>
    /// Code of the mod.
    /// </summary>
    public string? Code { get; set; }
}