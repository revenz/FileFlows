using FileFlows.Plugin;
using FileFlows.ScriptExecution;
using FileFlows.Shared.Helpers;

namespace FileFlows.Shared.Models;

/// <summary>
/// A script is a special function node that lets you reuse them
/// </summary>
public class Script: FileFlowObject, IInUse
{
    /// <summary>
    /// Gets or sets the javascript code of the script
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// Gets or sets if this script is a from a repository and cannot be modified
    /// </summary>
    public bool Repository { get; set; }

    /// <summary>
    /// Gets or sets the type of script
    /// </summary>
    public ScriptType Type { get; set; }

    /// <summary>
    /// Gets or sets the Language of script
    /// </summary>
    public ScriptLanguage Language { get; set; }
    
    /// <summary>
    /// Gets or sets the revision of the script
    /// </summary>
    public int? Revision { get; set; }

    /// <summary>
    /// Gets or sets the remote path of the script
    /// </summary>
    public string? Path { get; set; }
    
    /// <summary>
    /// Gets or sets the description of the script
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Gets or sets the help of the script which is displayed at the top of a script when added to the flow
    /// </summary>
    public string? Help { get; set; }
    
    /// <summary>
    /// Gets or sets the minimum version of FileFlows required
    /// </summary>
    public Version? MinimumVersion { get; set; }
    
    /// <summary>
    /// Gets or sets the Author who wrote this script
    /// </summary>
    public string? Author { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a list of outputs for the script
    /// </summary>
    public List<KeyValuePair<int, string>> Outputs { get; set; } = new ();
    //public List<ScriptOutput> Outputs { get; set; } = new List<ScriptOutput>();

    /// <summary>
    /// Gets or sets parameters for the script
    /// </summary>
    public List<ScriptParameter> Parameters { get; set; } = new List<ScriptParameter>();

    
    /// <summary>
    /// Gets or sets the latest revision of the script
    /// </summary>
    [DbIgnore]
    public int LatestRevision { get; set; }
    
    /// <summary>
    /// Gets or sets what is using this object
    /// </summary>
    [DbIgnore]
    public List<ObjectReference> UsedBy { get; set; }


    /// <summary>
    /// Gets a script from the code
    /// </summary>
    /// <param name="name">the name of the script</param>
    /// <param name="code">the script code</param>
    /// <param name="type">the type of the script</param>
    /// <returns>the new script</returns>
    public static Result<Script> FromCode(string name, string code, ScriptType type)
    {
        var result = new ScriptParser().Parse(name, code, type);
        if (result.Failed(out string error))
            return Result<Script>.Fail(error);

        var scriptModel = result.Value;
        return new Script()
        {
            Uid = scriptModel.Uid,
            Type = type,
            Name = scriptModel.Name?.EmptyAsNull() ?? name,
            Code = scriptModel.Code,
            Revision = scriptModel.Revision,
            Description = scriptModel.Description,
            Help = scriptModel.Help,
            Author = scriptModel.Author,
            Outputs = scriptModel.Outputs,
            Parameters = scriptModel.Parameters,
            MinimumVersion = scriptModel.MinimumVersion
        };
    }

    /// <summary>
    /// Updates this script from new code
    /// </summary>
    /// <param name="code">the code</param>
    /// <returns>the result of the update</returns>
    public Result<bool> UpdateFromCode(string code)
    {
        var result = new ScriptParser().Parse(this.Name, code, this.Type);
        if (result.Failed(out string error))
            return Result<bool>.Fail(error);
        
        var scriptModel = result.Value;
        if(string.IsNullOrWhiteSpace(scriptModel.Name) == false)
            this.Name = scriptModel.Name;
        this.Code = scriptModel.Code;
        this.Revision = scriptModel.Revision;
        this.Help = scriptModel.Help;
        this.Description = scriptModel.Description;
        this.Author = scriptModel.Author;
        this.Outputs = scriptModel.Outputs;
        this.Parameters = scriptModel.Parameters;
        this.MinimumVersion = scriptModel.MinimumVersion;
        return true;
    }
}

// /// <summary>
// /// Definition of a script output node
// /// </summary>
// public class ScriptOutput
// {
//     /// <summary>
//     /// Gets or sets the output index
//     /// </summary>
//     public int Index { get; set; }
//
//     /// <summary>
//     /// Gets or sets the description of the output
//     /// </summary>
//     public string Description { get; set; } = string.Empty;
// }

/// <summary>
/// A parameter passed into a script
/// </summary>
public class ScriptParameter
{
    /// <summary>
    /// Gets or sets the name of the parameter
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the parameter
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets options for the parameter
    /// </summary>
    public string[]? Options { get; set; } 
    
    /// <summary>
    /// Gets or sets the type of script argument
    /// </summary>
    public ScriptArgumentType Type {get; set; }
}

/// <summary>
/// Types of script parameters
/// </summary>
public enum ScriptArgumentType
{
    /// <summary>
    /// String parameter
    /// </summary>
    String, 
    /// <summary>
    /// Integer parameter
    /// </summary>
    Int,
    /// <summary>
    /// Boolean parameter
    /// </summary>
    Bool,
    /// <summary>
    /// Select for a single value
    /// </summary>
    Select,
    /// <summary>
    /// Select for an array of values
    /// </summary>
    SelectMultiple
}