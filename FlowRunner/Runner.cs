using FileFlows.ServerShared;
using FileFlows.Plugin;
using FileFlows.ServerShared.Services;
using FileFlows.Shared.Models;
using System.Text.RegularExpressions;
using FileFlows.FlowRunner.Helpers;
using FileFlows.FlowRunner.RunnerFlowElements;
using FileFlows.FlowRunner.TemplateRenders;
using FileFlows.Plugin.Helpers;
using FileFlows.Plugin.Services;
using FileFlows.RemoteServices;
using FileFlows.ServerShared.FileServices;
using FileFlows.Shared.Helpers;
using FlowHelper = FileFlows.FlowRunner.Helpers.FlowHelper;

namespace FileFlows.FlowRunner;

/// <summary>
/// A runner instance, this is called as a standalone application that is fired up when FileFlows needs to process a file
/// it exits when done, free up any resources used by this process
/// </summary>
public class Runner
{
    internal readonly RunInstance runInstance;
    //internal FlowExecutorInfo Info { get; private set; }
    private Flow Flow;
    private ProcessingNode Node;
    internal readonly CancellationTokenSource CancellationToken = new CancellationTokenSource();
    internal bool Canceled { get; private set; }
    private string WorkingDir;

    internal readonly List<Flow> ExecutedFlows = new();
    
    //private string ScriptDir, ScriptSharedDir, ScriptFlowDir;

    /// <summary>
    /// Creates an instance of a Runner
    /// </summary>
    /// <param name="runInstance">the run intance running this</param>
    /// <param name="flow">The flow that is being executed</param>
    /// <param name="node">The processing node that is executing this flow</param>
    /// <param name="workingDir">the temporary working directory to use</param>
    public Runner(RunInstance runInstance, Flow flow, ProcessingNode node, string workingDir)
    {
        this.runInstance = runInstance;
        runInstance.RpcClient.OnAbort += Abort;
        this.Flow = flow;
        this.Node = node;
        this.WorkingDir = workingDir;
    }

    /// <summary>
    /// Aborts the runner
    /// </summary>
    private void Abort()
    {
        this.Canceled = true;
        CancellationToken.Cancel();
    }

    private NodeParameters nodeParameters;

    /// <summary>
    /// Gets or sets the current executing flow element
    /// </summary>
    internal Node CurrentFlowElement { get; set; }


