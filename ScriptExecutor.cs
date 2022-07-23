using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Jint;
using Jint.Runtime;
/// <summary>
/// Executes a script
/// </summary>
class ScriptExecutor 
{
    
    /// <summary>
    /// Delegate used by the executor so log messages can be passed from the javascript code into the flow runner
    /// </summary>
    /// <param name="values">the parameters for the logger</param>
    delegate void LogDelegate(params object[] values);
    
    /// <summary>
    /// Executes javascript
    /// </summary>
    /// <param name="execArgs">the execution arguments</param>
    /// <returns>the output to be called next</returns>
    public int Execute(ScriptExecutionArgs execArgs)
    {
        if (string.IsNullOrEmpty(execArgs?.Code))
            return -1; // no code, flow cannot continue doesnt know what to do
        var logger = new Logger();
        try
        {
            // replace Variables. with dictionary notation
            string tcode = execArgs.Code;
            tcode = Regex.Replace(tcode, @"(\.\.\/)+Shared\/", "./");

            foreach(Match match in Regex.Matches(tcode, @"import[\s]+{[^}]+}[\s]+from[\s]+['""]([^'""]+)['""]"))
            {
                var importFile = match.Groups[1].Value;
                if(importFile.EndsWith(".js") == false)
                    tcode = tcode.Replace(importFile, importFile + ".js");
            }

            tcode = tcode.Replace("Flow.Execute(", "Execute(");
            var http = new System.Net.Http.HttpClient();
                

            var sb = new StringBuilder();
            var log = new
            {
                ILog = new LogDelegate(logger.ILog),
                DLog = new LogDelegate(logger.DLog),
                WLog = new LogDelegate(logger.WLog),
                ELog = new LogDelegate(logger.ELog)
            };
            var engine = new Engine(options =>
            {
                options.AllowClr();
                options.EnableModules(@"D:\src\FileFlows\FileFlowsScriptRepo\Shared");
            })
            .SetValue("Logger", logger)
            .SetValue("Variables", execArgs.Variables)
            .SetValue("Sleep", (int milliseconds) => Thread.Sleep(milliseconds))
            .SetValue("http", http)
            .SetValue("MissingVariable", (string variableName) => {
                logger.ELog("MISSING VARIABLE: " + variableName + Environment.NewLine + $"The required variable '{variableName}' is missing and needs to be added via the Variables page.");
                throw new MissingVariableException();
            })
            //.SetValue("Flow", args)
            .SetValue("Hostname", Environment.MachineName)
            .SetValue("Execute", (object eArgs) => {
                string json = JsonSerializer.Serialize(eArgs);
                var jsonOptions = new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true
                };
                var eeARgs = JsonSerializer.Deserialize<ExecuteArgs>(JsonSerializer.Serialize(eArgs), jsonOptions);
                //var result = args.Execute(eeARgs);
                //logger.ILog("result:", result);
                //return result;
             })
            .SetValue(nameof(FileInfo), new Func<string, FileInfo>((string file) => new FileInfo(file)))
            .SetValue(nameof(DirectoryInfo), new Func<string, DirectoryInfo>((string path) => new DirectoryInfo(path))); ;
            foreach (var arg in execArgs.AdditionalArguments ?? new ())
                engine.SetValue(arg.Key, arg.Value);

            //var engineOutput = engine.Evaluate(tcode);
            engine.AddModule("Script", tcode);
            var ns = engine.ImportModule("Script");
            var result = ns.Get("result");            
            try{
            if(result != null){
                int num = (int)result.AsNumber();

                logger.ILog("Script result: " + num);
                return num;
            }
            }catch(Exception){}
            return 0;
        }
        catch(JavaScriptException ex)
        {
            // print out the code block for debugging
            int lineNumber = 0;
            string[] lines = execArgs.Code.Split('\n') ?? new string[] {};
            string pad = "D" + lines.ToString()?.Length;
            logger.DLog("Code: " + Environment.NewLine +
                string.Join("\n", lines.Select(x => (++lineNumber).ToString("D3") + ": " + x)));

            //logger?.ELog($"Failed executing script [{ex.LineNumber}, {ex.Column}]: {ex.Message}");
            logger?.ELog($"Failed executing script: {ex.Message}");
            return -1;

        }
        catch (Exception ex)
        {
            while(ex.InnerException != null)
                ex = ex.InnerException;
            if(ex is MissingVariableException == false)
                logger?.ELog("Failed executing script: " + ex.Message + Environment.NewLine + ex.StackTrace);
            return -1;
        }
    }
}

public class MissingVariableException:Exception
{

}