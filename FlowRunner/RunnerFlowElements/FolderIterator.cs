using System.Text.RegularExpressions;
using FileFlows.FlowRunner.Helpers;
using FileFlows.Plugin;
using FileFlows.Plugin.Helpers;
using FileFlows.ServerShared;
using FileFlows.Shared;
using FileFlows.Shared.Models;
using Humanizer;

namespace FileFlows.FlowRunner.RunnerFlowElements;

/// <summary>
/// A special flow element that iterates all files in a folder and process them through a sub flow
/// </summary>
public class FolderIterator : Node
{
    /// <summary>
    /// Gets or sets the flow to execute
    /// </summary>
    private Flow Flow { get; init; }
    
    /// <summary>
    /// Gets or sets the runner executing this flow
    /// </summary>
    private Runner Runner { get; init; }
    
    /// <summary>
    /// Gets or sets the folder to iterate
    /// </summary>
    public string? Folder { get; set; }
    
    /// <summary>
    /// Gets or set the pattern to iterate over
    /// </summary>
    public string? Pattern { get; set; }

    /// <summary>
    /// Gets or sets if all files or just the top folder files should be iterated over
    /// </summary>
    public bool Recursive { get; set; }
    
    /// <inheritdoc />
    public override int Execute(NodeParameters args)
    {
        if (string.IsNullOrWhiteSpace(Folder))
        {
            args.FailureReason = "No folder given";
            args.Logger?.ELog(args.FailureReason);
            return -1;
        }

        var localPathResult = args.FileService.GetLocalPath(args.ReplaceVariables(Folder, stripMissing: true));
        if (localPathResult.Failed(out string error))
        {
            args.FailureReason = error;
            args.Logger?.ELog(args.FailureReason);
            return -1;
        }

        var localPath = localPathResult.Value;
        if (System.IO.Directory.Exists(localPath) == false)
        {
            args.FailureReason = "Folder does not exist: " + localPath;
            args.Logger?.ELog(args.FailureReason);
            return -1;
        }
        
        var files = System.IO.Directory.GetFiles(localPath, Pattern?.StartsWith('*') == true? Pattern : "*.*",
            Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).ToList();
        
        if (string.IsNullOrWhiteSpace(Pattern) == false && Pattern.StartsWith('*') == false)
        {
            string pattern = Pattern.ToLowerInvariant() switch
            {
                "images" => @"\.(jpg|jpeg|jpe|jp2|png|gif|bmp|webp)$",
                "videos" => @"\.(ts|mp4|mkv|avi|mpe|mpeg|mov|mpv|flv|wmv|webm|avchd|h264|h265)$",
                "audio" or "music" => @"\.(mp3|wav|ogg|aac|wma|flac|alac|m4a|m4p)$",
                _ => Pattern
            };
            var regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            files = files.Where(x => regex.IsMatch(x)).ToList();
        }

        if (files.Count == 0)
        {
            args.Logger.ILog("No files found to process");
            return 2;
        }
        
        // now we have the files to iterate, we have to create a new NodeParameters, we dont want to mess with our existing one

        var current = 0;
        var total = files.Count;
        args.PartPercentageUpdate(0);
        foreach (var file in files)
        {
            args.Logger?.ILog("Starting new file: " + file);
            args.RecordAdditionalInfo("File", Plugin.Helpers.FileHelper.GetShortFileName(file), 1, null);
            
            var result = RunFlow(args, file, ++current);

            // clear the info that was set
            args.RecordAdditionalInfo("Element", null, 0, null);
            args.RecordAdditionalInfo("File", null, 0, null);

            if (result.Failed(out error))
            {
                args.FailureReason = error;
                args.Logger?.ELog(error);
                return -1;
            }
            if (result != 1 && result != 0)
            {
                args.FailureReason = "Unexpected result from file: " + result;
                args.Logger?.ELog(args.FailureReason);
                return -1;
            }

            var progress = ((float)current) / total * 100f;
            args.PartPercentageUpdate(progress);
        }
        args.PartPercentageUpdate(100);

        return 1;
    }

