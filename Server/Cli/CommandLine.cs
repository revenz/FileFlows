using System.Text.RegularExpressions;

namespace FileFlows.Server.Cli;

/// <summary>
/// The command line interface parser
/// </summary>
internal static class CommandLine
{
    /// <summary>
    /// Attempts to process the command line
    /// </summary>
    /// <param name="args">the command line args</param>
    /// <returns>true if command line was processed and the application should now exit</returns>
    public static bool Process(string[] args)
    {
        if (args?.Any() != true)
            return false;

        var logger = new CliLogger();
        try
        {
            if (string.IsNullOrWhiteSpace(args[0]) || HelpArgument(args[0]))
            {
                PrintHelp(logger);
                return true;
            }
            var commands = GetCommands();
            for (int i = 0; i < args.Length; i++)
            {
                var cmdSwitch = args[i];
                var cliSwitch = cmdSwitch.TrimStart('-', '/').Replace("-", "").ToLowerInvariant();
                var command = commands.FirstOrDefault(x => x.Switch.ToLowerInvariant().Replace("-", "")
                                                           == cliSwitch);
                if (command == null)
                    continue;
                try
                {
                    command.ParseArguments(logger, args.Skip(i + 1).ToArray());
                }
                catch (Exception exArgs)
                {
                    command.PrintHelp(logger);
                    logger.ILog("");
                    logger.ILog(exArgs.Message);
                    return true;
                }

                if (command.Run(logger))
                    return true;
            }
        }
        catch (Exception ex)
        {
            logger.ELog(ex.Message);
            return true;
        }

        return false;
    }

    private static bool SecondArgumentHelp(string[] args)
    {
        if (args.Length < 2)
            return false;
        return HelpArgument(args[1]);

    }
    private static bool HelpArgument(string arg)
    {
        if (string.IsNullOrEmpty(arg))
            return false;

        return Regex.IsMatch(arg.ToLower(), @"^([\-]{0,2}|[/]?)(help|\?)$");
    }

    private static Command[] GetCommands()
    {
        var commands = typeof(Command).Assembly.GetTypes().Where(x =>
                x.BaseType == typeof(Command) && x.IsAbstract == false)
            .Select(x => Activator.CreateInstance(x) as Command)
            .Where(x => x != null).ToArray() ?? new Command[] { };
        return commands!;
    }

    internal static void PrintHelp(ILogger logger)
    {
        logger.ILog("FileFlows v" + Globals.Version);
        logger.ILog("");
        List<(string, string)> args = new ();
        foreach(var command in GetCommands())
        {
            try
            {
                if(command.PrintToConsole)
                    args.Add(("--" + command.Switch, command.Description));
            }
            catch (Exception) { }
        }
        args.Sort();
        var maxLength = args.Max(x => x.Item1.Length);
        foreach (var arg in args)
            logger.ILog(arg.Item1.PadRight(maxLength) + " : " + arg.Item2);
        
        logger.ILog("");
    }
}