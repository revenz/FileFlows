using System.Diagnostics;
using System.Runtime.InteropServices;
using FileFlows.Server.Helpers;
using FileFlows.Services;
using FileFlows.ServerShared.Workers;
using FileFlows.Shared.Helpers;
using FileFlows.Shared.Models;
using FlowService = FileFlows.Services.FlowService;
using LibraryFileService = FileFlows.Services.LibraryFileService;
using LibraryService = FileFlows.Services.LibraryService;
using NodeService = FileFlows.Services.NodeService;

namespace FileFlows.Server.Workers;

public class TelemetryReporter : ServerWorker
{
    public TelemetryReporter() : base(ScheduleType.Daily, 5, quiet: true)
    {
        Trigger();
    }

    private string GetHostOs()
    {
        if (Application.Docker == false)
        {
            return OperatingSystem.IsMacOS() ? "MacOS" :
                OperatingSystem.IsLinux() ? "Linux" :
                OperatingSystem.IsFreeBSD() ? "FreeBSD" :
                OperatingSystem.IsWindows() ? "Windows" :
                RuntimeInformation.OSDescription;
        }

        // unRAID adds this
        var hostOs = Environment.GetEnvironmentVariable("HOST_OS");
        if(string.IsNullOrWhiteSpace(hostOs) == false)
            return "Docker: " + hostOs.Trim();

        var dockerHostOs = GetDockerHostOs();
        if (string.IsNullOrWhiteSpace(dockerHostOs) == false)
            return "Docker:" + dockerHostOs.Trim();

        return "Docker";

    }
    /// <summary>
    /// Gets the host operating system by running the Docker command.
    /// </summary>
    /// <returns>
    /// A string representing the host operating system, or an empty string if the OS cannot be determined.
    /// </returns>
    public static string GetDockerHostOs()
    {
        try
        {
            // Create a new process to run the Docker CLI command
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "docker",
                ArgumentList = { "info", "--format", "'{{json .OperatingSystem}}'" },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Process.Start(processStartInfo);

            process.Start();

            // Read the output from the Docker command
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (output.Contains("daemon"))
                return string.Empty; // likely `Cannot connect to the Docker daemon at unix:///var/run/docker.sock. Is the docker daemon running?`

            // Check the exit code to ensure the command succeeded
            if (process.ExitCode == 0)
            {
                // Trim and clean the output, removing any extra quotes
                return output.Replace("'", "").Replace("\"", "").Trim();
            }
        }
        catch (Exception)
        {
            // Ignored
        }

        return string.Empty;
    }

    /// <inheritdoc />
    protected override void ExecuteActual(Settings settings)
    {
        try
        {
// #if (DEBUG && false)
//             return;
// #else
            if (settings.DisableTelemetry == true && LicenseService.IsLicensed())
                return; // they have turned it off, dont report anything

            TelemetryData data = new TelemetryData();
            data.ClientUid = settings.Uid;
            data.Version = Globals.Version;
            data.Language = settings.Language?.EmptyAsNull() ?? "en";
            data.DatabaseProvider = ServiceLoader.Load<AppSettingsService>().Settings.DatabaseType.ToString();
            var pNodes = ServiceLoader.Load<NodeService>().GetAllAsync().Result.Where(x => x.Enabled).ToList();
            data.ProcessingNodes = pNodes.Count;
            var hardwareInfo = ServiceLoader.Load<HardwareInfoService>().GetHardwareInfo();
            data.HardwareInfo = hardwareInfo;
            data.ProcessingNodeData = pNodes.Select(x => new ProcessingNodeData()
            {
                OS = x.OperatingSystem,
                Architecture = x.Architecture,
                Runners = x.FlowRunners,
                Internal = x.Uid == CommonVariables.InternalNodeUid || x.Name == CommonVariables.InternalNodeName,
                HardwareInfo = x.Uid == CommonVariables.InternalNodeUid ? hardwareInfo : x.HardwareInfo
            }).ToList();
            data.Architecture = RuntimeInformation.ProcessArchitecture.ToString();
            data.OS = GetHostOs();

            var lfService = ServiceLoader.Load<LibraryFileService>();
            var libFileStatus = lfService.GetStatus().Result;

            int filesFailed = libFileStatus
                .Where(x => x.Status == FileStatus.ProcessingFailed)
                .Select(x =>x.Count)
                .FirstOrDefault();
            int filesProcessed = libFileStatus
                .Where(x => x.Status == FileStatus.Processed)
                .Select(x =>x.Count)
                .FirstOrDefault();
            
            data.FilesFailed = filesFailed;
            data.FilesProcessed = filesProcessed;
            var repo = ServiceLoader.Load<RepositoryService>().GetRepository().Result ?? new ();
            var repoScripts = repo.FlowScripts.Union(repo.SharedScripts).Union(repo.SystemScripts)
                .Where(x => x.Uid != null)
                .DistinctBy(x => x.Uid)
                .ToDictionary(x => x.Uid!.Value, x => x);
            
            var flows = ServiceLoader.Load<FlowService>().GetAllAsync().Result;
            var dictNodes = new Dictionary<string, int>();
            foreach (var fp in flows?.SelectMany(x => x.Parts)?.ToArray() ?? new FlowPart[] { })
            {
                if (fp == null)
                    continue;
                var flowElementUid = fp.FlowElementUid;
                if (flowElementUid.StartsWith("SubFlow:"))
                    continue;
                if (flowElementUid.StartsWith("Script:"))
                {
                    string uid = flowElementUid[7..];
                    if (Guid.TryParse(uid, out var guid) == false || repoScripts.TryGetValue(guid, out var rs) == false)
                        continue;
                    // we use a variable here so we dont update the actual cached flow object
                    flowElementUid = "Script:" + rs.Name; // so we get the name of the script in telemetry
                }
                if (!dictNodes.TryAdd(flowElementUid, 1))
                    dictNodes[flowElementUid] += 1;
            }

            data.Nodes = dictNodes.Select(x => new TelemetryDataSet
            {
                Name = x.Key,
                Count = x.Value
            }).ToList();

            var libraries = ServiceLoader.Load<LibraryService>().GetAllAsync().Result;
            dictNodes.Clear();
            foreach (var lib in libraries?.Where(x => string.IsNullOrEmpty(x.Template) == false) ?? new List<Library>())
            {
                if (!dictNodes.TryAdd(lib.Template, 1))
                    dictNodes[lib.Template] += 1;
            }

            data.StorageSaved = lfService.GetTotalStorageSaved().Result;

            data.LibraryTemplates = dictNodes.Select(x => new TelemetryDataSet
            {
                Name = x.Key,
                Count = x.Value
            }).ToList();


            dictNodes.Clear();
            foreach (var lib in flows?.Where(x => string.IsNullOrEmpty(x.Template) == false) ?? new List<Flow>())
            {
                if (!dictNodes.TryAdd(lib.Template, 1))
                    dictNodes[lib.Template] += 1;
            }

            data.FlowTemplates = dictNodes.Select(x => new TelemetryDataSet
            {
                Name = x.Key,
                Count = x.Value
            }).ToList();

            string url = Globals.FileFlowsDotComUrl + "/api/telemetry";
            _ = HttpHelper.Post(url, data).Result;

//#endif
        }
        catch (Exception)
        {
            // FF-410: silent fail, may not have an internet connection
        }
    }