    /// <summary>
    /// Starts the flow runner processing
    /// </summary>
    public (FileStatus Status, bool KeepFiles) Run(FlowLogger logger)
    {
        var systemHelper = new SystemHelper(runInstance);
        FileStatus status = FileStatus.ProcessingFailed;
        try
        {
            try
            {
                systemHelper.Start();
                try
                {
                    status = RunActual(logger);
                }
                catch (Exception ex)
                {
                    nodeParameters?.Logger?.ELog("Error in runner: " + ex.Message + Environment.NewLine +
                                                 ex.StackTrace);

                    if (string.IsNullOrWhiteSpace(nodeParameters.FailureReason))
                        nodeParameters.FailureReason = "Error in runner: " + ex.Message;
                    if (runInstance.LibraryFile?.Status == FileStatus.Processing)
                        status = FileStatus.ProcessingFailed;
                }
            }
            catch (Exception ex)
            {
                runInstance.Properties.Logger.ELog("Failure in runner: " + ex.Message + Environment.NewLine + ex.StackTrace);
                status = FileStatus.ProcessingFailed;
            }

            try
            {
                Finish();
            }
            catch (Exception ex)
            {
                runInstance.Properties.Logger.ELog("Failed 'Finishing' runner: " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }
        finally
        {
            systemHelper.Stop();
        }

        logger.ILog($"Run status: {status}");
        return (status, status == FileStatus.ProcessingFailed);
    }

    /// <summary>
    /// Finish executing of a file
    /// </summary>
    public void Finish()
    {
        if(nodeParameters?.OriginalMetadata != null)
            runInstance.LibraryFile.OriginalMetadata = nodeParameters.OriginalMetadata;
        if (nodeParameters?.Metadata != null)
            runInstance.LibraryFile.FinalMetadata = nodeParameters.Metadata;

        runInstance.LibraryFile.ProcessingEnded = DateTime.UtcNow;
        if(nodeParameters != null) // this is null if it fails to remotely download the file
            CalculateFinalSize();
    }

    /// <summary>
    /// Calculates the final size of the file
    /// </summary>
    private void CalculateFinalSize()
    {
        if (nodeParameters.IsDirectory)
            runInstance.LibraryFile.FinalSize = nodeParameters.GetDirectorySize(nodeParameters.WorkingFile);
        else
            runInstance.LibraryFile.FinalSize = nodeParameters.LastValidWorkingFileSize;

        nodeParameters?.Logger?.ILog("Original Size: " + runInstance.LibraryFile.OriginalSize);
        nodeParameters?.Logger?.ILog("Final Size: " + runInstance.LibraryFile.FinalSize);
        runInstance.LibraryFile.OutputPath = Node.UnMap(nodeParameters.WorkingFile);
        nodeParameters?.Logger?.ILog("Output Path: " + runInstance.LibraryFile.OutputPath);
        if (string.IsNullOrWhiteSpace(nodeParameters.FailureReason) == false)
        {
            nodeParameters?.Logger?.ILog("Final Failure Reason: " + nodeParameters.FailureReason);
            runInstance.LibraryFile.FailureReason = nodeParameters.FailureReason;
        }

        runInstance.RpcClient.LibraryFileHandler.UpdateLibraryFile(runInstance.LibraryFile).Wait();
    }

    /// <summary>
    /// Starts processing a file
    /// </summary>
    /// <param name="logger">the logger used to log messages</param>
    private FileStatus RunActual(FlowLogger logger)
    {
        VariablesHelper.StartedAt = DateTime.Now;
        
        var cacheHelper = new CacheHelper(logger, (key) =>
        {
            try
            {
                var json = runInstance.RpcClient.Cache.GetJsonAsync(key).Result;
                return json ?? default;
            }
            catch (Exception)
            {
                // Ignore
                return null;
            }
        }, (key, json, expiration) =>
        {
            try
            {
                runInstance.RpcClient.Cache.StoreJsonAsync(key, json, expiration).Wait();
            }
            catch (Exception)
            {
                // Ignore
            }
        });
        
        nodeParameters = new NodeParameters(runInstance.LibraryFile.Name, logger,
            runInstance.Properties.IsDirectory, runInstance.Properties.LibraryPath, fileService: FileService.Instance,
            cancellationToken: CancellationToken.Token)
        {
            Node = new () { Uid = Node.Uid, Name = Node.Name, Type = Node.Address },
            Library = runInstance.LibraryFile.Library ?? new () { Uid = runInstance.LibraryFile.LibraryUid!.Value, 
                Name = runInstance.LibraryFile.Name, 
                Type = typeof(Library).FullName
            },
            LicenseLevel = (LicenseLevel)(int) runInstance.Properties.Config.LicenseLevel,
            LibraryFileName = runInstance.LibraryFile.Name,
            IsRemote = runInstance.Properties.IsRemote,
            LogImageActual = logger.Image,
            NotificationCallback = (severity, title, message) =>
            {
                runInstance.RpcClient.Basic.RecordNotification((NotificationSeverity)severity, title, message).Wait();
            },
            Cache = cacheHelper,
            SetThumbnailActual = (binaryData) =>
            {
                runInstance.RpcClient.LibraryFileHandler.SetThumbnail(binaryData).Wait();
            },
            SetDisplayNameActual = (displayName) =>
            {
                logger.ILog("Updating display name to : " + (displayName ?? "null"));
                runInstance.LibraryFile.Additional ??= new();
                runInstance.LibraryFile.Additional.DisplayName = displayName;
            },
            SetTraitsActual = (traits) =>
            {
                logger.ILog("Setting file traits: " + string.Join(", ", traits ?? []));
                runInstance.LibraryFile.Additional ??= new();
                runInstance.LibraryFile.Additional.Traits = traits;
            },
            GetPropertyActual = (property) 
                => runInstance.LibraryFile?.Additional?.Properties?.TryGetValue(property, out var value) == true ? value : null,
            SetPropertyActual = (property, value) =>
            {
                if(string.IsNullOrWhiteSpace(property))
                    return;
                runInstance.LibraryFile.Additional ??= new();
                if (string.IsNullOrWhiteSpace(value))
                {
                    logger.ILog($"Removing file property: {property} => {value}");
                    if(runInstance.LibraryFile.Additional.Properties?.ContainsKey(property) == true)
                        runInstance.LibraryFile.Additional.Properties.Remove(property);
                }
                else
                {
                    logger.ILog($"Setting file property: {property} => {value}");
                    runInstance.LibraryFile.Additional ??= new();
                    runInstance.LibraryFile.Additional.Properties[property] = value;
                }
            }
        };
        
        nodeParameters.Variables["library.Name"] = runInstance.LibraryFile.Library.Name;
        nodeParameters.Variables["library.Path"] = runInstance.Properties.LibraryPath;

        if (runInstance.LibraryFile.Additional?.FileDropUserUid != null &&
            runInstance.LibraryFile.Additional?.FileDropUserUid != Guid.Empty)
        {
            var uid = runInstance.LibraryFile.Additional.FileDropUserUid.Value;
            nodeParameters.Variables["FileDropUserUid"] = uid.ToString();
            nodeParameters.Variables["fdUserUid"] = uid.ToString();
            if (string.IsNullOrWhiteSpace(runInstance.LibraryFile.Additional?.ShortName) == false)
                nodeParameters.Variables["ShortName"] = runInstance.LibraryFile.Additional.ShortName;
            nodeParameters.Variables["FileDropUserOutputDir"] 
                = Path.Combine(runInstance.Properties.Config.ManualLibraryPath, "file-drop-users", uid.ToString(), "processed", runInstance.LibraryFile.Uid.ToString());
            nodeParameters.Variables["fdOutput"] 
                = Path.Combine(runInstance.Properties.Config.ManualLibraryPath, "file-drop-users", uid.ToString(), "processed", runInstance.LibraryFile.Uid.ToString());
        }

        if(runInstance.LibraryFile.Additional?.FileDropFlowUid != null && runInstance.LibraryFile.Additional?.FileDropFlowUid != Guid.Empty)
            nodeParameters.Variables["FileDropFlowUid"] = runInstance.LibraryFile.Additional.FileDropFlowUid;

        if (runInstance.Properties.Config.Resources?.Any() == true)
        {
            var resourcesDir = Path.Combine(WorkingDir, "resources");
            if (Directory.Exists(resourcesDir) == false)
                Directory.CreateDirectory(resourcesDir);
            foreach (var res in runInstance.Properties.Config.Resources)
            {
                try
                {
                    var file = Path.Combine(resourcesDir, res.Name + MimeTypeHelper.GetFileExtension(res.MimeType));
                    File.WriteAllBytes(file, res.Data);
                    nodeParameters.Variables[$"resource.{res.Name}"] = file;
                }
                catch (Exception ex)
                {
                    nodeParameters.Logger?.ELog($"Failed saving resource '{res.Name}': {ex.Message}");
                }
            }
        }
        if(Globals.IsDocker)
            nodeParameters.Variables["common"] = DirectoryHelper.DockerModsCommonDirectory;

        if (runInstance.LibraryFile.CustomVariables?.Any() == true)
        {
            foreach (var kv in runInstance.LibraryFile.CustomVariables)
            {
                var kvValue = kv.Value;
                if (kvValue is JsonElement je)
                    kvValue = ObjectHelper.JsonElementToObject(je);
                
                nodeParameters.Logger.ILog($"Adding Custom Variable: {kv.Key} = {kvValue}");
                nodeParameters.Variables[kv.Key] = kvValue;
            }
        }

        // set the method to replace variables
        // this way any path can have variables and will just automatically get replaced
        FileService.Instance.ReplaceVariables = nodeParameters.ReplaceVariables;
        FileService.Instance.Logger = logger;
        
        nodeParameters.HasPluginActual = (name) =>
        {
            var normalizedSearchName = Regex.Replace(name.ToLower(), "[^a-z]", string.Empty);
            return runInstance.Properties.Config.PluginNames?.Any(x =>
                Regex.Replace(x.ToLower(), "[^a-z]", string.Empty) == normalizedSearchName) == true;
        };
        nodeParameters.UploadFile = (string source, string destination) =>
        {
            var task = new FileUploader(logger, RemoteService.ServiceBaseUrl, runInstance.Properties.Uid, RemoteService.AccessToken, RemoteService.NodeUid)
                .UploadFile(source, destination);
            task.Wait();
            return task.Result;
        };
        nodeParameters.DeleteRemote = (path, ifEmpty, includePatterns) =>
        {
            var task = new FileUploader(logger, RemoteService.ServiceBaseUrl, runInstance.Properties.Uid, RemoteService.AccessToken, RemoteService.NodeUid)
                .DeleteRemote(path, ifEmpty, includePatterns);
            task.Wait();
            return task.Result.Success;
        };
        nodeParameters.SendEmail = (to, subject, body) => 
        {
            var result = runInstance.RpcClient.Basic.SendEmail(to, subject, body).Result;
            return string.IsNullOrEmpty(result) ? true : Result<bool>.Fail(result);
        };


        var renderer = new ScribanRenderer();
        nodeParameters.RenderTemplate = (template) =>
            renderer.Render(nodeParameters, template);
        
        nodeParameters.IsDocker = Globals.IsDocker;
        nodeParameters.IsWindows = Globals.IsWindows;
        nodeParameters.IsLinux = Globals.IsLinux;
        nodeParameters.IsMac = Globals.IsMac;
        nodeParameters.IsArm = Globals.IsArm;
        nodeParameters.PathMapper = (path) => Node.Map(path);
        nodeParameters.PathUnMapper = (path) => Node.UnMap(path);
        nodeParameters.LibraryIgnorePath = (path) =>
        {
            runInstance.Properties.RpcClient.LibraryFileHandler.LibraryIgnorePath(path).Wait();
        };
        nodeParameters.MimeTypeUpdater = (mimeType) =>
        {
            runInstance.LibraryFile.Additional ??= new();
            runInstance.LibraryFile.Additional.MimeType = mimeType;
        };
        nodeParameters.ScriptExecutor = new ScriptExecutor()
        {
            SharedDirectory = Path.Combine(runInstance.Properties.ConfigDirectory, "Scripts", "Shared"),
            FileFlowsUrl = RemoteService.ServiceBaseUrl,
            PluginMethodInvoker = (plugin, method, methodArgs) 
                => Helpers.PluginHelper.PluginMethodInvoker(runInstance, nodeParameters, plugin, method, methodArgs)
        };
        foreach (var variable in runInstance.Properties.Config.Variables)
        {
            object value = variable.Value;
            if (value == null)
                continue;
            if (variable.Value?.Trim()?.ToLowerInvariant() == "true")
                value = true;
            else if (variable.Value?.Trim()?.ToLowerInvariant() == "false")
                value = false;
            else if (Regex.IsMatch(variable.Value?.Trim(), @"^[\d](\.[\d]+)?$"))
                value = variable.Value.IndexOf(".", StringComparison.Ordinal) > 0 ? float.Parse(variable.Value) : int.Parse(variable.Value);
            
            nodeParameters.Variables.TryAdd(variable.Key, value);
        }
        
        Plugin.Helpers.FileHelper.DontChangeOwner = Node.DontChangeOwner;
        Plugin.Helpers.FileHelper.DontSetPermissions = Node.DontSetPermissions;
        Plugin.Helpers.FileHelper.Permissions = Node.PermissionsFolders ?? Globals.DefaultPermissionsFile;
        if (Plugin.Helpers.FileHelper.Permissions is < 0 or > 777)
            Plugin.Helpers.FileHelper.Permissions = Globals.DefaultPermissionsFile;
        Plugin.Helpers.FileHelper.PermissionsFolders = Node.PermissionsFolders ?? Globals.DefaultPermissionsFolder;
        if (Plugin.Helpers.FileHelper.PermissionsFolders is < 0 or > 777)
            Plugin.Helpers.FileHelper.PermissionsFolders = Globals.DefaultPermissionsFolder;

        nodeParameters.RunnerUid = runInstance.Properties.Uid;
        nodeParameters.TempPath = WorkingDir;
        nodeParameters.Variables["temp"] = WorkingDir;
        nodeParameters.TempPathName = new DirectoryInfo(WorkingDir).Name;
        nodeParameters.RelativeFile = runInstance.LibraryFile.RelativePath;
        nodeParameters.PartPercentageUpdate = (percent) =>
            runInstance.Properties.RpcClient.RunnerInfo.UpdatePartPercentage(percent).Wait();
        HttpHelper.Logger = nodeParameters.Logger;

        nodeParameters.Result = NodeResult.Success;
        nodeParameters.GetToolPathActual = (name) =>
        {
            string? variable = null;
            var varOverride = nodeParameters.Variables
                .LastOrDefault(x => x.Key.ToLowerInvariant() == name.ToLowerInvariant());
            if (varOverride.Value != null && varOverride.Value is string strVarOverride)
            {
                nodeParameters.Logger?.ILog($"ToolPathVariable '{name}' = '{strVarOverride}'");
                variable = strVarOverride;
            }

            if (string.IsNullOrEmpty(variable))
            {
                variable = runInstance.Properties.Config.Variables.Where(x => string.Equals(x.Key, name, StringComparison.InvariantCultureIgnoreCase))
                    .Select(x => x.Value).FirstOrDefault();
                if (string.IsNullOrWhiteSpace(variable))
                    return variable;
            }

            var final = Node.Map(variable);
            nodeParameters?.Logger?.ILog($"Tool '{name}' variable = '{final}");
            return final;
        };
        
        // must be done after GetToolPathActual so we can get the tools
        nodeParameters.ArchiveHelper = new ArchiveHelper(nodeParameters);
        nodeParameters.ImageHelper = new ImageHelper(logger, nodeParameters);
        nodeParameters.PdfHelper = new PdfHelper(nodeParameters.StringHelper);
        nodeParameters.SetTagsFunction = (tagUids, replace) =>
        {
            var allowed = tagUids.Where(runInstance.Properties.Config.Tags.ContainsKey).ToList();
            if(replace)
                runInstance.LibraryFile.Tags = allowed?.ToList() ?? [];
            else // must be distinct
                runInstance.LibraryFile.Tags = runInstance.LibraryFile.Tags.Union(allowed).Distinct().ToList();
            return allowed.Count;
        };
        nodeParameters.SetTagsByNameFunction = (tagNames, replace) =>
        {
            // lookup tagnames in the runInstance.Properties.Config.Tags
            var tagUids = tagNames.Select(x => runInstance.Properties.Config.Tags
                .FirstOrDefault(y => string.Equals(y.Value, x, StringComparison.InvariantCultureIgnoreCase)).Key)
                .ToList();
            
            if(replace)
                runInstance.LibraryFile.Tags = tagUids?.ToList() ?? [];
            else // must be distinct
                runInstance.LibraryFile.Tags = runInstance.LibraryFile.Tags.Union(tagUids).Distinct().ToList();
            return tagUids.Count;
        };
        
        nodeParameters.GetPluginSettingsJson = (pluginSettingsType) =>
        {
            string? json = null;
            runInstance.Properties.Config.PluginSettings?.TryGetValue(pluginSettingsType, out json);
            return json;
        };
        
        nodeParameters.StatisticRecorderRunningTotals = (name, value) =>
            _ = runInstance.RpcClient.Statistics.RecordRunningTotal(name, value);
        nodeParameters.StatisticRecorderAverage = (name, value) =>
            _ = runInstance.RpcClient.Statistics.RecordAverage(name, value);
        nodeParameters.AdditionalInfoRecorder = (name, value, steps, expiry) =>
            runInstance.Properties.RpcClient.RunnerInfo.RecordAdditionalInfo(name, value, steps, expiry).Wait();

        var flow = new FlowHelper(runInstance).GetStartupFlow(runInstance.Properties.IsRemote, Flow, runInstance.LibraryFile.Name);

        var flowExecutor = new ExecuteFlow()
        {
            Flow = flow,
            Runner = this
        };

        int result = flowExecutor.Execute(nodeParameters);
        logger.ILog("flowExecutor result: " + result);
        runInstance.LibraryFile.Additional ??= new();
        runInstance.LibraryFile.Additional.Version = Globals.Version;
        
        if(Canceled)
            return FileStatus.ProcessingFailed;
        
        if (result == RunnerCodes.Completed)
        {
            logger.ILog("flowExecutor result was completed");
            if (nodeParameters.Reprocess is { HoldForMinutes: > 0 })
            {
                logger.ILog($"Setting Hold For Minutes = {nodeParameters.Reprocess.HoldForMinutes}");
                runInstance.LibraryFile.HoldUntil = DateTime.UtcNow.AddMinutes(nodeParameters.Reprocess.HoldForMinutes.Value);
            }

            if (nodeParameters.Reprocess.ReprocessNode != null)
            {
                logger.ILog($"Setting ProcessOnNodeUid = '{nodeParameters.Reprocess.ReprocessNode.Uid}'");
                runInstance.LibraryFile.ProcessOnNodeUid = nodeParameters.Reprocess.ReprocessNode.Uid;
                return FileStatus.ReprocessByFlow;
            }
            if (nodeParameters.Reprocess is { HoldForMinutes: > 0 })
                return FileStatus.Unprocessed;
            
            logger.ILog("flowExecutor processed successfully");
            return FileStatus.Processed;
        }
        if(result is RunnerCodes.Failure or RunnerCodes.TerminalExit)
            return FileStatus.ProcessingFailed;
        if(result == RunnerCodes.RunCanceled)
            return FileStatus.ProcessingFailed;
        
        nodeParameters.Logger.WLog("Safety caught flow execution unexpected result code: " + result);
        return FileStatus.ProcessingFailed; // safety catch, shouldn't happen
    }

    /// <summary>
    /// Increases the total parts that are in the flow
    /// </summary>
    /// <param name="additional">the additional parts to increase</param>
    public void IncreaseTotalParts(int additional)
        => runInstance.Properties.RpcClient.RunnerInfo.IncreaseTotalParts(additional);
}
