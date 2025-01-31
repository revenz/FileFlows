using System.Dynamic;
using System.Reflection;
using System.Text.RegularExpressions;
using FileFlows.FlowRunner.RunnerFlowElements;
using FileFlows.Plugin;
using FileFlows.Plugin.Attributes;
using FileFlows.Server;
using FileFlows.Shared;
using FileFlows.Shared.Models;

namespace FileFlows.FlowRunner.Helpers;

/// <summary>
/// Helper for executing the flow
/// </summary>
public class FlowHelper
{
    private readonly Dictionary<Guid, Flow> FlowInstances = new();

    /// <summary>
    /// the run instance running this
    /// </summary>
    private readonly RunInstance runInstance;

    /// <summary>
    /// Creates a new instance of the flow helper
    /// </summary>
    /// <param name="runInstance">the run instance running this</param>
    public FlowHelper(RunInstance runInstance)
    {
        this.runInstance = runInstance;
    }
    
    /// <summary>
    /// Creates the instance of the startup flow
    /// </summary>
    /// <param name="isRemote">if this is a remote flow and the file needs downloading</param>
    /// <param name="initialFlow">the initial flow to run after startup</param>
    /// <param name="workingFile">the path of the working file</param>
    /// <returns>the startup flow</returns>
    internal Flow GetStartupFlow(bool isRemote, Flow initialFlow, string workingFile)
    {
        FlowInstances[initialFlow.Uid] = initialFlow;

        var flow = new Flow { Parts = new() };
        var partStartup = new FlowPart()
        {
            Uid = Guid.NewGuid(),
            FlowElementUid = typeof(Startup).FullName,
            Name = "Startup",
            OutputConnections = new List<FlowConnection>(),
            Outputs = 1
        };
        flow.Parts.Add(partStartup);

        if (isRemote && Regex.IsMatch(workingFile, "^http(s)?://", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase) == false)
        {
            var partDownload = new FlowPart()
            {
                Uid = Guid.NewGuid(),
                FlowElementUid = typeof(FileDownloader).FullName,
                Name = "Downloading...",
                OutputConnections = new List<FlowConnection>(),
                Inputs = 1,
                Outputs = 1
            };
            flow.Parts.Add(partDownload);
            partStartup.OutputConnections.Add(new ()
            {
                Output = 1,
                Input = 1,
                InputNode = partDownload.Uid
            });
        }

        var partSubFlow = CreateSubFlowPart(initialFlow);

        flow.Parts.Last().OutputConnections.Add(new ()
        {
            Output = 1,
            Input = 1,
            InputNode = partSubFlow.Uid
        });
        flow.Parts.Add(partSubFlow);
        
        
        // connect the failure flow up to these so any sub flow will trigger a failure flow
        var failureFlow =
            runInstance.Config.Flows?.FirstOrDefault(x => x is { Type: FlowType.Failure, Default: true });
        if (failureFlow != null)
        {
            FlowPart fpFailure = new()
            {
                Uid = Guid.NewGuid(), // flow.Uid,
                FlowElementUid = typeof(ExecuteFlow).FullName,
                Name = failureFlow.Name,
                Inputs = 1,
                OutputConnections = new List<FlowConnection>(),
                Model = ((Func<ExpandoObject>)(() =>
                {
                    dynamic expandoObject = new ExpandoObject();
                    expandoObject.Flow = failureFlow;
                    return expandoObject;
                }))()
                
            };
            flow.Parts.Add(fpFailure);
            partSubFlow.ErrorConnection = new()
            {
                Output = -1,
                Input = 1,
                InputNode = fpFailure.Uid
            };
        }

        return flow;
    }