    /// <summary>
    /// Construct the new NodeParameters to use against the file
    /// </summary>
    /// <param name="args">the existing NodeParameters</param>
    /// <param name="file">the file we will be iterating</param>
    /// <returns>the new NodeParameters for that file</returns>
    private NodeParameters NewNodeParameters(NodeParameters args, string file)
    {
        var newLogger = args.Logger;//new FlowLogger(null);
        var newArgs = new NodeParameters(file, newLogger, false, args.LibraryPath, args.FileService)
        {
            Node = args.Node,
            LicenseLevel = args.LicenseLevel,
            LibraryFileName = args.LibraryFileName,
            IsRemote = args.IsRemote,
            LogImageActual = args.LogImageActual, //newLogger.Image,
            ImageHelper = args.ImageHelper,
            NotificationCallback = args.NotificationCallback
        };
        newArgs.ArchiveHelper = new ArchiveHelper(newArgs);

        newArgs.HasPluginActual = args.HasPluginActual;
        newArgs.UploadFile = args.UploadFile;
        newArgs.DeleteRemote = args.DeleteRemote;
        newArgs.SendEmail = args.SendEmail;
        newArgs.RenderTemplate = args.RenderTemplate;
        newArgs.IsDocker = args.IsDocker;
        newArgs.IsWindows = args.IsWindows;
        newArgs.IsLinux = args.IsLinux;
        newArgs.IsMac = args.IsMac;
        newArgs.IsArm = args.IsArm;
        newArgs.PathMapper = args.PathMapper;
        newArgs.PathUnMapper = args.PathUnMapper;
        newArgs.ScriptExecutor = args.ScriptExecutor;
        foreach (var variable in args.Variables)
        {
            if(newArgs.Variables.ContainsKey(variable.Key) == false)
                newArgs.Variables.TryAdd(variable.Key, variable.Value);
        }
        
        newArgs.RunnerUid = args.RunnerUid;
        newArgs.TempPath = args.TempPath;
        newArgs.TempPathName = args.TempPathName;
        newArgs.RelativeFile = args.RelativeFile;
        newArgs.PartPercentageUpdate = _ => { };

        newArgs.Result = NodeResult.Success;
        newArgs.GetToolPathActual = args.GetToolPathActual;
        newArgs.GetPluginSettingsJson = args.GetPluginSettingsJson;
        newArgs.StatisticRecorderRunningTotals = args.StatisticRecorderRunningTotals;
        newArgs.StatisticRecorderAverage = args.StatisticRecorderAverage;
        newArgs.AdditionalInfoRecorder = args.AdditionalInfoRecorder;
        
        return newArgs;
    }

