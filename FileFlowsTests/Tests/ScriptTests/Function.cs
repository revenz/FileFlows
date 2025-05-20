namespace FileFlowsTests.Tests.ScriptTests;

/// <summary>
/// A flow element that executes custom code
/// </summary>
public class Function
{

    /// <summary>
    /// Gets or sets the code to execute
    /// </summary>
    public string Code { get; set; }
    
    /// <inheritdoc />
    public int Execute(NodeParameters args)
    {
        if (string.IsNullOrEmpty(Code))
        {
            args.FailureReason = "No code specified in Function script";
            args.Logger?.ELog(args.FailureReason);
            return -1; // no code, flow cannot continue doesn't know what to do
        }

        try
        {
            return args.ScriptExecutor.Execute(new FileFlows.Plugin.Models.ScriptExecutionArgs
            {
                Args = args,
                Logger = args.Logger,
                TempPath = args.TempPath,
                Language = ScriptLanguage.JavaScript,
                ScriptType = ScriptType.Flow,
                AdditionalArguments = args.Variables,
                Code = Code
            });
        }
        catch (Exception ex)
        {
            args.FailureReason = "Failed executing function: " + ex.Message;
            args.Logger?.ELog("Failed executing function: " + ex.Message + Environment.NewLine + ex.StackTrace);
            return -1;
        }
    }
}