/// <summary>
/// Parses a script code block into a ScriptModel
/// </summary>
public class ScriptParser
{
    Regex rgxParameter = new Regex(@"(?<=(@param[\s]+))\{([^\}]+)\}[\s]+([\w]+)[\s]+(.*?)$");
    Regex rgxOutput = new Regex(@"(?<=(@output[\s]+))(.*?)$");
    
    /// <summary>
    /// Parses the code of a script and returns a ScriptModel
    /// </summary>
    /// <param name="name">the name of the script</param>
    /// <param name="code">the script to parse</param>
    /// <returns>a parsed model</returns>
    public ScriptModel Parse(string name, string code)
    {
        if (string.IsNullOrEmpty(code))
            throw new Exception("No script found");
        var rgxComments = new Regex(@"\/\*(\*)?(.*?)\*\/", RegexOptions.Singleline);
        var matchComments = rgxComments.Match(code.Trim());
        if (matchComments.Success == false)
            throw new Exception("Failed to locate comment section.  A script must start with a comment block describing the script.");
        var comments = matchComments.Value.Trim()[1..^1];
        // remove the start * 
        comments = string.Join("\n", comments.Replace("\r\n", "\n").Split('\n')
            .Select(x => Regex.Replace(x, @"^[\s]*[\*]+[\s]*", ""))).Trim();

        ScriptModel model = new()
        {
            Name = name,
            Outputs = new (),
            Parameters = new()
        };
        int atIndex = comments.IndexOf("@", StringComparison.Ordinal);
        if (atIndex < 1)
            throw new Exception("No output parameters found");
        
        model.Description = comments.Substring(0, atIndex).Trim();
        if (string.IsNullOrEmpty(model.Description))
            throw new Exception("No description found in comments");
        
        comments = comments.Substring(atIndex);

        foreach (var line in comments.Split('\n'))
        {
            if (ParseArgument(model, line))
                continue;
            if (ParseOutput(model, line))
                continue;
            if (line.StartsWith("@author ") || line.StartsWith("@version ") || line.StartsWith("@revision "))
                continue;
            Console.WriteLine("Unexpected line [" + line + "]");
            throw new Exception("Unexpected line: " + line);
        }

        if (model.Outputs.Count == 0)
            throw new Exception("No outputs defined.  You must define at least one output node");

        return model;
    }

    /// <summary>
    /// Parse a comment line and if argument will add it ot the model
    /// </summary>
    /// <param name="model">the ScriptModel to add the argument to</param>
    /// <param name="line">the comment line to parse</param>
    /// <returns>true if parsed as a argument</returns>
    /// <exception cref="Exception">throws exception if line is invalid</exception>
    private bool ParseArgument(ScriptModel model, string line)
    {
        var paramMatch = rgxParameter.Match(line);
        if (paramMatch.Success == false)
            return false;
        var param = new ScriptParameter();
        switch (paramMatch.Groups[2].Value.ToLower())
        {
            case "bool":
                param.Type = ScriptArgumentType.Bool;
                break;
            case "string":
                param.Type = ScriptArgumentType.String;
                break;
            case "int":
                param.Type = ScriptArgumentType.Int;
                break;
            default:
                throw new Exception("Invalid parameter type: " + paramMatch.Groups[2].Value);
        }

        try
        {
            param.Name = paramMatch.Groups[3].Value;
            param.Description = paramMatch.Groups[4].Value;
            model.Parameters.Add(param);
            return true;
        }
        catch (Exception)
        {
            throw new Exception("Invalid parameter: " + line);
        }
    }

    
    /// <summary>
    /// Parse a comment line and if an output will add it ot the model
    /// </summary>
    /// <param name="model">the ScriptModel to add the argument to</param>
    /// <param name="line">the comment line to parse</param>
    /// <returns>true if parsed as a output</returns>
    /// <exception cref="Exception">throws exception if line is invalid</exception>
    private bool ParseOutput(ScriptModel model, string line)
    {
        var match = rgxOutput.Match(line);
        if (match.Success == false)
            return false;
        ScriptOutput output = new ();
        output.Index = model.Outputs.Count + 1;
        output.Description = match.Value;
        model.Outputs.Add(output);
        return true;
    }
}