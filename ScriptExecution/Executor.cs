using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FileFlows.Plugin;
using FileFlows.Plugin.Helpers;
using Jint;
using Jint.Runtime;
using Jint.Runtime.Interop;

namespace FileFlows.ScriptExecution;

/// <summary>
/// A Javascript code executor
/// </summary>
public class Executor
{
    /// <summary>
    /// Delegate used by the executor so log messages can be passed from the javascript code into the flow runner
    /// </summary>
    /// <param name="values">the parameters for the logger</param>
    delegate void LogDelegate(params object[] values);
    
    /// <summary>
    /// Gets or sets the variables that will be passed into the executed code
    /// </summary>
    public Dictionary<string, object> Variables { get; set; } = new ();

    /// <summary>
    /// Gets or sets the logger for the code execution
    /// </summary>
    public Logger Logger { get; set; } = null!;

    /// <summary>
    /// Gets or sets the code to execute
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the HTTP client to be used in the code execution
    /// </summary>
    public HttpClient HttpClient { get; set; } = null!;

    /// <summary>
    /// Gets or sets the additional arguments that will be passed into the code execution
    /// </summary>
    public Dictionary<string, object> AdditionalArguments { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// Gets or sets the directory where shared modules will be loaded from
    /// </summary>
    public string? SharedDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the process executor that is used by script to execute an external process
    /// </summary>
    public IProcessExecutor ProcessExecutor { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets if the code should be included in the failed logged
    /// </summary>
    public bool DontLogCode { get; set; }
    
    /// <summary>
    /// Static constructor for the executor
    /// </summary>
    static Executor()
    {
        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
        {
            string resourceName = new AssemblyName(args.Name).Name + ".dll";
            var resource = Array.Find(typeof(Executor).Assembly.GetManifestResourceNames(),
                element => element.EndsWith(resourceName));
            if (resource == null)
                return null;

            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource);
            if (stream == null)
                return null;
            
            byte[] assemblyData = new byte[stream.Length];
            var read = stream.Read(assemblyData, 0, assemblyData.Length);
            return Assembly.Load(assemblyData);
        };
    }

    /// <summary>
    /// Executes javascript
    /// </summary>
    /// <returns>the result of the execution</returns>
    public object? Execute()
    {
        if (string.IsNullOrEmpty(Code))
            return false; // no code, flow cannot continue doesnt know what to do
        try
        {
            // Create a wrapper object for LanguageHelper
            var languageHelperWrapper = new
            {
                GetEnglishFor = new Func<string, string>(LanguageHelper.GetEnglishFor),
                GetIso1Code = new Func<string, string>(LanguageHelper.GetIso1Code),
                GetIso2Code = new Func<string, string>(LanguageHelper.GetIso2Code),
                AreSame = new Func<string, string, bool>(LanguageHelper.AreSame)
            };

            AdjustVariables(Variables);
            
            // replace Variables. with dictionary notation
            string tcode = Code
                .Replace("Variables.Resource.", "Variables.resource.")
                .Replace("Variables.Resources.", "Variables.resource.")
                .Replace("Variables.resources.", "Variables.resource.");
            foreach (string k in Variables.Keys.OrderByDescending(x => x.Length))
            {
                // replace Variables.Key or Variables?.Key?.Subkey etc to just the variable
                // so Variables.file?.Orig.Name, will be replaced to Variables["file.Orig.Name"] 
                // since its just a dictionary key value 
                string keyRegex = @"Variables(\?)?\." + k.Replace(".", @"(\?)?\.");

                string replacement = "Variables['" + k + "']";
                if (k.StartsWith("file.") || k.StartsWith("folder."))
                {
                    // FF-301: special case, these are readonly, need to make these easier to use
                    if (Regex.IsMatch(k, @"\.(Date|Create|Modified)$"))
                        continue; // dates
                    if (Regex.IsMatch(k, @"\.(Year|Day|Month|Size)$"))
                        replacement = "Number(" + replacement + ")";
                    else
                        replacement += ".toString()";
                }
                
                // object? value = Variables[k];
                // if (value is JsonElement jElement)
                // {
                //     if (jElement.ValueKind == JsonValueKind.String)
                //         value = jElement.GetString();
                //     if (jElement.ValueKind == JsonValueKind.Number)
                //         value = jElement.GetInt64();
                // }

                tcode = Regex.Replace(tcode, keyRegex, replacement);
            }

            // remove this for FF-1663: ability to get output from the execute process while its running
            //tcode = tcode.Replace("Flow.Execute(", "Execute(");

            if (tcode.StartsWith("function") == false && tcode.IndexOf("export const result", StringComparison.Ordinal) < 0)
            {
                tcode = "function Script() {\n" + tcode + "\n}\n";
                tcode += $"var scriptResult = Script();\nexport const result = scriptResult;";
            }

            if (SharedDirectory != null) // can be null in unit tests
            {
                string sharedDir = SharedDirectory.Replace("\\", "/");
                if (sharedDir.EndsWith("/") == false)
                    sharedDir += "/";
                tcode = Regex.Replace(tcode, @"(?<=(from[\s](['""])))(\.\.\/)*Shared\/", sharedDir);
            }

            foreach(Match match in Regex.Matches(tcode, @"import[\s]+{[^}]+}[\s]+from[\s]+['""]([^'""]+)['""]"))
            {
                var importFile = match.Groups[1].Value;
                tcode = tcode.Replace(match.Value, "");
                if (importFile.EndsWith(".js") == false)
                    tcode = match.Value.Replace(importFile, importFile + ".js") + "\n" + tcode;
                else
                    tcode = match.Value + "\n" + tcode;
            }

            var processExecutor = this.ProcessExecutor ?? new BasicProcessExecutor(Logger);

            var engine = new Engine(options =>
            {
                options.AllowClr();
                if (string.IsNullOrEmpty(SharedDirectory) == false)
                {
                    Logger.ILog("Shared Directory for scripts: " + SharedDirectory);
                    options.EnableModules(SharedDirectory);
                }
                else
                {
                    Logger.WLog("No Shared Directory for scripts defined.");
                }
            })

            .SetValue("Logger", Logger)
            .SetValue("Checksum", new CheckSumHelper())
            .SetValue("Variables", Variables)
            .SetValue("Sleep", (int milliseconds) => Thread.Sleep(milliseconds))
            .SetValue("http", HttpClient)
            //.SetValue("CacheStore", CacheStore.Instance)
            .SetValue("LanguageHelper", languageHelperWrapper)
            .SetValue("StringContent", (string content) => new StringContent(content))
            .SetValue("JsonContent", (object content) =>
            {
                if (content is string == false)
                    content = JsonSerializer.Serialize(content);
                return new StringContent(content as string ?? string.Empty, Encoding.UTF8, "application/json");
            })
            .SetValue("FormUrlEncodedContent", (IEnumerable<KeyValuePair<string, string>> content) => new System.Net.Http.FormUrlEncodedContent(content))
            .SetValue("MissingVariable", (string variableName) => {
                Logger.ELog("MISSING VARIABLE: " + variableName + Environment.NewLine + $"The required variable '{variableName}' is missing and needs to be added via the Variables page.");
                throw new MissingVariableException();
            })
            .SetValue("Hostname", Environment.MachineName)
            // .SetValue("Execute", (object eArgs) => {
            //    var jsonOptions = new JsonSerializerOptions()
            //    {
            //        PropertyNameCaseInsensitive = true
            //    };
            //    var eeARgs = JsonSerializer.Deserialize<ProcessExecuteArgs>(JsonSerializer.Serialize(eArgs), jsonOptions) ?? new ProcessExecuteArgs();
            //    var result = processExecutor.Execute(eeARgs);
            //    Logger.ILog("Exit Code: " + (result.ExitCode?.ToString() ?? "null"));
            //    return result;
            // })
            .SetValue(nameof(FileInfo), new Func<string, FileInfo>((string file) => new FileInfo(file)))
            .SetValue(nameof(DirectoryInfo), new Func<string, DirectoryInfo>((string path) => new DirectoryInfo(path)));
            
            // Expose the ExecuteArgs type to Jint
            engine.SetValue("ExecuteArgs", TypeReference.CreateTypeReference(engine, typeof(ExecuteArgs)));

            foreach (var arg in AdditionalArguments ?? new())
            {
                if (arg.Value is JsonElement je)
                {
                    switch (je.ValueKind)
                    {
                        case JsonValueKind.False:
                            engine.SetValue(arg.Key, false);
                            continue;
                        case JsonValueKind.True:
                            engine.SetValue(arg.Key, true);
                            continue;
                        case JsonValueKind.Number:
                            engine.SetValue(arg.Key, je.GetDouble());
                            continue;
                        case JsonValueKind.String:
                            engine.SetValue(arg.Key, je.GetString());
                            continue;
                        case JsonValueKind.Array:
                            // Convert JsonElement array to a .NET array
                            var array = je.EnumerateArray()
                                .Select(item => item.ValueKind switch
                                {
                                    JsonValueKind.False => (object)false,
                                    JsonValueKind.True => true,
                                    JsonValueKind.Number => item.GetDouble(),
                                    JsonValueKind.String => item.GetString(),
                                    _ => null // Handle unsupported value kinds, if needed
                                })
                                .ToArray();
                            engine.SetValue(arg.Key, array);
                            continue;
                    }   
                }
                
                engine.SetValue(arg.Key, arg.Value);
            }

            // if(DontLogCode == false)
            //     Logger.DLog("Executing code: \n\n" + tcode + "\n\n" + new string('-', 30));
            #if(DEBUG)
            Logger.ILog("TCode:\n" + tcode);
            #endif
            engine.Modules.Add("Script", tcode);
            var ns = engine.Modules.Import("Script");
            var result = ns.Get("result");
            try
            {
                if (result != null)
                {
                    try
                    {
                        int num = (int)result.AsNumber();
                        Logger.ILog("Script result: " + num);
                        return num;
                    }
                    catch (Exception)
                    {
                        bool bResult = (bool)result.AsBoolean();
                        Logger.ILog("Script result: " + bResult);
                        return bResult;
                    }
                }
            }
            catch (Exception)
            {
            }

            AdjustVariables(Variables);

            return true;
        }
        catch(JavaScriptException ex)
        {
            if (ex.Message == "true")
                return true;
            if (ex.Message == "undefined")
                return null;
            if (int.TryParse(ex.Message, out int code))
                return code;
            //if (DontLogCode == false)
            {
                // print out the code block for debugging
                int lineNumber = 0;
                var lines = Code.Split('\n');
                Logger.DLog("Code: " + Environment.NewLine +
                            string.Join("\n", lines.Select(x => (++lineNumber).ToString("D3") + ": " + x)));
            }

            Logger.ELog($"Failed executing script: {ex.Message}");
            return false;

        }
        catch (Exception ex)
        {
            if(ex is MissingVariableException == false)
                Logger.ELog("Failed executing script: " + ex.Message + Environment.NewLine + ex.StackTrace);
            return false;
        }
    }


    /// <summary>
    /// Adjusts the varaibles to remove any JsonElements
    /// </summary>
    /// <param name="variables">the varaibles</param>
    private void AdjustVariables(Dictionary<string, object> variables)
    {
        foreach (var key in variables.Keys)
        {
            var obj = variables[key];
            if (obj == null)
                continue;
            if (obj is JsonElement je == false)
                continue;
            if (je.ValueKind == JsonValueKind.False)
            {
                Logger?.ILog($"Adjust variable '{key}' to: false");
                variables[key] = false;
                continue;
            }
            if (je.ValueKind == JsonValueKind.True)
            {
                Logger?.ILog($"Adjust variable '{key}' to: true");
                variables[key] = true;
                continue;
            }
            if (je.ValueKind == JsonValueKind.String)
            {
                string sValue = je.GetString() ?? string.Empty;
                variables[key] = sValue;
                Logger?.ILog($"Adjust variable '{key}' to: '{sValue}'");
                continue;
            }
            if (je.ValueKind == JsonValueKind.Number)
            {
                int iValue = je.GetInt32();
                variables[key] = iValue;
                Logger?.ILog($"Adjust variable '{key}' to: {iValue}");
                continue;
            }
        }
    }
}

/// <summary>
/// Exception that is thrown when a script is missing a Variable
/// </summary>
public class MissingVariableException : Exception
{

}