    /// <summary>
    /// Creates a flow part for a sub flow
    /// </summary>
    /// <param name="flow">the flow to create the sub flow part for</param>
    /// <returns>the flow part</returns>
    internal FlowPart CreateSubFlowPart(Flow flow)
        => new ()
        {
            Uid = Guid.NewGuid(), // flow.Uid,
            FlowElementUid = typeof(ExecuteFlow).FullName,
            Name = flow.Name,
            Inputs = 1,
            OutputConnections = new List<FlowConnection>(),
            Model = ((Func<ExpandoObject>)(() =>
            {
                dynamic expandoObject = new ExpandoObject();
                expandoObject.Flow = flow;
                return expandoObject;
            }))()
        };

    /// <summary>
    /// Loads a flow element instance
    /// </summary>
    /// <param name="logger">The logger used to log</param>
    /// <param name="part">the part in the flow</param>
    /// <param name="variables">the variables that are executing in the flow from NodeParameters</param>
    /// <param name="runner">the runner</param>
    /// <returns>the node instance</returns>
    /// <exception cref="Exception">If the flow element type cannot be found</exception>
    internal Result<Node> LoadFlowElement(ILogger logger, FlowPart part, Dictionary<string, object> variables, Runner runner)
    {
        if (part.Type == FlowElementType.Script)
        {
            // special type
            var nodeScript = new ScriptNode();
            nodeScript.Uid = part.Uid;
            nodeScript.Model = part.Model;
            if (Guid.TryParse(part.FlowElementUid[7..], out Guid scriptUid) == false) // 7 to remove "Scripts."
                return Result<Node>.Fail("Failed to parse script UID: " + part.FlowElementUid[7..]);

            var flowScript = runInstance.Config.FlowScripts.FirstOrDefault(x => x.Uid == scriptUid);
            if (flowScript == null)
                return Result<Node>.Fail("Script not found");

            nodeScript.Script = flowScript;
            if (string.IsNullOrWhiteSpace(part.Name))
                part.Name = flowScript.Name?.EmptyAsNull() ?? scriptUid.ToString();
            return nodeScript;
        }
        
        if (part.FlowElementUid.EndsWith(".GotoFlow"))
        {
            // special case, don't use the BasicNodes execution of this, use the runners execution,
            // we have more control and can load it as a sub flow
            if (part.Model is IDictionary<string, object> dictModel == false)
                return Result<Node>.Fail("Failed to load model for GotoFlow flow element.");

            if (dictModel.TryGetValue("Flow", out object? oFlow) == false || oFlow == null)
                return Result<Node>.Fail("Failed to get flow from GotoFlow model.");
            ObjectReference? orFlow;
            string json = JsonSerializer.Serialize(oFlow);
            try
            {
                orFlow = JsonSerializer.Deserialize<ObjectReference>(json,new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception)
            {
                return Result<Node>.Fail("Failed to load GotoFlow model from: " + json);
            }
            if(orFlow == null)
                return Result<Node>.Fail("Failed to load GotoFlow model from: " + json);


            var gotoFlow = runInstance.Config.Flows.FirstOrDefault(x => x.Uid == orFlow.Uid);
            if(gotoFlow == null)
                return Result<Node>.Fail("Failed to locate Flow defined in the GotoFlow flow element.");

            if (dictModel.TryGetValue("UpdateFlowUsed", out object? oUpdateFlowUsed) && oUpdateFlowUsed?.ToString()?.ToLowerInvariant() == "true")
            {
                runner.Info.LibraryFile.Flow = new()
                {
                    Uid = gotoFlow.Uid,
                    Name = gotoFlow.Name
                };
            }

            return new ExecuteFlow()
            {
                Flow = gotoFlow
            };
        }

        if (part.Type == FlowElementType.SubFlow)
        {
            string sUid = part.FlowElementUid[8..]; // remove SubFlow:
            var subFlow = runInstance.Config.Flows.FirstOrDefault(x => x.Uid.ToString() == sUid);
            if (subFlow == null)
                return Result<Node>.Fail($"Failed to locate sub flow '{sUid}'.");
            // add all the fields into the variables 
            if (subFlow.Properties?.Fields?.Any() == true && part.Model is IDictionary<string, object> subFlowModel)
            {
                foreach (var field in subFlow.Properties.Fields)
                {
                    if (string.IsNullOrWhiteSpace(field.FlowElementField))
                        continue;
                    if (subFlowModel.TryGetValue(field.Name, out object? fieldValue))
                    {
                        logger?.ILog(
                            $"Setting sub flow field variable [{field.FlowElementField}] = {fieldValue?.ToString() ?? "null"}");
                        variables[field.FlowElementField] = fieldValue;
                    }
                }
            }

            return new ExecuteFlow
            {
                Flow = subFlow,
                Properties = part.Model as IDictionary<string, object>,
            };
        }

        if (part.FlowElementUid.EndsWith(".FolderIterator"))
            return FolderIterator.Load(part, runner);
        if (part.FlowElementUid.EndsWith(".ListIterator"))
            return ListIterator.Load(part, runner);
        
        var nt = GetFlowElementType(part.FlowElementUid);
        if (nt == null)
        {
            return Result<Node>.Fail("Failed to load flow element: " + part.FlowElementUid);
        }

        var instance = CreateFlowElementInstance(logger, part, nt, variables);
        if (instance == null)
            return instance;
        if ((int)instance.LicenseLevel > (int)runner.runInstance.Config.LicenseLevel)
            return Result<Node>.Fail(
                $"The flow element {instance.GetType().Name} requires a {instance.LicenseLevel} license.");
        return instance;

    }

    /// <summary>
    /// Creates an instance of a flow element
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="part">the flow part to create an instance for</param>
    /// <param name="flowElementType">the flow element type</param>
    /// <param name="variables">the variables to load</param>
    /// <returns>an instance of the flow element</returns>
    public Node CreateFlowElementInstance(ILogger logger, FlowPart part, Type flowElementType, Dictionary<string, object> variables)
    {
        object? node;
        if (flowElementType == typeof(Startup))
            node = new Startup(runInstance);
        else if (flowElementType == typeof(FileDownloader))
            node = new FileDownloader(runInstance);
        else
            node = Activator.CreateInstance(flowElementType);
        
        if(node == null)
            return default;

        if (node is SubFlowOutput sfOutput && Regex.IsMatch(part.Name, @"[\d]$") && int.TryParse(part.Name[^1..], out int sfOutputValue))
        {
            // special case for a SubFlowOutput[1-9], these are special flow elements from the UI that shorthand a SubFlowOutput 
            sfOutput.Output = sfOutputValue;
            return sfOutput;
        }
        
        var properties = flowElementType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
        if (part.Model is IDictionary<string, object> dict)
        {
            foreach (var k in dict.Keys)
            {
                try
                {
                    if (k == "Name")
                        continue; // this is just the display name in the flow UI
                    var prop = properties.FirstOrDefault(x => x.Name == k);
                    if (prop == null)
                        continue;

                    if (dict[k] == null)
                        continue;

                    object  value;
                    if (dict[k] is JsonElement je && je.ValueKind == JsonValueKind.Number &&
                        prop.PropertyType == typeof(string))
                    {
                        // FF-1591: Video Has Stream.Channels went from int to string, others may do something similar
                        if (prop.GetCustomAttribute<MathValueAttribute>() != null)
                            value = "=" + je;
                        else
                            value = je.ToString();
                    }
                    else 
                        value = Converter.ConvertObject(prop.PropertyType, dict[k], Logger.Instance);
                    if (value != null)
                        prop.SetValue(node, value);
                }
                catch (Exception ex)
                {
                    runInstance.Logger?.ELog("Failed setting property: " + ex.Message + Environment.NewLine + ex.StackTrace);
                    runInstance.Logger?.ELog("Type: " + flowElementType.Name + ", Property: " + k);
                }
            }
        }
        
        // load any values that have been set by properties
        foreach (var prop in properties)
        {
            string strongName = part.Uid + "." + prop.Name;
            object? varValue;
            if (variables.TryGetValue(part.Uid + "." + prop.Name, out varValue) == false 
                    && variables.TryGetValue(strongName, out varValue) == false)
                continue;
            if (varValue == null)
                continue;

            if (varValue is JsonElement je)
            {
                switch (je.ValueKind)
                {
                    case JsonValueKind.False: varValue = (object)false;
                        break;
                    case JsonValueKind.True: varValue = (object)true;
                        break;
                    case JsonValueKind.String: varValue = je.GetString();
                        break;
                    case JsonValueKind.Number:
                        if (prop.PropertyType == typeof(long))
                            varValue = je.GetInt64();
                        else if (prop.PropertyType == typeof(float))
                            varValue = (float)je.GetDouble();
                        else if (prop.PropertyType == typeof(int))
                            varValue = je.GetInt32();
                        else if (prop.PropertyType == typeof(short))
                            varValue = je.GetInt16();
                        else if (je.TryGetInt32(out int i32))
                            varValue = i32;
                        else
                            continue;
                        break;
                }
            }

            logger?.ILog(strongName + " => Type Is: " + varValue.GetType().FullName);
            try
            {
                var value = Converter.ConvertObject(prop.PropertyType, varValue, Logger.Instance);
                if (value != null)
                    prop.SetValue(node, value);
            }
            catch (Exception ex)
            {
                logger.ELog("Failed setting variable: " + ex.Message);
            }
        }

        return (Node)node;
    }


    /// <summary>
    /// Loads the code for a script
    /// </summary>
    /// <param name="scriptUid">the UID of the script</param>
    /// <returns>the code of the script</returns>
    private string GetScriptCode(Guid scriptUid)
    {
        var file = new FileInfo(Path.Combine(runInstance.ConfigDirectory, "Scripts", "Flow", scriptUid + ".js"));
        if (file.Exists == false)
            return string.Empty;
        return File.ReadAllText(file.FullName);
    }
    
    
    /// <summary>
    /// Gets the flow element type from the full name of a flow element
    /// </summary>
    /// <param name="fullName">the full name of the flow element</param>
    /// <returns>the type if known otherwise null</returns>
    internal Type? GetFlowElementType(string fullName)
    {
        // special checks for our internal flow elements
        if (fullName.EndsWith("." + nameof(FileDownloader)))
            return typeof(FileDownloader);
        if (fullName.EndsWith("." + nameof(ExecuteFlow)))
            return typeof(ExecuteFlow);
        if (fullName.EndsWith("." + nameof(Startup)))
            return typeof(Startup);
        if (fullName.EndsWith(nameof(SubFlowInput)))
            return typeof(SubFlowInput);
        if (fullName.EndsWith(nameof(SubFlowOutput)) || fullName.StartsWith(nameof(SubFlowOutput)))
            return typeof(SubFlowOutput);

        var dlls = new DirectoryInfo(runInstance.WorkingDirectory).GetFiles("*.dll", SearchOption.AllDirectories);
        foreach (var dll in dlls)
        {
            try
            {
                //var assembly = Context.LoadFromAssemblyPath(dll.FullName);
                var assembly = Assembly.LoadFrom(dll.FullName);
                var types = assembly.GetTypes();
                var pluginType = types.FirstOrDefault(x => x.IsAbstract == false && x.FullName == fullName);
                if (pluginType != null)
                    return pluginType;
            }
            catch (Exception ex)
            {
                runInstance.Logger.WLog("Failed to load assembly: " + dll.FullName + " > " + ex.Message);
            }
        }

        runInstance.Logger.WLog(
            $"Failed to load '{fullName}' from any of the following DLLs:{Environment.NewLine} {string.Join(Environment.NewLine, dlls.Select(x => " - " + x.FullName))}");
        
        return null;
    }
}