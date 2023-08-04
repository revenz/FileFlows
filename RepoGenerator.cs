using System.Text.Json;

/// <summary>
/// Repository Generator
/// </summary>
class RepoGenerator 
{
    /// <summary>
    /// Generates the repository json file
    /// </summary>
    public static void Run()
    {
        var repo = new Repository();
        repo.SharedScripts = GetScripts("Scripts/Shared", ScriptType.Shared);
        repo.SystemScripts = GetScripts("Scripts/System", ScriptType.System);
        repo.FlowScripts = GetScripts("Scripts/Flow", ScriptType.Flow);
        repo.WebhookScripts = GetScripts("Scripts/Webhook", ScriptType.Webhook);
        repo.FunctionScripts = GetScripts("Scripts/Function", ScriptType.Template);
        repo.FlowTemplates = GetTemplates("Templates/Flow", community: true);
        repo.CommunityFlowTemplates = GetTemplates("Templates/Flow", community: true);
        repo.LibraryTemplates = GetTemplates("Templates/Library");
        string json = JsonSerializer.Serialize(repo, new JsonSerializerOptions() {
            WriteIndented = true,
            Converters = { new DataConverter() }            
        });   
        File.WriteAllText("repo.json", json);
        Console.WriteLine("Done");
    }

    private static List<RepositoryObject> GetScripts(string path, ScriptType type)
    {        
        List<RepositoryObject> scripts = new List<RepositoryObject>();
        var rgxComments = new Regex(@"\/\*(\*)?(.*?)\*\/", RegexOptions.Singleline);
        var basePath = new DirectoryInfo(path);
        foreach(var file in basePath.GetFiles("*.js", SearchOption.AllDirectories))
        {
            string content = File.ReadAllText(file.FullName);
            var script = new RepositoryObject();

            // if this throws an exception, the script is invalid, we're not trying to make this generator bullet proof, we want exceptions if the script is bad
            string comments = rgxComments.Match(content).Value;
            comments = string.Join("\n", comments.Split(new string[] { "\r\n", "\n"}, StringSplitOptions.RemoveEmptyEntries).Select(x => {
                x = x.Trim();
                if(x.StartsWith("/*"))
                    return string.Empty;
                if(x.StartsWith("*/"))
                    return string.Empty;
                if(x.StartsWith("*"))
                    x = x[1..].Trim();
                return x;
            })).Trim();
            if(comments.StartsWith("@") == false)
                comments = "@description " + comments;
            bool outputs = false;
            foreach(Match match in Regex.Matches(comments, "@[^@]+")) 
            {
                string part = match.Value;
                if(part.StartsWith("@description "))
                    script.Description = part.Substring("@description ".Length).Trim();
                else if(part.StartsWith("@name "))
                    script.Name = part.Substring("@name ".Length).Trim();
                else if(part.StartsWith("@revision "))
                    script.Revision = int.Parse(part.Substring("@revision ".Length).Trim());
                else if(part.StartsWith("@outputs "))
                    outputs = true;
                else if(part.StartsWith("@minimumVersion "))
                    script.MinimumVersion = part.Substring("@minimumVersion ".Length).Trim();
            }
            if(string.IsNullOrWhiteSpace(script.Description))
                throw new Exception("No description found in: " + file.FullName);
            if(script.Revision < 1)
                throw new Exception("No revision found in: " + file.FullName);

                
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
            
            string content = File.ReadAllText(file.FullName);
            var template = JsonSerializer.Deserialize<RepositoryObject>(content, options);
            template.Path = "Templates/" + basePath.Name + "/" + file.FullName.Substring(basePath.FullName.Length + 1).Replace("\\", "/");
            templates.Add(template);
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
    /// Gets or sets a list of library templates 
    /// </summary>
    public List<RepositoryObject> LibraryTemplates { get; set; } = new List<RepositoryObject>();

    /// <summary>
    /// Gets or sets a list of flow templates 
    /// </summary>
    public List<RepositoryObject> FlowTemplates { get; set; } = new List<RepositoryObject>();

    /// <summary>
    /// Gets or sets a list of community flow templates 
    /// </summary>
    public List<RepositoryObject> CommunityFlowTemplates { get; set; } = new List<RepositoryObject>();
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

}