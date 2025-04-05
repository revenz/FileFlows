using System.ComponentModel.DataAnnotations;
using FileFlows.Plugin;

namespace FileFlows.Shared.Models;

/// <summary>
/// A processing node used by FileFlows to process Library Files
/// </summary>
public class ProcessingNode: FileFlowObject
{
    /// <summary>
    /// Gets or sets the temporary path used by this node
    /// </summary>
    public string TempPath { get; set; }

    /// <summary>
    /// Gets or sets the address this node is located at, hostname or ip address
    /// </summary>
    public string Address { get; set; }
    
    /// <summary>
    /// Gets or sets the Icon of the Node
    /// </summary>
    public string Icon { get; set; }

    /// <summary>
    /// Gets or sets when the node was last seen
    /// </summary>
    public DateTime LastSeen { get; set; }

    /// <summary>
    /// Gets or sets if this node is enabled
    /// </summary>
    public bool Enabled { get; set; }
    
    /// <summary>
    /// Gets or sets the number of seconds to check for a new file to process
    /// </summary>
    public int ProcessFileCheckInterval { get; set; }
    
    /// <summary>
    /// Gets or sets the priority of the processing node
    /// Higher the value, the higher the priority 
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Gets or sets the type of operating system this node is running on
    /// </summary>
    public OperatingSystemType OperatingSystem { get; set; }
    /// <summary>
    /// Gets or sets the architecture type
    /// </summary>
    public ArchitectureType Architecture { get; set; }
    /// <summary>
    /// Gets or sets the FileFlows version of this node
    /// </summary>
    public string Version { get; set; }
    
    /// <summary>
    /// Gets or sets a script to execute before a runner can start
    /// </summary>
    public Guid? PreExecuteScript { get; set; }

    /// <summary>
    /// Gets or sets the number of flow runners this node can run simultaneously 
    /// </summary>
    public int FlowRunners { get; set; }
    
    /// <summary>
    /// Gets or sets the processing order this node will use
    /// </summary>
    public ProcessingOrder? ProcessingOrder { get; set; }

    /// <summary>
    /// Gets or sets the SignalrUrl this node uses
    /// </summary>
    public string SignalrUrl { get; set; }
    /// <summary>
    /// Gets or sets the mappings for this node
    /// </summary>
    public List<KeyValuePair<string, string>> Mappings { get; set; }
    /// <summary>
    /// Gets or sets the variables for this node
    /// </summary>
    public List<KeyValuePair<string, string>> Variables { get; set; }
    /// <summary>
    /// Gets or sets the schedule for this node
    /// </summary>
    public string Schedule { get; set; }
    /// <summary>
    /// Gets or sets if the owner should not be changed
    /// </summary>
    public bool DontChangeOwner { get; set; }
    /// <summary>
    /// Gets or sets if permissions should not be set
    /// </summary>
    public bool DontSetPermissions { get; set; }
    /// <summary>
    /// Gets or sets the permissions to set
    /// </summary>
    public string Permissions { get; set; }
    
    /// <summary>
    /// Gets or sets the hardware information
    /// </summary>
    public HardwareInfo? HardwareInfo { get; set; }

    /// <summary>
    /// Gets or sets the permissions to set
    /// </summary>
    public int? PermissionsFiles
    {
        // this will change in a few versions and Permissions property will be removed
        get => int.TryParse(Permissions, out int value) ? value : null;
        set => Permissions = value?.ToString("D3");
    }
    /// <summary>
    /// Gets or sets the permissions to set for folders
    /// </summary>
    public int? PermissionsFolders { get; set; }
    /// <summary>
    /// Gets or sets if this node can process all libraries
    /// </summary>
    public ProcessingLibraries AllLibraries { get; set; }
    /// <summary>
    /// Gets or sets the libraries this node can process
    /// </summary>
    public List<ObjectReference> Libraries { get; set; }
    /// <summary>
    /// Gets or sets the maximum file size this node can process
    /// </summary>
    [Range(0, 10_000_000)]
    public int MaxFileSizeMb { get; set; }

    /// <summary>
    /// Gets or sets how many files have been processed
    /// </summary>
    [DbIgnore]
    public int ProcessedFiles { get; set; }
    
