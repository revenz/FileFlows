using System.Reflection;

namespace FileFlows.Server.Cli;

/// <summary>
/// A command that will be executed
/// </summary>
public abstract class Command
{
    /// <summary>
    /// Runs the command
    /// </summary>
    /// <returns>true if the command exits the application</returns>
    public abstract bool Run(ILogger logger);
    /// <summary>
    /// Gets the switch for this command
    /// </summary>
    public abstract string Switch { get; }
    /// <summary>
    /// Gets the description for this command
    /// </summary>
    public abstract string Description { get; }
    /// <summary>
    /// Gets if this command should be printed when running the help command
    /// </summary>
    public virtual bool PrintToConsole => true;

    /// <summary>
    /// Parse the command arugments
    /// </summary>
    /// <param name="logger">The logger to use</param>
    /// <param name="args">the command line arguments</param>
    /// <exception cref="Exception">throws if an argument is missing</exception>
    public virtual void ParseArguments(ILogger logger, string[] args)
    {
        var commandLineArgs = this.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.GetCustomAttribute<CommandLineArg>() != null)
            .ToDictionary(p => p.GetCustomAttribute<CommandLineArg>().Switch);

        List<string> passedIn = new(); 
        for (int i = 0; i < args.Length; i++)
        {
            string arg;
            object value;
            PropertyInfo? p;
            
            if (i == 0 && commandLineArgs.TryGetValue("", out p))
            {
                arg = string.Empty;
                value = args[i];
            }
            else
            {
                arg = args[i].TrimStart('-', '/');
                if (commandLineArgs.TryGetValue(arg, out p) == false)
                    break;
                
                if (i == args.Length - 1)
                    break;
                
                value = args[++i];
            }

            passedIn.Add(arg);
            
            if (p.PropertyType == typeof(int))
            {
                if (int.TryParse(value as string, out int iValue))
                    value = iValue;
                else
                    throw new Exception($"Invalid argument '{p.Name}', a integer was expected");
            }
            else if (p.PropertyType == typeof(long))
                if (long.TryParse(value as string, out long l))
                    value = l;
                else
                    throw new Exception($"Invalid argument '{p.Name}', a long was expected");
            else if (p.PropertyType == typeof(bool))
                value = (value as string)?.ToLower() == "true" ||  (value as string) == "1";
            else if (p.PropertyType != typeof(string))
                throw new Exception("Unsupported argument type in code: " + p.Name);
            
            p.SetValue(this, value);
        }

        var missing = commandLineArgs.Values.Where(x =>
        {
            var att = x.GetCustomAttribute<CommandLineArg>();
            if (att.Optional)
                return false;
            return passedIn.Contains(att.Switch) == false;
        }).ToList();

        if (missing.Any())
        {
            int maxSwitchLength = missing.Max(m => m.GetCustomAttribute<CommandLineArg>().Switch.Length);

            foreach (var m in missing)
            {
                var att = m.GetCustomAttribute<CommandLineArg>();
                if(string.IsNullOrEmpty(att.MissingErrorOverride) == false)
                    logger.ILog(att.MissingErrorOverride);
                else
                    logger.ILog($"--{att.Switch.PadRight(maxSwitchLength)}: Is Required");
            }

            throw new Exception("Missing arguments");
        }
    }

    /// <summary>
    /// Prints the help for this command to the console
    /// </summary>
    internal void PrintHelp(ILogger logger)
    {
        logger.ILog("FileFlows");
        logger.ILog("  Command: " + GetType().Name);
        logger.ILog("  Summary: " + Description);
        var prop = this.GetType().GetProperty("Arguments");
        if (prop == null)
            return;

        logger.ILog("  Args:");
        SortedDictionary<string, string> parameters = new();
        var type = prop.PropertyType;
        foreach (var p in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            var att = p.GetCustomAttribute<CommandLineArg>();
            if (att == null)
                continue;
            var index = att.Switch;
            var name = p.Name;
            parameters.Add(index, name);
        }

        // Find the maximum length of switch and property name
        int maxKeyLength = parameters.Keys.Max(k => k.Length);
        int maxValueLength = parameters.Values.Max(v => v.Length);

        // Print parameters with even spacing
        foreach (var p in parameters)
        {
            logger.ILog($"--{p.Key.PadRight(maxKeyLength)} - {p.Value.PadRight(maxValueLength)}");
        }

    }
}