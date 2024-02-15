using System.Text.Json;
using System.Text.Json.Serialization;

namespace FileFlowsScriptRepo.Generators;

/// <summary>
/// Repository Generator
/// </summary>
class FlowTemplateGenerator : Generator
{
    /// <summary>
    /// Generates the repository json file
    /// </summary>
    public static void Run()
    {
        string prefix = "";
        
        var flows = GetFlowTemplates(Path.Combine(GetProjectRootDirectory(), "Templates", "Flow"));
        string flowsJson = JsonSerializer.Serialize(flows, new JsonSerializerOptions() {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new DataConverter() }            
        });   
        File.WriteAllText(Path.Combine(GetProjectRootDirectory(), "flows.json"), flowsJson);
        Console.WriteLine("Done");
    }

    /// <summary>
    /// Gets the templates from a given folder
    /// </summary>
    /// <param name="path">the folder path</param>
    /// <returns>a list of flows</returns>
    private static List<FlowTemplate> GetFlowTemplates(string path)
    {        
        List<FlowTemplate> templates = new List<FlowTemplate>();
        var rgxComments = new Regex(@"\/\*(\*)?(.*?)\*\/", RegexOptions.Singleline);
        var basePath = new DirectoryInfo(path);
        JsonSerializerOptions options = new JsonSerializerOptions(){    
            PropertyNameCaseInsensitive = true
        };


        foreach(var file in basePath.GetFiles("*.json", SearchOption.AllDirectories))
        {
            Console.WriteLine("File: " + file);
            
            string content = File.ReadAllText(file.FullName);
            var template = JsonSerializer.Deserialize<FlowTemplate>(content, options);
            if (template == null)
                continue;
            
            template.Path = "Templates/" + basePath.Name + "/" + file.FullName[(basePath.FullName.Length + 1)..].Replace("\\", "/");
            if (template.Parts?.Any() == true)
            {
                template.Plugins = template.Parts.Where(x => x.FlowElementUid.StartsWith("Script") == false)
                    .Select(x => x.FlowElementUid.Split('.')[x.FlowElementUid.StartsWith("FileFlows.") ? 1 : 0].Replace("Nodes", " Nodes").Replace("  ", " "))
                    .Distinct()
                    .ToList();
                template.Scripts = template.Parts.Where(x => x.FlowElementUid.StartsWith("Script") == true)
                    .Select(x => x.FlowElementUid).Distinct().Select(x =>
                        x[8..].Trim()
                    ).ToList();
            }
            else
            {
                Console.WriteLine("No pats: " + template.Name);
            }

            template.Author = template.Properties.Author;
            template.Description = template.Properties.Description;
            template.Tags = template.Properties.Tags;
            template.MinimumVersion = template.Properties.MinimumVersion;
            
            template.Properties = null;
            template.Fields = null;

            template.Parts = null; // to avoid writing it json
            templates.Add(template);
        }

        return templates;
    }
}

public class FlowTemplate 
{
    /// <summary>
    /// Gets or sets the path of the script
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the author of this object
    /// </summary>
    public string Author { get; set; }
    /// <summary>
    /// Gets or sets tags for this flow
    /// </summary>
    public List<string> Tags { get; set; }

    /// <summary>
    /// Gets or sets the revision of the script
    /// </summary>
    public int Revision { get; set; }

    /// <summary>
    /// Gets or sets the minimum version of FileFlows required for this object
    /// </summary>
    public string MinimumVersion { get; set; }

    /// <summary>
    /// Gets or sets plugins used by this object
    /// </summary>
    public List<string>? Plugins { get; set; }

    /// <summary>
    /// Gets or sets scripts used by this object
    /// </summary>
    public List<string>? Scripts { get; set; }

    /// <summary>
    /// Gets or sets the type of flow
    /// </summary>
    public FlowType Type { get; set; }
    
    /// <summary>
    /// Gets or sets the parts of this flow
    /// </summary>
    public List<FlowPart>? Parts { get; set; }
    
    /// <summary>
    /// Gets or sets the fields (used by legacy tmplates)
    /// </summary>
    public List<FlowField> Fields { get; set; }
    
    /// <summary>
    /// Gets or sets the flow properties
    /// </summary>
    public FlowProperties Properties { get; set; }
}


public class FlowPart
{
    /// <summary>
    /// Gets or sets the FlowElementUid this flow part is an instance of
    /// This is the full name of the flow element, Namespace.TypeName
    /// </summary>
    public string FlowElementUid { get; set; }

    /// <summary>
    /// Gets or sets the node, used by legacy templates instead of flow element uid
    /// </summary>
    public string Node
    {
        get => FlowElementUid;
        set
        {
            if(string.IsNullOrWhiteSpace(value) == false)
                FlowElementUid = value;
        }
    }
}

public class FlowField
{
    /// <summary>
    /// Gets or sets the name of the field
    /// </summary>
    public string Name { get; set; }
}


/// <summary>
/// Advanced flow properties
/// </summary>
public class FlowProperties
{

    /// <summary>
    /// Gets or sets the description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the author of this object
    /// </summary>
    public string Author { get; set; }
    /// <summary>
    /// Gets or sets tags for this flow
    /// </summary>
    public List<string> Tags { get; set; }

    /// <summary>
    /// Gets or sets the minimum version of FileFlows required for this object
    /// </summary>
    public string MinimumVersion { get; set; }

    /// <summary>
    /// Gets or sets the fields
    /// </summary>
    public List<FlowField> Fields { get; set; }
}

/// <summary>
/// A type of Flow
/// </summary>
public enum FlowType
{
    /// <summary>
    /// A standard flow
    /// </summary>
    Standard = 0,
    /// <summary>
    /// A special flow that is executed when a flow fails during execution
    /// </summary>
    Failure = 1
}