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
        repo.SharedScripts = GetScripts("Shared");
        repo.ProcessScripts = GetScripts("Process");
        repo.FlowScripts = GetScripts("Flows");
        string json = JsonSerializer.Serialize(repo, new JsonSerializerOptions() {
            WriteIndented = true
        });   
        File.WriteAllText("repo.json", json);
    }

    private static List<Script> GetScripts(string path)
    {        
        List<Script> scripts = new List<Script>();
        var rgxComments = new Regex(@"\/\*(\*)?(.*?)\*\/", RegexOptions.Singleline);
        var basePath = new DirectoryInfo(path);
        foreach(var file in basePath.GetFiles("*.js", SearchOption.AllDirectories))
        {
            string content = File.ReadAllText(file.FullName);
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
            var script = new Script();
            foreach(Match match in Regex.Matches(comments, "@[^@]+")) 
            {
                string part = match.Value;
                if(part.StartsWith("@description "))
                    script.Description = part.Substring("@description ".Length).Trim();
                else if(part.StartsWith("@name "))
                    script.Name = part.Substring("@name ".Length).Trim();
                else if(part.StartsWith("@revision "))
                    script.Revision = int.Parse(part.Substring("@revision ".Length).Trim());
            }
            if(string.IsNullOrWhiteSpace(script.Name))
                script.Name = file.Name;
            if(string.IsNullOrWhiteSpace(script.Description))
                throw new Exception("No description found in: " + file.FullName);
            if(script.Revision < 1)
                throw new Exception("No revision found in: " + file.FullName);

            script.Path = basePath.Name + "/" + file.FullName.Substring(basePath.FullName.Length + 1).Replace("\\", "/");
            scripts.Add(script);
        }
        return scripts;
    }
}

class Repository 
{
    public List<Script> SharedScripts { get; set; } = new List<Script>();
    public List<Script> ProcessScripts { get; set; } = new List<Script>();
    public List<Script> FlowScripts { get; set; } = new List<Script>();
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