    /// <summary>
    /// The directory separator used by this node
    /// </summary>
    public char DirectorySeparatorChar = System.IO.Path.DirectorySeparatorChar;

    /// <summary>
    /// Maps a path locally for this node
    /// </summary>
    /// <param name="path">The path to map</param>
    /// <returns>The path mapped locally for this node</returns>
    public string Map(string path)
    {
        if (string.IsNullOrEmpty(path))
            return string.Empty;
        if (Mappings != null && Mappings.Count > 0)
        {
            // convert all \ to / for now
            path = path.Replace("\\", "/");
            foreach (var mapping in Mappings)
            {
                if (string.IsNullOrEmpty(mapping.Value) || string.IsNullOrEmpty(mapping.Key))
                    continue;
                string pattern = Regex.Escape(mapping.Key.Replace("\\", "/"));
                string replacement = mapping.Value.Replace("\\", "/");
                path = Regex.Replace(path, "^" + pattern, replacement, RegexOptions.IgnoreCase);
            }
            // now convert / to path character
            if (DirectorySeparatorChar != '/')
                path = path.Replace('/', DirectorySeparatorChar);
            if(path.StartsWith("//")) // special case for SMB paths
                path = path.Replace('/', '\\');
        }
        return path;
    }
    
    /// <summary>
    /// Unmaps a path for this node
    /// </summary>
    /// <param name="path">The mapped path</param>
    /// <returns>The unmapped path</returns>
    public string UnMap(string path)
    {
        if (string.IsNullOrEmpty(path))
            return string.Empty;
        if (Mappings != null && Mappings.Count > 0)
        {
            foreach (var mapping in Mappings)
            {
                if (string.IsNullOrEmpty(mapping.Value) || string.IsNullOrEmpty(mapping.Key))
                    continue;
                path = Regex.Replace(path, "^" + Regex.Escape(mapping.Value), mapping.Key, RegexOptions.IgnoreCase);
                path = Regex.Replace(path, "^" + Regex.Escape(mapping.Value.Replace("\\", "/")), mapping.Key, RegexOptions.IgnoreCase);
                path = Regex.Replace(path, "^" + Regex.Escape(mapping.Value.Replace("/", "\\")), mapping.Key, RegexOptions.IgnoreCase);
            }
        }

        int forwardIndex = path.IndexOf("/");
        int backIndex = path.IndexOf("\\");
        if (forwardIndex >= 0 && backIndex >= 0)
        {
            // we have both slashes, need to use the first one
            (string correct, string incorrect) = backIndex < forwardIndex ? ("\\", "/") : ("/", "\\");
            path = path.Replace(incorrect, correct);
        }

        return path;
    }

    /// <summary>
    /// Gets a variable, or empty if not found
    /// </summary>
    /// <param name="key">the key to the variable</param>
    /// <returns>the variable value</returns>
    public string GetVariable(string key)
        => Variables?.FirstOrDefault(x => x.Key == key).Value ?? string.Empty;

    /// <summary>
    /// Gets or sets the status of the processing node
    /// </summary>
    [DbIgnore]
    public ProcessingNodeStatus Status { get; set; }
}

/// <summary>
/// Status of a processing node
/// </summary>
public enum ProcessingNodeStatus
{
    /// <summary>
    /// The node is offline or cannot be reached
    /// </summary>
    Offline = 0,
    /// <summary>
    /// The node is available but not processing
    /// </summary>
    Idle = 1,
    /// <summary>
    /// The node is currently processing files
    /// </summary>
    Processing = 2,
    /// <summary>
    /// The node is disabled
    /// </summary>
    Disabled = 3,
    /// <summary>
    /// The node is out of schedule
    /// </summary>
    OutOfSchedule = 4,
    /// <summary>
    /// The node's version does not match the servers
    /// </summary>
    VersionMismatch = 5,
    /// <summary>
    /// If a higher priority node is available to process a file
    /// </summary>
    HigherPriorityNodeAvailable = 6,
    /// <summary>
    /// Installing DockerMods
    /// </summary>
    InstallingDockerMods = 7,
    /// <summary>
    /// If the maximum runners have been reached
    /// </summary>
    MaximumRunnersReached = 8,
    /// <summary>
    /// Restricted for processing by library
    /// </summary>
    RestrictedByLibrary = 9
}