namespace FileFlows.FlowRunner;

/// <summary>
/// Runner Codes
/// </summary>
public static class RunnerCodes
{
    /// <summary>
    /// Flow completed with no more connections available
    /// </summary>
    public const int Completed = 0;
    
    /// <summary>
    /// Code for a failure event, that can be handled (by failure flow or failure output)
    /// </summary>
    public const int Failure = -1;
    
    /// <summary>
    /// Gets the terminal exit code from the runner, this means, abort, completely cannot go to failure flow etc
    /// </summary>
    public const int TerminalExit = -9999;
        
    /// <summary>
    /// Code for a canceled flow, this has already been handled by what canceled it, no need to log again
    /// </summary>
    public const int RunCanceled = -9998;
}