    /// <summary>
    /// Loads a folder iterator from a part
    /// </summary>
    /// <param name="part">the part to load it from</param>
    /// <param name="runner">the runner being used</param>
    /// <returns>the folder iterator instance</returns>
    public static Result<Node> Load(FlowPart part, Runner runner)
    {
        // special case, don't use the BasicNodes execution of this, use the runners execution,
        // we have more control and can load it as a sub flow
        if (part.Model is IDictionary<string, object> dictModel == false)
            return Result<Node>.Fail("Failed to load model for Folder Iterator flow element.");

        if (dictModel.TryGetValue("Flow", out object? oFlow) == false || oFlow == null)
            return Result<Node>.Fail("Failed to get flow from Folder Iterator model.");
        ObjectReference? orFlow;
        string json = JsonSerializer.Serialize(oFlow);
        try
        {
            orFlow = JsonSerializer.Deserialize<ObjectReference>(json, new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception)
        {
            return Result<Node>.Fail("Failed to load Folder Iterator model from: " + json);
        }

        if (orFlow == null)
            return Result<Node>.Fail("Failed to load Folder Iterator model from: " + json);


        var flow = runner.runInstance.Config.Flows.FirstOrDefault(x => x.Uid == orFlow.Uid);
        if (flow == null)
            return Result<Node>.Fail("Failed to locate Flow defined in the Folder Iterator flow element.");
        var folderIterator = new FolderIterator()
        {
            Flow = flow,
            Runner = runner
        };
        if (dictModel.TryGetValue(nameof(Folder), out var oFolder) && oFolder != null)
            folderIterator.Folder = oFolder.ToString();
        if (dictModel.TryGetValue(nameof(Pattern), out var oPattern) && oPattern != null)
            folderIterator.Pattern = oPattern.ToString();
        if (dictModel.TryGetValue(nameof(Recursive), out var oAllFiles) && oAllFiles != null)
            folderIterator.Recursive = oAllFiles.ToString().Equals("true", StringComparison.InvariantCultureIgnoreCase);

        return folderIterator;
    }

    /// <summary>
    /// Runs the flow against the file
    /// </summary>
    /// <param name="args">the current NodeParameters</param>
    /// <param name="file">the file to run it against</param>
    /// <returns>the output from the flow</returns>
    private Result<int> RunFlow(NodeParameters args, string file, int fileCount)
    {
        var part = Flow.Parts.FirstOrDefault(x => x.Inputs == 0);
        if (part == null)
            return Result<int>.Fail("Failed to find Input node");
        
        var newArgs = NewNodeParameters(args, file);
        
        int count = 0;
        while (++count < Math.Min(Runner.runInstance.Config.MaxNodes, 250))
        {
            if (Runner.CancellationToken.IsCancellationRequested || Runner.Canceled)
                return Result<int>.Fail("Flow was canceled");
            if (part == null) // always false, but just in case code changes and this is no longer always false
                return Result<int>.Fail("Flow part was null");
            
            Node? currentFlowElement = null;
            TemporaryLogger? loadFELogger = new(); // log this to a string, so we can include it in the flow element start
            try
            {
                var lfeResult = new FlowHelper(Runner.runInstance).LoadFlowElement(loadFELogger, part, newArgs.Variables, Runner);
                if(lfeResult.Failed(out var lfeError) || lfeResult.Value == null)
                {
                    if(string.IsNullOrWhiteSpace(lfeError) == false)
                        newArgs.Logger?.ELog(lfeError);
                    
                    // happens when canceled or when the node failed to load
                    return Result<int>.Fail(lfeError?.EmptyAsNull() ?? "Failed to load flow element: " + part.Name +
                          "\nEnsure you have the required plugins installed.");
                }

                currentFlowElement = lfeResult.Value;
                Runner.CurrentFlowElement = currentFlowElement;

                if (currentFlowElement is SubFlowOutput sfOutput)
                    return sfOutput.Output;

                newArgs.RecordAdditionalInfo("Element", currentFlowElement.Name?.EmptyAsNull() ?? currentFlowElement.GetType().Name.Humanize(), 1, null);
                
                newArgs.Logger?.ILog(new string('-', 70));
                newArgs.Logger?.ILog(
                    $"Iterative Flow Element {(Runner.Info.LibraryFile.ExecutedNodes.Count + 1)}.{fileCount}.{count}: {part.Label?.EmptyAsNull() ?? part.Name?.EmptyAsNull() ?? currentFlowElement.Name} [{currentFlowElement.GetType().FullName}]");
                newArgs.Logger?.ILog(new string('-', 70));
                newArgs.Logger?.ILog("Working File: " + newArgs.WorkingFile);
                loadFELogger.WriteToLog(newArgs.Logger);
                

                // clear the failure reason, if this isn't a failure flow, if it is, we already have the reason for failure
                newArgs.FailureReason = null;
                
                if (currentFlowElement.PreExecute(newArgs) == false)
                    return Result<int>.Fail("PreExecute failed");
                int output = currentFlowElement.Execute(newArgs);
                if (output is RunnerCodes.TerminalExit or RunnerCodes.RunCanceled)
                    return output; // just finish this, the flow element that caused the terminal exit already was recorded
                
                if (Runner.Canceled)
                    return RunnerCodes.RunCanceled;
                
                if (output == RunnerCodes.Failure && part.ErrorConnection == null)
                {
                    // the execution failed                     
                    return Result<int>.Fail("Flow Element returned error code: " + currentFlowElement!.Name);
                }

                var outputNode = output == RunnerCodes.Failure
                    ? part.ErrorConnection
                    : part.OutputConnections?.FirstOrDefault(x => x.Output == output);
                
                if (outputNode == null)
                {
                    if(string.IsNullOrWhiteSpace(Flow.Name) == false)
                        newArgs.Logger?.ILog($"Flow '{Flow.Name}' completed");
                    else
                        newArgs.Logger?.ILog($"Flow completed");
                    // flow has completed
                    return 0;
                }
                
                var newPart = Flow.Parts.FirstOrDefault(x => x.Uid == outputNode.InputNode);
                
                if (newPart == null)
                {
                    // couldn't find the connection, maybe bad data, but flow has now finished
                    newArgs.Logger?.WLog("Couldn't find output, flow completed: " + outputNode?.Output);
                    return outputNode?.Output == -1 ? RunnerCodes.Failure : RunnerCodes.Completed;
                }
                
                part = newPart;
            }
            catch (Exception ex)
            {
                return Result<int>.Fail(ex.Message);
            }
        }

        return 0;
    }
}