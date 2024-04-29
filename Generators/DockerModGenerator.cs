using System.Text.Json;
using System.Text.Json.Serialization;

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
        string prefix = "";
        
        var modFiles = Directory.GetFiles(Path.Combine(GetProjectRootDirectory(), "DockerMods"), "*.sh", SearchOption.AllDirectories);

        List<DockerMod> mods = new();
        foreach (var file in modFiles)
        {
            try
            {
                var mod = parseDockerMod(file);
                mod.Code = null; // we dont want the code here
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
    static DockerMod parseDockerMod(string filePath)
    {
        var mod = new DockerMod();

        using (StreamReader sr = new StreamReader(filePath))
        {
            string? line = sr.ReadLine(); // reads the first # ----- line
            bool inDescription = false;
            var descNewLine = "#" + new string(' ', " Description: ".Length);
            bool finishedCommentBlock = false;
            while ((line = sr.ReadLine()) != null)
            {
                if (finishedCommentBlock)
                {
                    mod.Code += line + "\n";
                }
                else if (finishedCommentBlock == false && line.StartsWith("#"))
                {
                    if (line.StartsWith("# -----------"))
                    {
                        finishedCommentBlock = true;
                    }
                    else if (ParseComment(line, mod))
                    {
                        inDescription = line.StartsWith("# Description:");
                    }
                    else if (inDescription && line.StartsWith(descNewLine))
                    {
                        mod.Description += "\n" + line[descNewLine.Length..].Trim();
                    }
                }
            }
        }

        return mod;
    }

    /// <summary>
    /// Parses a comment line and updates the mod data accordingly.
    /// </summary>
    /// <param name="comment">The comment line to parse.</param>
    /// <param name="dockerMod">The mod data object to update.</param>
    /// <returns>true if it is consumed</returns>
    static bool ParseComment(string? comment, DockerMod dockerMod)
    {
        if (string.IsNullOrWhiteSpace(comment))
            return false;
        
        if (comment.StartsWith("# Name:"))
        {
            dockerMod.Name = comment["# Name:".Length..].Trim();
            return true;
        }
        if (comment.StartsWith("# Description:"))
        {
            dockerMod.Description += comment["# Description:".Length..].Trim();
            return true;
        }
        if (comment.StartsWith("# Author:"))
        {
            dockerMod.Author = comment["# Author:".Length..].Trim();
            return true;
        }
        if (comment.StartsWith("# Revision:"))
        {
            dockerMod.Revision = int.Parse(comment["# Revision:".Length..].Trim());
            return true;
        }
        if (comment.StartsWith("# Icon:"))
        {
            dockerMod.Icon = comment["# Icon:".Length..].Trim();
            return true;
        }
        return false;
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