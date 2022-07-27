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
        repo.SharedScripts = GetScripts("Shared", ScriptType.Shared);
        repo.SystemScripts = GetScripts("System", ScriptType.System);
        repo.FlowScripts = GetScripts("Flow", ScriptType.Flow);
        repo.Templates = GetScripts("Templates", ScriptType.Template);
        string json = JsonSerializer.Serialize(repo, new JsonSerializerOptions() {
            WriteIndented = true
        });   
        File.WriteAllText("repo.json", json);
    }

    private static List<Script> GetScripts(string path, ScriptType type)
    {        
        List<Script> scripts = new List<Script>();
        var rgxComments = new Regex(@"\/\*(\*)?(.*?)\*\/", RegexOptions.Singleline);
        var basePath = new DirectoryInfo(path);
        foreach(var file in basePath.GetFiles("*.js", SearchOption.AllDirectories))
        {
            string content = File.ReadAllText(file.FullName);
            var script = new Script();

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
            }
            if(string.IsNullOrWhiteSpace(script.Description))
                throw new Exception("No description found in: " + file.FullName);
            if(script.Revision < 1)
                throw new Exception("No revision found in: " + file.FullName);

                
            if(type == ScriptType.Template && outputs == false)
                throw new Exception($"Template '{file}' must define outputs!");
            

            if(string.IsNullOrWhiteSpace(script.Name))
                script.Name = file.Name[..^(file.Extension.Length)];

            script.Path = basePath.Name + "/" + file.FullName.Substring(basePath.FullName.Length + 1).Replace("\\", "/");
            scripts.Add(script);
        }
        return scripts;
    }
}

enum ScriptType 
{
    Flow = 0,
    System = 1,
    Shared = 2,
    Template = 3

}

class Repository 
{
    /// <summary>
    /// Gets or sets the shared scripts
    /// </summary>
    public List<Script> SharedScripts { get; set; } = new List<Script>();
    /// <summary>
    /// Gets or sets the system scripts
    /// </summary>
    public List<Script> SystemScripts { get; set; } = new List<Script>();
    /// <summary>
    /// Gets or sets the flow scripts
    /// </summary>
    public List<Script> FlowScripts { get; set; } = new List<Script>();

    /// <summary>
    /// Gets a list of templates 
    /// </summary>
    public List<Script> Templates { get; set; } = new List<Script>();
}

class Script 
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

}