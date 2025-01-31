using Microsoft.JSInterop;

namespace FileFlows.Client;

public class ClientConsoleLogger : ILogWriter
{
    public ClientConsoleLogger()
    {
        FileFlows.Shared.Logger.Instance.RegisterWriter(this);
    }

    public static IJSRuntime jsRuntime { get; set; }

    /// <summary>
    /// Logs a message
    /// </summary>
    /// <param name="type">the type of log message</param>
    /// <param name="args">the arguments for the log message</param>
    public async Task Log(LogType type, object[] args)
    {
        await jsRuntime.InvokeVoidAsync("ff.log", new object[] { (int)type, args });
    }

    public string GetTail(int length = 50) => "Not implemented";
}