    /// <summary>
    /// Represents telemetry data collected from a client.
    /// </summary>
    public class TelemetryData
    {
        /// <summary>
        /// Gets or sets the unique identifier of the client.
        /// </summary>
        public Guid ClientUid { get; set; }

        /// <summary>
        /// Gets or sets the version of the telemetry data.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the operating system of the client.
        /// </summary>
        public string OS { get; set; }

        /// <summary>
        /// Gets or sets the architecture of the client's operating system.
        /// </summary>
        public string Architecture { get; set; }
        
        /// <summary>
        /// Gets or sets the language being used by the system
        /// </summary>
        public string Language { get; set; }

        /// <summary>
        /// Gets or sets the number of processing nodes in the client.
        /// </summary>
        public int ProcessingNodes { get; set; }
        
        /// <summary>
        /// Gets or sets the hardware information of the server
        /// </summary>
        public HardwareInfo HardwareInfo { get; set; }

        /// <summary>
        /// Gets or sets the data of individual processing nodes in the client.
        /// </summary>
        public List<ProcessingNodeData> ProcessingNodeData { get; set; }

        /// <summary>
        /// Gets or sets the telemetry data sets collected from various nodes in the client.
        /// </summary>
        public List<TelemetryDataSet> Nodes { get; set; }

        /// <summary>
        /// Gets or sets the telemetry data sets for library templates in the client.
        /// </summary>
        public List<TelemetryDataSet> LibraryTemplates { get; set; }

        /// <summary>
        /// Gets or sets the telemetry data sets for flow templates in the client.
        /// </summary>
        public List<TelemetryDataSet> FlowTemplates { get; set; }

        /// <summary>
        /// Gets or sets the number of files processed by the client.
        /// </summary>
        public int FilesProcessed { get; set; }

        /// <summary>
        /// Gets or sets the number of files that failed during processing by the client.
        /// </summary>
        public int FilesFailed { get; set; }
        
        /// <summary>
        /// Gets or sets the amount of storage saved
        /// </summary>
        public long StorageSaved { get; set; }
        
        /// <summary>
        /// Gets or sets the db provider they are using
        /// </summary>
        public string DatabaseProvider { get; set; }
    }

    /// <summary>
    /// Represents a telemetry data set with a name and a count value.
    /// </summary>
    public class TelemetryDataSet
    {
        /// <summary>
        /// Gets or sets the name of the telemetry data set.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the count value of the telemetry data set.
        /// </summary>
        public int Count { get; set; }
    }

    /// <summary>
    /// Represents the data related to a processing node, including its operating system and architecture.
    /// </summary>
    public class ProcessingNodeData
    {
        /// <summary>
        /// Gets or sets the operating system of the processing node.
        /// </summary>
        public OperatingSystemType OS { get; set; }

        /// <summary>
        /// Gets or sets the architecture of the processing node's operating system.
        /// </summary>
        public ArchitectureType Architecture { get; set; }
        
        /// <summary>
        /// Gets or sets the number of runners on this node
        /// </summary>
        public int Runners { get; set; }
    
        /// <summary>
        /// Gets or sets if this is the internal processing node
        /// </summary>
        public bool Internal { get; set; }
        
        /// <summary>
        /// Gets or sets the hardware information of the processing node.
        /// </summary>
        public HardwareInfo HardwareInfo { get; set; }
    }
}
