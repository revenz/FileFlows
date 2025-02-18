using FileFlowsScriptRepo.Generators;

args = new[] { "--repo" };

if(args[0] == "--repo" || args[0] == "--generate")
{
    RepoGenerator.Run();
    DockerModGenerator.Run();
    return;
}

string code = File.ReadAllText(args[0]);

var cl = CommandLine.Parse(args);


if(args[0].Contains("System") == false)
{
    // its a flow script

    // will throw exception if invalid
    var script = new ScriptParser().Parse("ScriptNode", code);
    if(script == null)
    {
        Console.WriteLine("Failed to parse script");
        return;   
    }


    // build up the entry point
    string epParams = string.Join(", ", script.Parameters?.Select(x => x.Name)?.ToArray() ?? new string[] {});
    // all scripts must contain the "Script" method we then add this to call that 
    string entryPoint = $"var scriptResult = Script({epParams});\nexport const result = scriptResult;";

    code = (code + "\n\n" + entryPoint).Replace("\t", "   ").Trim();

}
var logger = new Logger();

var executor = new Executor();
executor.Code = code;
executor.Logger = new FileFlows.ScriptExecution.Logger();
executor.Logger.ELogAction = (largs) => ScriptLogger.ELog(largs);
executor.Logger.WLogAction = (largs) => ScriptLogger.WLog(largs);
executor.Logger.ILogAction = (largs) => ScriptLogger.ILog(largs);
executor.Logger.DLogAction = (largs) => ScriptLogger.DLog(largs);
executor.HttpClient = new HttpClient();
executor.Variables = cl.Variables;
executor.AdditionalArguments = cl.Parameters;
executor.SharedDirectory = new DirectoryInfo("Scripts/Shared").FullName;

executor.Execute();