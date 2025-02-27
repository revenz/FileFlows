using FileFlows.Node.Workers;
using FileFlows.Server.Workers;
using FileFlows.ServerShared.Models;
using FileFlows.ServerShared.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using FileFlows.Node.Ui;
using FileFlows.RemoteServices;
using FileFlows.ServerShared;
using FileFlows.ServerShared.Helpers;
using FileFlows.ServerShared.Workers;
using FileFlows.Shared.Models;

namespace FileFlows.Node;

/// <summary>
/// A manager that handles registering a node with the FileFlows server
/// </summary>
public class NodeManager
{
    /// <summary>
    /// Gets or sets if this node is registered
    /// </summary>
    public bool Registered { get; private set; }

    /// <summary>
    /// Starts the node processing
    /// </summary>
    public void Start()
    {
        StartWorkers();
    }

    /// <summary>
    /// Stops the node processing
    /// </summary>
    public void Stop()
    {
        WorkerManager.StopWorkers();
    }

    /// <summary>
    /// Starts the node workers
    /// </summary>
    private void StartWorkers()
    {
        Shared.Logger.Instance?.ILog("Starting workers");
        var updater = new NodeUpdater();
        
        if (updater.RunCheck())
            return;

        var nodeVersion = Globals.Version;
        var nodeVersionVersion = new Version(nodeVersion);

        // var flowWorker = new FlowWorker(AppSettings.Instance.HostName)
        // {
        //     IsEnabledCheck = () =>
        //     {
        //         if (this.Registered == false)
        //         {
        //             Logger.Instance?.ILog($"Node not registered, Flow Worker skip running.");
        //             return false;
        //         }
        //
        //         if (AppSettings.IsConfigured() == false)
        //         {
        //             Logger.Instance?.ILog($"Node not configured, Flow Worker skip running.");
        //             return false;
        //         }
        //
        //         var nodeService = ServiceLoader.Load<INodeService>();
        //         try
        //         {
        //             var settings = nodeService.GetByAddressAsync(AppSettings.Instance.HostName).Result;
        //             if (settings == null)
        //             {
        //                 Logger.Instance?.ELog("Failed getting settings for node: " + AppSettings.Instance.HostName);
        //                 return false;
        //             }
        //
        //             AppSettings.Instance.Save();
        //
        //             var serverVersion = ServiceLoader.Load<ISettingsService>().GetServerVersion().Result;
        //             if (serverVersion != nodeVersionVersion)
        //             {
        //                 Logger.Instance?.ILog($"Node version '{nodeVersion}' does not match server version '{serverVersion}'");
        //                 NodeUpdater.CheckForUpdate();
        //                 return false;
        //             }
        //
        //             //return AppSettings.Instance.Enabled;
        //             return settings.Enabled;
        //         }
        //         catch (Exception ex)
        //         {
        //             if (ex.Message?.Contains("502 Bad Gateway") == true)
        //                 Logger.Instance?.ELog("Failed checking enabled: Unable to reach FileFlows Server.");
        //             else
        //                 Logger.Instance?.ELog("Failed checking enabled: " + ex.Message + Environment.NewLine +
        //                                       ex.StackTrace);
        //         }
        //
        //         return false;
        //     }
        // };
        
        WorkerManager.StartWorkers(
            //flowWorker, 
            updater, 
            //new RestApiWorker(), // is this used?
            new LogFileCleaner(),
            new TempFileCleaner(AppSettings.Instance.HostName), 
            //new SystemStatisticsWorker(),
            new ConfigCleaner()
        );

        // new NodeClient.Client(AppSettings.Instance.ServerUrl, AppSettings.Instance.HostName);
    }

    
    /// <summary>
    /// Registers the node with the server
    /// </summary>
    /// <returns>whether it was registered</returns>
    public async Task<(bool Success, string Message)> Register()
    {
        string path = DirectoryHelper.BaseDirectory;

        bool windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        List<RegisterModelMapping> mappings = new List<RegisterModelMapping>
            {
                new()
                {
                    Server = "ffmpeg",
                    Local =  Globals.IsDocker ? "/usr/local/bin/ffmpeg" :
                             windows ? Path.Combine(path, "Tools", "ffmpeg.exe") : "/usr/local/bin/ffmpeg"
                }
            };
        if (AppSettings.EnvironmentalMappings?.Any() == true)
        {
            Logger.Instance.ILog("Environmental mappings found, adding those");
            mappings.AddRange(AppSettings.EnvironmentalMappings);
        }

        string tempPath =  AppSettings.ForcedTempPath?.EmptyAsNull() ?? (Globals.IsDocker ? "/temp" : Path.Combine(DirectoryHelper.BaseDirectory, "Temp"));

        var settings = AppSettings.Instance;
        if (string.IsNullOrEmpty(settings.ServerUrl))
            return (false, "Server URL not set");
        
        RemoteService.AccessToken = settings.AccessToken;
        HardwareInfo? hardwareInfo = null;
        try
        {
            hardwareInfo = ServiceLoader.Load<HardwareInfoService>().GetHardwareInfo();
            Logger.Instance?.ILog("Hardware Info: " + Environment.NewLine + hardwareInfo);
        }
        catch(Exception ex)
        {
            Logger.Instance?.ELog("Failed to get hardware info: " + ex.Message);
        }

        
        var nodeService = ServiceLoader.Load<INodeService>();
        Shared.Models.ProcessingNode? result;
        try
        {
            result = await nodeService.Register(settings.ServerUrl, settings.HostName, tempPath, mappings, hardwareInfo);
            if (result == null)
            {
                this.Registered = false;
                return (false, "Failed to register");
            }
        }
        catch (TaskCanceledException ex)
        {
            Logger.Instance?.ELog("Failed to register with server: " + ex.Message);
            this.Registered = false;
            return (false, "Connection timed out. Check network and address.");
        }
        catch (Exception ex)
        {
            Logger.Instance?.ELog("Failed to register with server: " + ex.Message);
            this.Registered = false;
            if(ex.Message.StartsWith("A task was canceled"))
                return (false, "Connection timed out. Check network and address.");
            return (false, ex.Message);
        }

        if(result.Uid != CommonVariables.InternalNodeUid) // internal node uid is already set elsewhere to a unique UID for security
            RemoteService.NodeUid = result.Uid;
        RemoteService.ServiceBaseUrl = settings.ServerUrl;
        if (RemoteService.ServiceBaseUrl.EndsWith('/'))
            RemoteService.ServiceBaseUrl = RemoteService.ServiceBaseUrl[..^1];

        Logger.Instance?.ILog("Successfully registered node");


        settings.Save();
        this.Registered = true;
        return (true, string.Empty);
    }
}
