using System.Text.Json;

namespace FileFlowsScriptRepo.Generators;

/// <summary>
/// Repository Generator
/// </summary>
class RepoGenerator : Generator
{
    /// <summary>
    /// Generates the repository json file
    /// </summary>
    public static void Run()
    {
        string projDir = GetProjectRootDirectory();
        var repo = new Repository();
        repo.SharedScripts = GetScripts(Path.Combine(projDir, "Scripts/Shared"), ScriptType.Shared);
        repo.SystemScripts = GetScripts(Path.Combine(projDir, "Scripts/System"), ScriptType.System);
        repo.FlowScripts = GetScripts(Path.Combine(projDir, "Scripts/Flow"), ScriptType.Flow);
        repo.WebhookScripts = GetScripts(Path.Combine(projDir, "Scripts/Webhook"), ScriptType.Webhook);
        repo.FunctionScripts = GetScripts(Path.Combine(projDir, "Scripts/Function"), ScriptType.Template, new [] { ".js", ".cs", ".bat", ".ps1", "sh"});
        repo.SubFlows = GetTemplates(Path.Combine(projDir, "SubFlows"));
        repo.DockerMods = DockerModGenerator.GetMods().Select(x => new RepositoryObject()
        {
            Name = x.Name,
            Description = x.Description,
            Author = x.Author,
            Revision = x.Revision,
            Icon = x.Icon,
            Default = x.Default,
            Path = $"DockerMods/{x.FileName}"
        }).ToList();
        string json = JsonSerializer.Serialize(repo, new JsonSerializerOptions() {
            WriteIndented = true,
            Converters = { new DataConverter() }            
        });   
        File.WriteAllText(Path.Combine(projDir, "repo.json"), json);
        Console.WriteLine("Done");
    }

    private static List<RepositoryObject> GetScripts(string path, ScriptType type, string[] extensions = null)
    {        
        extensions ??=  new string[] { ".js" };
        List<RepositoryObject> scripts = new List<RepositoryObject>();
        var rgxComments = new Regex(@"\/\*(\*)?(.*?)\*\/", RegexOptions.Singleline);
        var basePath = new DirectoryInfo(path);
        foreach(var file in basePath.GetFiles("*.*", SearchOption.AllDirectories))
        {
            if (extensions.Contains(file.Extension.ToLowerInvariant()) == false)
                continue;
            
            string content = File.ReadAllText(file.FullName);
            var script = new RepositoryObject();

            // if this throws an exception, the script is invalid, we're not trying to make this generator bullet proof, we want exceptions if the script is bad
            string comments = rgxComments.Match(content).Value;
            
            // remove the start * 
            comments = string.Join("\n", comments.Replace("\r\n", "\n").Split('\n')
                .Select(x => Regex.Replace(x, @"^[\s]*[\*]+[\s]*", ""))).Trim();
            
            bool outputs = false;
            foreach (var line in comments.Split('\n'))
            {
                if (line.StartsWith('@') == false)
                {
                    if (string.IsNullOrWhiteSpace(script.Description))
                        script.Description = line;
                    else
                        script.Description += "\n" + line;
                }
                if(line.StartsWith("@description "))
                    script.Description = line.Substring("@description ".Length).Trim();
                else if (line.StartsWith("@uid ") && Guid.TryParse(line[5..].Trim(), out var uid))
                    script.Uid = uid;
                else if(line.StartsWith("@name "))
                    script.Name = line.Substring("@name ".Length).Trim();
                else if(line.StartsWith("@revision "))
                    script.Revision = int.Parse(line.Substring("@revision ".Length).Trim());
                else if(line.StartsWith("@outputs "))
                    outputs = true;
                else if(line.StartsWith("@minimumVersion "))
                    script.MinimumVersion = line.Substring("@minimumVersion ".Length).Trim();
            }
            if(string.IsNullOrWhiteSpace(script.Description))
                throw new Exception("No description found in: " + file.FullName);
            if(script.Revision < 1)
                throw new Exception("No revision found in: " + file.FullName);
            if(script.Uid == null && type != ScriptType.Template)
                throw new Exception("No UID found in: " + file.FullName);

                
            if(type == ScriptType.Template && outputs == false)
                throw new Exception($"Template '{file}' must define outputs!");

            if(string.IsNullOrWhiteSpace(script.Name))
                script.Name = file.Name[..^(file.Extension.Length)];

            script.Path = "Scripts/" + basePath.Name + "/" + file.FullName.Substring(basePath.FullName.Length + 1).Replace("\\", "/");
            scripts.Add(script);
        }
        return scripts;
    }

    

