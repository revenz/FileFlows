namespace FileFlowsTests.Tests.ScriptTests;

/// <summary>
/// Test base for script tests
/// </summary>
public abstract class ScriptTest : TestBase
{
    
    /// <summary>
    /// Executes a script
    /// </summary>
    /// <param name="code">the code to execute</param>
    /// <param name="parameters">the parameters</param>
    /// <returns>the result of the execution</returns>
    protected ExecuteResult ExecuteScript(string code, Dictionary<string, object> parameters)
    {
        var logger = new TestLogger();
        NodeParameters args = new (null, logger, false, null, null);
        
        string epParams = string.Join(", ", parameters.Keys.ToArray());
        string entryPoint = $"var scriptResult = Script({epParams});\nexport const result = scriptResult;";
        code += "\n" + entryPoint;
        
        var result = new ScriptExecutor().Execute(new()
        {
            Args = args,
            Logger = logger,
            ScriptType = ScriptType.Flow,
            Code = code,
            AdditionalArguments = parameters
        });
        return new ExecuteResult()
        {
            ExitCode = result,
            Log = logger.ToString()
        };
    }
}

/// <summary>
/// A script execution result
/// </summary>
public class ExecuteResult
{
    /// <summary>
    /// Gets or sets the exit code from the execution
    /// </summary>
    public int ExitCode { get; set; }

    /// <summary>
    /// Gets or sets the log of the execution
    /// </summary>
    public string Log { get; set; } = string.Empty;
}