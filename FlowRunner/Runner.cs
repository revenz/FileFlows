using System.Reflection;
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
using FileFlows.ServerShared.Interfaces;
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
    internal FlowExecutorInfo Info { get; private set; }
    private Flow Flow;
    private ProcessingNode Node;
    internal readonly CancellationTokenSource CancellationToken = new CancellationTokenSource();
    internal bool Canceled { get; private set; }
    private string WorkingDir;

    internal readonly List<Flow> ExecutedFlows = new();

    /// <summary>
    /// The number of flow elements that currently have been executed
    /// </summary>
    private int ExecutedSteps = 0;

    /// <summary>
    /// The number of flow elements that have been executed and that count towards the total allowed
    /// We dont count steps like startup, enter sub flow, sub flow output etc
    /// </summary>
    private int ExecutedStepsCountedTowardsTotal = 0;
    
    //private string ScriptDir, ScriptSharedDir, ScriptFlowDir;

    /// <summary>
    /// Creates an instance of a Runner
    /// </summary>
    /// <param name="runInstance">the run intance running this</param>
    /// <param name="info">The execution info that is being run</param>
    /// <param name="flow">The flow that is being executed</param>
    /// <param name="node">The processing node that is executing this flow</param>
    /// <param name="workingDir">the temporary working directory to use</param>
    public Runner(RunInstance runInstance, FlowExecutorInfo info, Flow flow, ProcessingNode node, string workingDir)
    {
        this.runInstance = runInstance;
        this.Info = info;
        this.Flow = flow;
        this.Node = node;
        this.WorkingDir = workingDir;
    }

    /// <summary>
    /// A delegate for the flow complete event
    /// </summary>
    public delegate void FlowCompleted(Runner sender, bool success);
    /// <summary>
    /// An event that is called when the flow completes
    /// </summary>
    public event FlowCompleted OnFlowCompleted;
    private NodeParameters nodeParameters;

    /// <summary>
    /// Gets or sets the current executing flow element
    /// </summary>
    internal Node CurrentFlowElement { get; set; }

    /// <summary>
    /// Records the execution of a flow node
    /// </summary>
    /// <param name="nodeName">the name of the flow node</param>
    /// <param name="nodeUid">the UID of the flow node</param>
    /// <param name="output">the output after executing the flow node</param>
    /// <param name="duration">how long it took to execution</param>
    /// <param name="part">the flow node part</param>
    /// <param name="flowDepth">the depth of the executed flow</param>
    internal void RecordNodeExecution(string nodeName, string nodeUid, int output, TimeSpan duration, FlowPart part, int flowDepth)
    {
        if (Info.LibraryFile == null)
            return;

        Info.LibraryFile.ExecutedNodes ??= new List<ExecutedNode>();
        Info.LibraryFile.ExecutedNodes.Add(new ExecutedNode
        {
            NodeName = nodeName,
            NodeUid = part.Type == FlowElementType.Script ? "ScriptNode" : nodeUid,
            FlowPartUid = part.Uid,
            
            Output = output,
            ProcessingTime = duration,
            Depth = flowDepth,
        });

        _ = SendUpdate(Info);
    }


    /// <summary>
    /// Starts the flow runner processing
    /// </summary>
    public (bool Success, bool KeepFiles) Run(FlowLogger logger)
    {
        var systemHelper = new SystemHelper(runInstance);
        bool success = false;
        try
        {
            try
            {
                systemHelper.Start();
                var service = ServiceLoader.Load<IFlowRunnerService>();
                var updated = service.Start(Info).Result;
                if (updated == null)
                    return (false, false); // failed to update
                var communicator = FlowRunnerCommunicator.Load(runInstance, Info.LibraryFile.Uid);
                var connected = communicator.InitializeAsync().Result;
                if (connected == false)
                {
                    logger.ELog("Failed to connect to Server via SignalR");
                    return (false, false);
                }
                communicator.OnCancel += Communicator_OnCancel;
                logger.SetCommunicator(communicator);
                bool finished = false;
                DateTime lastSuccessHello = DateTime.UtcNow;
                var task = Task.Run(async () =>
                {
                    while (finished == false)
                    {
                        if (finished == false)
                        {
                            bool success = await communicator.Hello(runInstance.Uid, this.Info, nodeParameters);
                            if (success == false)
                            {
                                if (lastSuccessHello < DateTime.UtcNow.AddMinutes(-2))
                                {
                                    nodeParameters?.Logger?.ELog("Hello failed, cancelling flow");
                                    Communicator_OnCancel();
                                    return;
                                }

                                nodeParameters?.Logger?.WLog("Hello failed, if continues the flow will be canceled");
                            }
                            else
                            {
                                lastSuccessHello = DateTime.UtcNow;
                            }
                        }

                        await Task.Delay(5_000);
                    }
                });
                try
                {
                    RunActual(logger, communicator);
                }
                catch (Exception ex)
                {
                    finished = true;
                    task.Wait();

                    nodeParameters?.Logger?.ELog("Error in runner: " + ex.Message + Environment.NewLine +
                                                 ex.StackTrace);

                    if (string.IsNullOrWhiteSpace(nodeParameters.FailureReason))
                        nodeParameters.FailureReason = "Error in runner: " + ex.Message;
                    if (Info.LibraryFile?.Status == FileStatus.Processing)
                        SetStatus(FileStatus.ProcessingFailed);
                }
                finally
                {
                    finished = true;
                    task.Wait();
                    communicator.OnCancel -= Communicator_OnCancel;
                    communicator.Close();
                }
            }
            catch (Exception ex)
            {
                runInstance.Logger.ELog("Failure in runner: " + ex.Message + Environment.NewLine + ex.StackTrace);
            }

            try
            {
                Finish().Wait();
                success = true;
            }
            catch (Exception ex)
            {
                runInstance.Logger.ELog("Failed 'Finishing' runner: " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }
        finally
        {
            systemHelper.Stop();
        }

        return (success, Info.LibraryFile.Status == FileStatus.ProcessingFailed);
    }

    /// <summary>
    /// Called when the communicator receives a cancel request
    /// </summary>
    private void Communicator_OnCancel()
    {
        nodeParameters?.Logger?.ILog("##### CANCELING FLOW!");
        CancellationToken.Cancel();
        nodeParameters?.Cancel();
        Canceled = true;
        if (CurrentFlowElement != null)
            CurrentFlowElement.Cancel().Wait();
        Info.LibraryFile.FailureReason = "Flow was canceled";
    }

    /// <summary>
    /// Finish executing of a file
    /// </summary>
    public async Task Finish()
    {
        string? log = null;
        if (nodeParameters?.Logger is FlowLogger fl)
        {
            log = fl.ToString();
            await fl.Flush();
        }

        if(nodeParameters?.OriginalMetadata != null)
            Info.LibraryFile.OriginalMetadata = nodeParameters.OriginalMetadata;
        if (nodeParameters?.Metadata != null)
            Info.LibraryFile.FinalMetadata = nodeParameters.Metadata;
        // calculates the final fingerprint
        // if (string.IsNullOrWhiteSpace(Info.LibraryFile.OutputPath) == false)
        // {
        //     Info.LibraryFile.FinalFingerprint = Plugin.Helpers.FileHelper.CalculateFingerprint(Info.LibraryFile.OutputPath);
        // }

        await Complete(log);
        OnFlowCompleted?.Invoke(this, Info.LibraryFile.Status == FileStatus.Processed);
    }

    /// <summary>
    /// Calculates the final size of the file
    /// </summary>
    private void CalculateFinalSize()
    {
        if (nodeParameters.IsDirectory)
            Info.LibraryFile.FinalSize = nodeParameters.GetDirectorySize(nodeParameters.WorkingFile);
        else
        {
            Info.LibraryFile.FinalSize = nodeParameters.LastValidWorkingFileSize;

            // try
            // {
            //     if (Info.Fingerprint)
            //     {
            //         Info.LibraryFile.Fingerprint = ServerShared.Helpers.FileHelper.CalculateFingerprint(nodeParameters.WorkingFile) ?? string.Empty;
            //         nodeParameters?.Logger?.ILog("Final Fingerprint: " + Info.LibraryFile.Fingerprint);
            //     }
            //     else
            //     {
            //         Info.LibraryFile.Fingerprint = string.Empty;
            //     }
            // }
            // catch (Exception ex)
            // {
            //     nodeParameters?.Logger?.ILog("Error with fingerprinting: " + ex.Message + Environment.NewLine + ex.StackTrace);
            // }
        }
        nodeParameters?.Logger?.ILog("Original Size: " + Info.LibraryFile.OriginalSize);
        nodeParameters?.Logger?.ILog("Final Size: " + Info.LibraryFile.FinalSize);
        Info.LibraryFile.OutputPath = Node.UnMap(nodeParameters.WorkingFile);
        nodeParameters?.Logger?.ILog("Output Path: " + Info.LibraryFile.OutputPath);
        nodeParameters?.Logger?.ILog("Final Status: " + Info.LibraryFile.Status);
        if (string.IsNullOrWhiteSpace(nodeParameters.FailureReason) == false)
        {
            nodeParameters?.Logger?.ILog("Final Failure Reason: " + nodeParameters.FailureReason);
            Info.LibraryFile.FailureReason = nodeParameters.FailureReason;
        }
    }

    /// <summary>
    /// Called when the flow execution completes
    /// </summary>
    private async Task Complete(string log)
    {
        DateTime start = DateTime.UtcNow;
        Info.LibraryFile.ProcessingEnded = DateTime.UtcNow;
        if(nodeParameters != null) // this is null if it fails to remotely download the file
            CalculateFinalSize();
        do
        {
            try
            {

                var service = ServiceLoader.Load<IFlowRunnerService>();
                await service.Finish(Info);
                return;
            }
            catch (Exception)
            {
                
            }
            await Task.Delay(30_000);
        } while (DateTime.UtcNow.Subtract(start) < new TimeSpan(0, 10, 0));
        runInstance.Logger?.ELog("Failed to inform server of flow completion");
    }

    /// <summary>
    /// Called when the current flow step changes, ie it moves to a different node to execute
    /// </summary>
    /// <param name="partName">the step part name</param>
    /// <param name="dontCountTowardsTotal">if this step should not count towards the total number of steps allowed</param>
    internal Result<bool> StepChanged(string partName, bool dontCountTowardsTotal = false)
    {
        ++ExecutedSteps;
        if (dontCountTowardsTotal == false)
            ++ExecutedStepsCountedTowardsTotal;
        
        if (ExecutedStepsCountedTowardsTotal > runInstance.Config.MaxNodes)
             return Result<bool>.Fail("Exceeded maximum number of flow elements to process");

        // remove old additional info
        var aiKeys = Info.AdditionalInfos.Keys.ToArray();
        foreach (var kv in aiKeys)
        {
            if (--Info.AdditionalInfos[kv].Steps < 1)
                Info.AdditionalInfos.Remove(kv);
        }
        
        Info.CurrentPartName = partName;
        Info.CurrentPart = ExecutedSteps;
        Info.CurrentPartPercent = 0;
        try
        {
            _ = SendUpdate(Info, waitMilliseconds: 1000);
        }
        catch (Exception) 
        { 
            // silently fail, not a big deal, just incremental progress update
            runInstance.Logger.WLog("Failed to record step change: " + ExecutedSteps + " : " + partName);
        }

        return true;
    }

    /// <summary>
    /// Updates the currently steps completed percentage
    /// </summary>
    /// <param name="percentage">the percentage</param>
    private void UpdatePartPercentage(float percentage)
    {
        Info.CurrentPartPercent = percentage;
        try
        {
            _ = SendUpdate(Info);
        }
        catch (Exception)
        {
            // silently fail, not a big deal, just incremental progress update
        }
    }

    /// <summary>
    /// When an update was last sent to the server to say this is still alive
    /// </summary>
    private DateTime LastUpdate;
    /// <summary>
    /// A semaphore to ensure only one update is set at a time
    /// </summary>
    private SemaphoreSlim UpdateSemaphore = new SemaphoreSlim(1);
    
    /// <summary>
    /// Sends an update to the server
    /// </summary>
    /// <param name="info">the information to send to the server</param>
    /// <param name="waitMilliseconds">how long to wait to send, if takes longer than this, it wont be sent</param>
    private async Task SendUpdate(FlowExecutorInfo info, int waitMilliseconds = 100)
    {
        if (await UpdateSemaphore.WaitAsync(waitMilliseconds) == false)
        {
            // Program.Logger.DLog("Failed to wait for SendUpdate semaphore");
            return;
        }

        try
        {
            if(waitMilliseconds != 1000) // 1000 is the delay for finishing / step changes
                await Task.Delay(500);
            LastUpdate = DateTime.UtcNow;
            var service = ServiceLoader.Load<IFlowRunnerService>();;
            if(nodeParameters?.OriginalMetadata != null)
                Info.LibraryFile.OriginalMetadata = nodeParameters.OriginalMetadata;
            await service.Update(info);
        }
        catch (Exception)
        {
            // Ignored
        }
        finally
        {
            UpdateSemaphore.Release();
        }
    }

    /// <summary>
    /// Sets the status of file
    /// </summary>
    /// <param name="status">the status</param>
    private void SetStatus(FileStatus status)
    {
        DateTime start = DateTime.UtcNow;
        Info.LibraryFile.Status = status;
        if (status is FileStatus.Processed or FileStatus.ReprocessByFlow)
        {
            Info.LibraryFile.ProcessingEnded = DateTime.UtcNow;
        }
        else if(status == FileStatus.ProcessingFailed)
        {
            Info.LibraryFile.ProcessingEnded = DateTime.UtcNow;
            if (string.IsNullOrWhiteSpace(nodeParameters.FailureReason) == false)
                Info.LibraryFile.FailureReason = nodeParameters.FailureReason;
        }
        do
        {
            try
            {
                CalculateFinalSize();
                SendUpdate(Info, waitMilliseconds: 1000).Wait();
                runInstance.Logger?.DLog("Set final status to: " + status);
                return;
            }
            catch (Exception ex)
            {
                // this is more of a problem, its not ideal, so we do try again
                runInstance.Logger?.WLog("Failed to set status on server: " + ex.Message);
            }
            Thread.Sleep(5_000);
        } while (DateTime.UtcNow.Subtract(start) < new TimeSpan(0, 3, 0));
    }

    /// <summary>
    /// Starts processing a file
    /// </summary>
    /// <param name="logger">the logger used to log messages</param>
    /// <param name="communicator">The flow runner communicator</param>
    private void RunActual(FlowLogger logger, FlowRunnerCommunicator communicator)
    {
        VariablesHelper.StartedAt = DateTime.Now;
        
        var cacheService = ServiceLoader.Load<IDistributedCacheService>();
        var cacheHelper = new CacheHelper(logger, (key) =>
        {
            try
            {
                var json = cacheService.GetJsonAsync(key).Result;
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
                cacheService.StoreJsonAsync(key, json, expiration).Wait();
            }
            catch (Exception)
            {
                // Ignore
            }
        });
        
        nodeParameters = new NodeParameters(Info.LibraryFile.Name, logger,
            Info.IsDirectory, Info.LibraryPath, fileService: FileService.Instance)
        {
            Node = new () { Uid = Node.Uid, Name = Node.Name, Type = Node.Address },
            Library = Info.LibraryFile.Library ?? new () { Uid = Info.LibraryFile.LibraryUid!.Value, 
                Name = Info.LibraryFile.Name, 
                Type = typeof(Library).FullName
            },
            LicenseLevel = (LicenseLevel)(int) runInstance.Config.LicenseLevel,
            LibraryFileName = Info.LibraryFile.Name,
            IsRemote = Info.IsRemote,
            LogImageActual = logger.Image,
            NotificationCallback = (severity, title, message) =>
            {
                ServiceLoader.Load<INotificationService>().Record((NotificationSeverity)severity, title, message);
            },
            Cache = cacheHelper,
            SetThumbnailActual = (binaryData) =>
            {
                var service = ServiceLoader.Load<IFlowRunnerService>();
                service.SetThumbnail(Info.LibraryFile.Uid, binaryData).Wait();
            }
        };
        
        nodeParameters.Variables["library.Name"] = Info.Library.Name;
        nodeParameters.Variables["library.Path"] = Info.LibraryPath;

        if (Info.LibraryFile.Additional?.ResellerUserUid != null &&
            Info.LibraryFile.Additional?.ResellerUserUid != Guid.Empty)
        {
            var uid = Info.LibraryFile.Additional.ResellerUserUid;
            nodeParameters.Variables["ResellerUserUid"] = uid.ToString();
            nodeParameters.Variables["rUserUid"] = uid.ToString();
            nodeParameters.Variables["ResellerUserOutputDir"] 
                = Path.Combine(runInstance.Config.ManualLibraryPath, "reseller-users", uid.ToString(), "processed", Info.LibraryFile.Uid.ToString());
            nodeParameters.Variables["ruOutput"] 
                = Path.Combine(runInstance.Config.ManualLibraryPath, "reseller-users", uid.ToString(), "processed", Info.LibraryFile.Uid.ToString());
        }

        if(Info.LibraryFile.Additional?.ResellerFlowUid != null && Info.LibraryFile.Additional?.ResellerFlowUid != Guid.Empty)
            nodeParameters.Variables["ResellerFlowUid"] = Info.LibraryFile.Additional.ResellerFlowUid;

        if (runInstance.Config.Resources?.Any() == true)
        {
            var resourcesDir = Path.Combine(WorkingDir, "resources");
            if (Directory.Exists(resourcesDir) == false)
                Directory.CreateDirectory(resourcesDir);
            foreach (var res in runInstance.Config.Resources)
            {
                try
                {
                    var file = Path.Combine(resourcesDir, res.Name + GetFileExtension(res.MimeType));
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

        if (Info.LibraryFile.CustomVariables?.Any() == true)
        {
            foreach (var kv in Info.LibraryFile.CustomVariables)
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
            return runInstance.Config.PluginNames?.Any(x =>
                Regex.Replace(x.ToLower(), "[^a-z]", string.Empty) == normalizedSearchName) == true;
        };
        nodeParameters.UploadFile = (string source, string destination) =>
        {
            var task = new FileUploader(logger, RemoteService.ServiceBaseUrl, runInstance.Uid, RemoteService.AccessToken, RemoteService.NodeUid)
                .UploadFile(source, destination);
            task.Wait();
            return task.Result;
        };
        nodeParameters.DeleteRemote = (path, ifEmpty, includePatterns) =>
        {
            var task = new FileUploader(logger, RemoteService.ServiceBaseUrl, runInstance.Uid, RemoteService.AccessToken, RemoteService.NodeUid)
                .DeleteRemote(path, ifEmpty, includePatterns);
            task.Wait();
            return task.Result.Success;
        };
        nodeParameters.SendEmail = (to, subject, body) => ServiceLoader.Load<EmailService>().Send(to, subject, body).Result;


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
            communicator.LibraryIgnorePath(path).Wait();
        };
        nodeParameters.MimeTypeUpdater = (mimeType) =>
        {
            Info.LibraryFile.Additional ??= new();
            Info.LibraryFile.Additional.MimeType = mimeType;
        };
        nodeParameters.ScriptExecutor = new ScriptExecutor()
        {
            SharedDirectory = Path.Combine(runInstance.ConfigDirectory, "Scripts", "Shared"),
            FileFlowsUrl = RemoteService.ServiceBaseUrl,
            PluginMethodInvoker = (plugin, method, methodArgs) 
                => Helpers.PluginHelper.PluginMethodInvoker(runInstance, nodeParameters, plugin, method, methodArgs)
        };
        foreach (var variable in runInstance.Config.Variables)
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

        nodeParameters.RunnerUid = Info.Uid;
        nodeParameters.TempPath = WorkingDir;
        nodeParameters.Variables["temp"] = WorkingDir;
        nodeParameters.TempPathName = new DirectoryInfo(WorkingDir).Name;
        nodeParameters.RelativeFile = Info.LibraryFile.RelativePath;
        nodeParameters.PartPercentageUpdate = UpdatePartPercentage;
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
                variable = runInstance.Config.Variables.Where(x => string.Equals(x.Key, name, StringComparison.InvariantCultureIgnoreCase))
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
            var allowed = tagUids.Where(runInstance.Config.Tags.ContainsKey).ToList();
            if(replace)
                Info.LibraryFile.Tags = allowed?.ToList() ?? [];
            else // must be distinct
                Info.LibraryFile.Tags = Info.LibraryFile.Tags.Union(allowed).Distinct().ToList();
            return allowed.Count;
        };
        nodeParameters.SetTagsByNameFunction = (tagNames, replace) =>
        {
            // lookup tagnames in the runInstance.Config.Tags
            var tagUids = tagNames.Select(x => runInstance.Config.Tags
                .FirstOrDefault(y => string.Equals(y.Value, x, StringComparison.InvariantCultureIgnoreCase)).Key)
                .ToList();
            
            if(replace)
                Info.LibraryFile.Tags = tagUids?.ToList() ?? [];
            else // must be distinct
                Info.LibraryFile.Tags = Info.LibraryFile.Tags.Union(tagUids).Distinct().ToList();
            return tagUids.Count;
        };
        
        nodeParameters.GetPluginSettingsJson = (pluginSettingsType) =>
        {
            string? json = null;
            runInstance.Config.PluginSettings?.TryGetValue(pluginSettingsType, out json);
            return json;
        };
        var statService = ServiceLoader.Load<IStatisticService>();;
        nodeParameters.StatisticRecorderRunningTotals = (name, value) =>
            _ = statService.RecordRunningTotal(name, value);
        nodeParameters.StatisticRecorderAverage = (name, value) =>
            _ = statService.RecordAverage(name, value);
        nodeParameters.AdditionalInfoRecorder = RecordAdditionalInfo;

        var flow = new FlowHelper(runInstance).GetStartupFlow(Info.IsRemote, Flow, Info.WorkingFile);

        var flowExecutor = new ExecuteFlow()
        {
            Flow = flow,
            Runner = this
        };

        int result = flowExecutor.Execute(nodeParameters);
        Info.LibraryFile.Additional ??= new();
        Info.LibraryFile.Additional.Version = Globals.Version;
        
        if(Canceled)
            SetStatus(FileStatus.ProcessingFailed);
        else if (result == RunnerCodes.Completed)
        {
            if (nodeParameters.Reprocess is { HoldForMinutes: > 0 })
            {
                logger.ILog($"Setting Hold For Minutes = {nodeParameters.Reprocess.HoldForMinutes}");
                Info.LibraryFile.HoldUntil = DateTime.UtcNow.AddMinutes(nodeParameters.Reprocess.HoldForMinutes.Value);
            }

            if (nodeParameters.Reprocess.ReprocessNode != null)
            {
                logger.ILog($"Setting ProcessOnNodeUid = '{nodeParameters.Reprocess.ReprocessNode.Uid}'");
                Info.LibraryFile.ProcessOnNodeUid = nodeParameters.Reprocess.ReprocessNode.Uid;
                SetStatus(FileStatus.ReprocessByFlow);
            }
            else if (nodeParameters.Reprocess is { HoldForMinutes: > 0 })
            {
                SetStatus(FileStatus.Unprocessed);
            }
            else
                SetStatus(FileStatus.Processed);
        }
        else if(result is RunnerCodes.Failure or RunnerCodes.TerminalExit)
            SetStatus(FileStatus.ProcessingFailed);
        else if(result == RunnerCodes.RunCanceled)
            SetStatus(FileStatus.ProcessingFailed);
        else if(result == RunnerCodes.MappingIssue)
            SetStatus(FileStatus.MappingIssue);
        else
        {
            nodeParameters.Logger.WLog("Safety caught flow execution unexpected result code: " + result);
            SetStatus(FileStatus.ProcessingFailed); // safety catch, shouldn't happen
        }
    }
    
    private void RecordAdditionalInfo(string name, object value, int steps, TimeSpan? expiry)
    {
        if (value == null)
        {
            if (Info.AdditionalInfos.ContainsKey(name) == false)
                return; // nothing to do

            Info.AdditionalInfos.Remove(name);
        }
        else
        {
            if (value is TimeSpan ts)
                value = Plugin.Helpers.TimeHelper.ToHumanReadableString(ts);
            
            Info.AdditionalInfos[name] = new()
            {
                Value = value,
                Expiry = expiry ?? new TimeSpan(0, 1, 0),
                Steps = steps
            };
        }
        _ = SendUpdate(Info);
    }
    
    /// <summary>
    /// Gets the file extension based on the provided MIME type.
    /// If the MIME type is valid, the method attempts to use the subtype as the extension.
    /// If the subtype is not a valid extension, it falls back on known mappings for common MIME types.
    /// </summary>
    /// <param name="mimeType">The MIME type to analyze.</param>
    /// <returns>A string representing the file extension, including the leading period (e.g., ".jpg"). 
    /// Returns ".bin" for unknown or invalid MIME types.</returns>
    private string GetFileExtension(string mimeType)
    {
        // If mimeType is null or empty, return a default extension
        if (string.IsNullOrWhiteSpace(mimeType))
            return ".bin"; // Default for unknown types

        // Split the MIME type to get the type and subtype
        var parts = mimeType.Split('/');
        if (parts.Length != 2)
            return ".bin"; // Return default if it's not a valid MIME type format

        // Get the subtype and check if it can be used as a valid extension
        string subtype = parts[1].ToLower();

        // Return the subtype as the extension if it's valid (alphanumeric or hyphen)
        if (!string.IsNullOrWhiteSpace(subtype) && 
            subtype.All(c => char.IsLetterOrDigit(c) || c == '-'))
        {
            return "." + subtype; // Return the valid subtype with a leading dot
        }

        // Fallback to specific known mappings for common types
        return subtype switch
        {
            "jpeg" => ".jpg",
            "jpg" => ".jpg",
            "png" => ".png",
            "gif" => ".gif",
            "bmp" => ".bmp",
            "svg+xml" => ".svg",
            "plain" => ".txt",
            "html" => ".html",
            "csv" => ".csv",
            "pdf" => ".pdf",
            "zip" => ".zip",
            "mp3" => ".mp3",
            "wav" => ".wav",
            "ogg" => ".ogg",
            "mp4" => ".mp4",
            "mov" => ".mov",
            "avi" => ".avi",
            // Add more common types as needed
            _ => ".bin" // Default for unrecognized types
        };
    }

}