    /// <summary>
    /// Gets the templates from a given folder
    /// </summary>
    /// <param name="path">the folder path</param>
    /// <param name="community">if community flows should be included</param>
    /// <returns>a list of flows</returns>
    private static List<RepositoryObject> GetTemplates(string path, bool community = false)
    {        
        List<RepositoryObject> templates = new List<RepositoryObject>();
        var rgxComments = new Regex(@"\/\*(\*)?(.*?)\*\/", RegexOptions.Singleline);
        var basePath = new DirectoryInfo(path);
        JsonSerializerOptions options = new JsonSerializerOptions(){    
            PropertyNameCaseInsensitive = true
        };

        foreach(var file in basePath.GetFiles("*.json", SearchOption.AllDirectories))
        {
            var isCommunity = file.FullName.IndexOf("Community") > 0;
            if(isCommunity && community == false)
                continue;;
            if(isCommunity == false && community == true)
                continue;
            
            var isSubFlow = path.Contains("SubFlow") && path.Contains("Templates") == false;
            
            string content = File.ReadAllText(file.FullName);
            var template = JsonSerializer.Deserialize<SubFlowRepositoryObject>(content, options);
            if (template == null)
                continue;

            var actual = new RepositoryObject()
            {
                Name = template.Name,
                Description = template.Description?.EmptyAsNull() ?? template.Properties?.Description ?? string.Empty,
                Revision = template.Revision,
                Uid = template.Uid,
                MinimumVersion = template.MinimumVersion,
                Author = template.Author?.EmptyAsNull() ?? template?.Properties?.Author ?? string.Empty,
                SubFlows = template.SubFlows?.Any() != true ? null : template.SubFlows,
            };
            
            if(isSubFlow)
                actual.Path = "SubFlows/" + file.FullName[(basePath.FullName.Length + 1)..].Replace("\\", "/");
            else
                actual.Path = "Templates/" + basePath.Name + "/" + file.FullName[(basePath.FullName.Length + 1)..].Replace("\\", "/");
            templates.Add(actual);
        }

        return templates;
    }
}

enum ScriptType 
{
    Flow = 0,
    System = 1,
    Shared = 2,
    Template = 3,
    Webhook = 4

}

class Repository 
{
    /// <summary>
    /// Gets or sets the shared scripts
    /// </summary>
    public List<RepositoryObject> SharedScripts { get; set; } = new List<RepositoryObject>();
    /// <summary>
    /// Gets or sets the system scripts
    /// </summary>
    public List<RepositoryObject> SystemScripts { get; set; } = new List<RepositoryObject>();
    /// <summary>
    /// Gets or sets the flow scripts
    /// </summary>
    public List<RepositoryObject> FlowScripts { get; set; } = new List<RepositoryObject>();

    /// <summary>
    /// Gets or sets a list of function scripts 
    /// </summary>
    public List<RepositoryObject> FunctionScripts { get; set; } = new List<RepositoryObject>();

    /// <summary>
    /// Gets or sets a list of webhook script
    /// </summary>
    public List<RepositoryObject> WebhookScripts { get; set; } = new List<RepositoryObject>();

    /// <summary>
    /// Gets or sets a list of sub flows 
    /// </summary>
    public List<RepositoryObject> SubFlows { get; set; } = new List<RepositoryObject>();

    /// <summary>
    /// Gets or sets a list of DockerMods 
    /// </summary>
    public List<RepositoryObject> DockerMods { get; set; } = new List<RepositoryObject>();
}

public class RepositoryObject 
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
    /// Gets or sets the revision of the script
    /// </summary>
    public int Revision { get; set; }

    /// <summary>
    /// Gets or sets the minimum version of FileFlows required for this object
    /// </summary>
    public string MinimumVersion { get; set; }
    
    /// <summary>
    /// Gets or sets an optional UID for the repository item
    /// </summary>
    public Guid? Uid { get; set; }

    /// <summary>
    /// Gets or sets the author
    /// </summary>
    public string Author { get; set; }
    
    /// <summary>
    /// Gets or sets a list of sub flows this object depends on
    /// </summary>
    public List<Guid>? SubFlows { get; set; }
    
    /// <summary>
    /// Gets or sets an optional Icon
    /// </summary>
    public string? Icon { get; set; }
    
    /// <summary>
    /// Gets or sets if this is a default repository item and should be pre-selected 
    /// </summary>
    public bool? Default { get; set; }
}


/// <summary>
/// Sub Flow repository object
/// </summary>
public class SubFlowRepositoryObject : RepositoryObject
{
    /// <summary>
    /// Gets or sets the properties for this sub flow
    /// </summary>
    public RepositoryObjectProperties Properties { get; set; }
}

/// <summary>
/// Properties of a repository object
/// </summary>
public class RepositoryObjectProperties
{
    /// <summary>
    /// Gets or sets the description of this object
    /// </summary>
    public string Description { get; set; }
    /// <summary>
    /// Gets or sets the author of this object
    /// </summary>
    public string Author { get; set; }
}