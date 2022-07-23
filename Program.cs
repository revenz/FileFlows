if(args[0] == "--repo" || args[0] == "--generate")
{
    RepoGenerator.Run();
    return;
}

string code = File.ReadAllText(args[0]);

var cl = CommandLine.Parse(args);

var execArgs = new ScriptExecutionArgs
{
    Code = code,
    AdditionalArguments = new (),
    Variables = cl.Variables
};


if(args[0].Contains("Process") == false)
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

    execArgs.Code = (code + "\n\n" + entryPoint).Replace("\t", "   ").Trim();

}

execArgs.AdditionalArguments = cl.Parameters;
new ScriptExecutor().Execute(execArgs);