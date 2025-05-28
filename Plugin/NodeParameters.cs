using System.Reflection;
using System.Text.RegularExpressions;
using FileFlows.Plugin.Models;
using System.Runtime.InteropServices;
using System.Text.Json;
using FileFlows.Plugin.Helpers;
using FileFlows.Plugin.Services;

namespace FileFlows.Plugin;

public class NodeParameters
{
    /// <summary>
    /// The original filename of the file
    /// Note: This maybe a mapped filename if executed on a external processing node
    /// </summary>
    public string FileName { get; init; }
    
    /// <summary>
    /// Gets or sets the full filename as it appears in the library
    /// </summary>
    public string LibraryFileName { get; init; }

    /// <summary>
    /// Gets or sets the file relative to the library path
    /// </summary>
    public string RelativeFile { get; set; }
    
    /// <summary>
    /// Gets or seta a cancellation token to listen for
    /// </summary>
    public CancellationToken CancellationToken { get; init; }

    /// <summary>
    /// The current working file as it is being processed in the flow, 
    /// this is what a node should save any changes too, and if the node needs 
    /// to change the file path this should be updated too
    /// </summary>
    public string WorkingFile { get; private set; }

    /// <summary>
    /// Gets or sets reprocessing options
    /// </summary>
    public ReprocessOptions Reprocess { get; set; } = new();

    public ObjectReference? ReprocessNode
    {
        get => Reprocess.ReprocessNode;
        set => Reprocess.ReprocessNode = value;
    }

    /// <summary>
    /// Gets the working file shortname
    /// </summary>
    public string WorkingFileName => string.IsNullOrWhiteSpace(WorkingFile) ? string.Empty : 
        IsDirectory ? new DirectoryInfo(this.WorkingFile).Name : new FileInfo(this.WorkingFile).Name;

    private long _WorkingFileSize { get; set; }
    /// <summary>
    /// Gets the last actual record file size that is greater than zero
    /// </summary>
    public long LastValidWorkingFileSize { get; private set; }

    /// <summary>
    /// Gets or sets the file size of the current working file
    /// </summary>
    public long WorkingFileSize
    {
        get => _WorkingFileSize;
        private set
        {
            if (value > 1)
                LastValidWorkingFileSize = value;
            _WorkingFileSize = value;
        }
    }

    /// <summary>
    /// Gets or sets the logger used by the flow during execution
    /// </summary>
    public ILogger? Logger { get; set; }
    
    /// <summary>
    /// Gets or set s the script executor
    /// </summary>
    public IScriptExecutor ScriptExecutor { get; set; }

    /// <summary>
    /// Gets or sets the result of the flow
    /// </summary>
    public NodeResult Result { get; set; } = NodeResult.Success;
    
    /// <summary>
    /// Gets or sets why the current executing flow element failed
    /// This is cleared whenever a new flow element starts execution 
    /// </summary>
    public string FailureReason { get; set; }
    
    /// <summary>
    /// Gets the processing node this is running on
    /// </summary>
    public ObjectReference Node { get; init; }
    
    /// <summary>
    /// Gets or sets the library this file is from
    /// </summary>
    public ObjectReference Library { get; init; }

    /// <summary>
    /// Gets or sets the parameters used in the flow execution
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    
    /// <summary>
    /// Gets or sets the variables used that are passed between executed nodes
    /// </summary>
    public Dictionary<string, object> Variables { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// Gets or sets the function responsible for getting the actual tool path
    /// </summary>
    public Func<string, string>? GetToolPathActual { get; set; }
    
    /// <summary>
    /// Gets or sets the function responsible for getting if a plugin is available
    /// </summary>
    public Func<string, bool>? HasPluginActual { get; set; }
    
    /// <summary>
    /// Gets or sets the actoin responsible for logging an image
    /// </summary>
    public Action<string>? LogImageActual { get; set; }
    
    /// <summary>
    /// Gets or sets the function responsible for rendering a template
    /// </summary>
    public Func<string, string>? RenderTemplate { get; set; }
    
    /// <summary>
    /// Gets or sets a method used for setting the files thumbnail
    /// </summary>
    public Action<byte[]>? SetThumbnailActual { get; set; }

    /// <summary>
    /// Gets or sets the method to set the display name of the file which is shown in the UI/webconsole.
    /// </summary>
    public Action<string> SetDisplayNameActual { get; set; }
    
    /// <summary>
    /// Gets or sets the method to get properties
    /// </summary>
    public Func<string, string> GetPropertyActual { get; set; }
    
    /// <summary>
    /// Gets or sets the method to set properties
    /// </summary>
    public Action<string, string> SetPropertyActual { get; set; }

    /// <summary>
    /// Gets or sets the method to set the file traits
    /// </summary>
    public Action<List<string>> SetTraitsActual { get; set; }

    /// <summary>
    /// Gets or sets the action that records running totals statistics
    /// </summary>
    public Action<string, string>? StatisticRecorderRunningTotals { get; set; }
    
    /// <summary>
    /// Gets or sets the action that records average statistics
    /// </summary>
    public Action<string, int>? StatisticRecorderAverage { get; set; }
    
    /// <summary>
    /// Gets or sets the action that records additional info
    /// </summary>
    public Action<string, object, int, TimeSpan?>? AdditionalInfoRecorder { get; set; }
    
    /// <summary>
    /// Gets or sets the notification callback
    /// </summary>
    public ScriptExecutionArgs.NotificationDelegate NotificationCallback { get; set; }

    /// <summary>
    /// Gets or sets the function responsible for getting plugin settings JSON configuration
    /// </summary>
    public Func<string, string> GetPluginSettingsJson { get; set; }

    /// <summary>
    /// Gets or sets the function responsible for settings the tags
    /// </summary>
    public Func<Guid[], bool, int> SetTagsFunction { get; set; }

    /// <summary>
    /// Gets or sets the function responsible for settings the tags by their names
    /// </summary>
    public Func<string[], bool, int> SetTagsByNameFunction { get; set; }
    
    /// <summary>
    /// Gets or sets the function responsible for telling the library to ignore a specific path
    /// </summary>
    public Action<string>? LibraryIgnorePath { get; set; }
    
    /// <summary>
    /// Gets or sets the function responsible for mapping a path
    /// </summary>
    public Func<string, string>? PathMapper { get; set; }
    
    /// <summary>
    /// Gets or sets the function responsible for unmapping a path
    /// </summary>
    public Func<string, string>? PathUnMapper { get; set; }
    
    /// <summary>
    /// Gets or sets the action responsible for setting the mime type
    /// </summary>
    public Action<string>? MimeTypeUpdater { get; set; }
    
    /// <summary>
    /// Gets or sets the function responsible for uploading a file
    /// </summary>
    public Func<string, string, (bool Success, string Error)>? UploadFile { get; set; }
    
    /// <summary>
    /// Gets or sets the function responsible for deleting a remote directory
    /// </summary>
    public Func<string, bool, string[], bool>? DeleteRemote { get; set; }
    
    /// <summary>
    /// Gets or sets the function responsible for deleting a remote directory
    /// </summary>
    public Func<string[], string, string, Result<bool>>? SendEmail { get; set; }
    
    /// <summary>
    /// Gets the cache helper
    /// </summary>
    public CacheHelper Cache { get; init; }

    /// <summary>
    /// Gets or sets a goto flow 
    /// </summary>
    public Action<ObjectReference> GotoFlow { get; set; }

    /// <summary>
    /// Gets or sets if a directory is being processed instead of a file
    /// </summary>
    public bool IsDirectory { get; set; }
    
    /// <summary>
    /// Gets or sets if the original file that started this is a directory instead of a file
    /// </summary>
    public bool OriginalIsDirectory { get; }
    
    /// <summary>
    /// Gets or sets the path to the library this library file belongs to
    /// </summary>
    public string LibraryPath { get; set; }

    /// <summary>
    /// Gets or sets if this node is running inside a docker container
    /// </summary>
    public bool IsDocker { get; set; }

    /// <summary>
    /// Gets or sets if this node is running on windows
    /// </summary>
    public bool IsWindows { get; set; }

    /// <summary>
    /// Gets or sets if this node is running on linux
    /// </summary>
    public bool IsLinux { get; set; }

    /// <summary>
    /// Gets or sets if this node is running on a mac
    /// </summary>
    public bool IsMac { get; set; }

    /// <summary>
    /// Gets or sets if this node is running on a ARM base platform
    /// </summary>
    public bool IsArm { get; set; }
    
    /// <summary>
    /// Gets or sets if the file is a remote file and needs to be downloaded from the server then copied back to it
    /// </summary>
    public bool IsRemote { get; set; }

    /// <summary>
    /// Gets or sets the temporary path for this node
    /// </summary>
    public string TempPath { get; set; }

    /// <summary>
    /// Gets or sets the short temporary path name
    /// eg. Runner-42f99fc9-158e-408d-9133-de91a56a6ac8
    /// </summary>
    public string TempPathName { get; set; }

    /// <summary>
    /// Gets the temp path on the host if running on docker from the environmental variable "TempPathHost"
    /// If not running on docker just returns TempPath
    /// </summary>
    public string TempPathHost
    {
        get
        {
            var host = Environment.GetEnvironmentVariable("TempPathHost");
            if (string.IsNullOrEmpty(host))
                return TempPath;
            // need to append runner subpath
            return Path.Combine(host, "Runner-" + RunnerUid);
        }
    }

    /// <summary>
    /// Gets or sets the runners UID
    /// </summary>
    public Guid RunnerUid { get; set; }

    /// <summary>
    /// Gets or sets the action that handles updating a percentage change for a flow part
    /// </summary>
    public Action<float>? PartPercentageUpdate { get; set; }

    /// <summary>
    /// Gets or sets the process helper
    /// </summary>
    public IProcessHelper Process { get; set; }

    /// <summary>
    /// if this is af faked instance
    /// </summary>
    private bool Fake = false;
    
    /// <summary>
    /// Gets or sets the original metadata for the input file
    /// </summary>
    public Dictionary<string, object> OriginalMetadata { get; private set; }
    
    /// <summary>
    /// Gets or sets the metadata for the file
    /// </summary>
    public Dictionary<string, object> Metadata { get; private set; }
    
    /// <summary>
    /// Gets if the license level of this instance
    /// </summary>
    public LicenseLevel LicenseLevel { get; init; }

    /// <summary>
    /// Sets the metadata for the file
    /// </summary>
    /// <param name="metadata">the metadata for the file</param>
    public void SetMetadata(Dictionary<string, object> metadata)
    {
        if (OriginalMetadata == null)
            OriginalMetadata = metadata;
        else
            Metadata = metadata;
    }


    /// <summary>
    /// Sets the mime/type of the current file
    /// </summary>
    /// <param name="mimeType">the mime type</param>
    public void SetMimeType(string mimeType)
    {
        MimeTypeUpdater?.Invoke(mimeType);
    }
    
    /// <summary>
    /// Gets or sets the file service to use
    /// </summary>
    public IFileService FileService { get; init; }
    
    /// <summary>
    /// Gets the math helper 
    /// </summary>
    public MathHelper MathHelper { get; init; }
    
    /// <summary>
    /// Gets the string helper 
    /// </summary>
    public StringHelper StringHelper { get; init; }

    /// <summary>
    /// Constructs a node parameters instance used by the flow runner
    /// </summary>
    /// <param name="filename">the filename of the original library file</param>
    /// <param name="logger">the logger used during execution</param>
    /// <param name="isDirectory">if this is executing against a directory instead of a file</param>
    /// <param name="libraryPath">the path of the library this file exists in</param>
    /// <param name="fileService">the FileService to user</param>
    /// <param name="cancellationToken">the cancellation token</param>
    public NodeParameters(string? filename, ILogger logger, bool isDirectory, string? libraryPath,
        IFileService fileService, CancellationToken cancellationToken = default)
    {
        Fake = string.IsNullOrEmpty(filename);
        this.CancellationToken = cancellationToken;
        this.IsDirectory = isDirectory;
        this.OriginalIsDirectory = isDirectory;
        this.FileName = filename;
        this.LibraryPath = libraryPath;
        this.WorkingFile = filename;
        this.FileService = fileService;
        this.MathHelper = new(logger);
        this.StringHelper = new(logger);
        if (Fake == false)
        {
            try
            {
                this.WorkingFileSize = IsDirectory ? GetDirectorySize(filename) : new FileInfo(filename).Length;
            }
            catch (Exception)
            {
                // can fail in unit tests
            }
        }

        this.RelativeFile = string.Empty;
        this.TempPath = string.Empty;
        this.Logger = logger;
        //InitFile(filename);
        this.Process = new ProcessHelper(logger, cancellationToken, this.Fake);
    }

    /// <summary>
    /// Constructs a new basic node parameters with no file 
    /// </summary>
    /// <param name="logger">the logger used during execution</param>
    public NodeParameters(ILogger logger)
    {
        this.Logger = logger;
        this.Process = new ProcessHelper(logger, CancellationToken.None, false);
    }


    /// <summary>
    /// Gets the file size of a directory and all its files
    /// </summary>
    /// <param name="path">The path of the directory</param>
    /// <returns>The directories total size</returns>
    public long GetDirectorySize(string path)
    {
        if (Fake) return 100_000_000_000;
        try
        {
            DirectoryInfo dir = new DirectoryInfo(path);
            return dir.EnumerateFiles("*.*", SearchOption.AllDirectories).Sum(x => x.Length);
        }
        catch (Exception)
        {
            return 0;
        }
    }

    /// <summary>
    /// Maps a path to one that exists on the processing node
    /// Note: It is safe to map a path multiple times as this should not effect its value
    /// </summary>
    /// <param name="path">The path to map</param>
    /// <returns>The mapped path</returns>
    public string MapPath(string path)
    {
        if (Fake) return path;
        if (PathMapper == null)
            return path;
        return PathMapper(path);
    }

    /// <summary>
    /// Unmaps a path to the original FileFlows Server path
    /// Note: It is safe to unmap a path multiple times as this should not effect its value
    /// </summary>
    /// <param name="path">The path to unmap</param>
    /// <returns>The unmapped path as it appears on the server</returns>
    public string UnMapPath(string path)
    {
        if (Fake) return path;
        if (PathUnMapper == null)
            return path;
        return PathUnMapper(path);
    }

    /// <summary>
    /// Fails a flow
    /// </summary>
    /// <param name="reason">the reason for failing</param>
    /// <returns>the return code</returns>
    public int Fail(string reason)
    {
        FailureReason = reason;
        if(Logger != null)
            Logger.ELog(reason);
        return -1;
    }
    
    /// <summary>
    /// Checks if a plugin is available
    /// </summary>
    /// <param name="name">The name of the plugin</param>
    /// <returns>true if the plugin is available</returns>
    public bool HasPlugin(string name)
    {
        if (HasPluginActual == null) return false;
        return HasPluginActual(name);
    }

    /// <summary>
    /// Logs an image
    /// </summary>
    /// <param name="path">the path to the image</param>
    public void LogImage(string path)
        => LogImageActual?.Invoke(path);

    private bool _thumbnailSet = false;

    /// <summary>
    /// Gets if a thumbnail has been set
    /// </summary>
    /// <returns>true if has been set, otherwise false</returns>
    public bool HasThumbnailBeenSet()
        => _thumbnailSet;

    /// <summary>
    /// Sets the files thumbnail
    /// </summary>
    /// <param name="file">the thumbnail file</param>
    public void SetThumbnail(string file)
    {
        var actual = ReplaceVariables(file?.EmptyAsNull() ?? WorkingFile, stripMissing: true);

        if (string.IsNullOrWhiteSpace(file))
        {
            Logger?.WLog("No file specified for thumbnail image");
            return;
        }

        if (actual.StartsWith("http:", StringComparison.InvariantCultureIgnoreCase) ||
            actual.StartsWith("https:", StringComparison.InvariantCultureIgnoreCase))
        {
            Logger?.ILog("URL specified for thumbnail image: " + actual);
            var extension = GetExtensionFromUrl(actual);
            var newFile = Path.Combine(TempPath, Guid.NewGuid() + extension);
            var result2 = DownloadHelper.Download(actual, newFile).GetAwaiter().GetResult();
            if (result2.Failed(out var error2))
            {
                Logger.WLog(error2);
                return;
            }

            actual = newFile;
        }
        else
        {
            var local = FileService.GetLocalPath(actual);
            if (local.Failed(out var error2))
                Logger?.ILog("Failed creating thumbnail: " + error2);
            actual = local.Value;
        }

        var copy = Path.Combine(TempPath, Guid.NewGuid() + FileHelper.GetExtension(actual));
        Logger?.ILog($"Copying image for screenshot '{actual}' to '{copy}'");
        File.Copy(actual, copy);

        Logger?.ILog("Attempting to create thumbnail from: " + copy);
        var tempFile = Path.Combine(TempPath, Guid.NewGuid() + ".webp");
        var result = ImageHelper.ConvertToWebp(copy, tempFile, new ()
            {
                MaxWidth = 250,
                MaxHeight = 250,
                Mode = ResizeMode.Contain,
                Quality = 70
            });
        if (result.Failed(out var error))
        {
            Logger?.ILog("Failed creating thumbnail: " + error);
            return;
        }

        if (result.Value == false)
        {
            Logger?.ILog("Failed creating thumbnail");
            return;
        }

        try
        {
            var data = File.ReadAllBytes(tempFile);
            SetThumbnailActual(data);
            _thumbnailSet = true;
        }
        catch(Exception ex)
        {
            Logger?.ILog("Failed creating thumbnail: " + ex.Message);
        }
    }

    /// <summary>
    /// Gets the file extension from a given URL.
    /// </summary>
    /// <param name="url">The URL from which to extract the file extension.</param>
    /// <returns>The file extension as a lowercase, or an empty string if the URL is invalid or no extension is found.</returns>
    private string GetExtensionFromUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return string.Empty;

        try
        {
            var uri = new Uri(url);
            var path = uri.AbsolutePath;
            var extension = Path.GetExtension(path);
            return string.IsNullOrEmpty(extension) ? null : extension.ToLowerInvariant();
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Sets the display name of the file which is shown in the UI/webconsole.
    /// Set to empty or null to clear it
    /// </summary>
    /// <param name="displayName">the display name</param>
    public void SetDisplayName(string displayName)
        => SetDisplayNameActual(displayName);


    /// <summary>
    /// Gets the property value
    /// </summary>
    /// <param name="name">the name of the property</param>
    /// <returns>the value or null if not found</returns>
    public string GetProperty(string name)
        => GetPropertyActual(name);
    
    /// <summary>
    /// Sets a property 
    /// </summary>
    /// <param name="name">the name of the property</param>
    /// <param name="value">the value</param>
    public void SetProperty(string name, string value)
        => SetPropertyActual(name, value);

    /// <summary>
    /// Sets the traits of a file
    /// </summary>
    /// <param name="traits">the traits</param>
    public void SetTraits(string[] traits)
        => SetTraitsActual(traits?.ToList() ?? []);

    /// <summary>
    /// Gets the archive helper
    /// </summary>
    public IImageHelper ImageHelper { get; set; }
    
    /// <summary>
    /// Gets the PDF helper
    /// </summary>
    public IPdfHelper PdfHelper { get; set; }
    
    /// <summary>
    /// Gets the archive  helper
    /// </summary>
    public IArchiveHelper ArchiveHelper { get; set; }

    /// <summary>
    /// Gets the checksum helper
    /// </summary>
    public CheckSumHelper CheckSumHelper { get; } = new ();

    private bool initDone = false;
    
    /// <summary>
    /// Initializes a file ane updates the variables to that file information
    /// </summary>
    /// <param name="filename">the name of the file to initialize</param>
    public void InitFile(string filename)
    {
        if (Fake) return;
        try
        {
            Logger.ILog("Initing file: " + filename);
            long? folderSize = null;
            if (IsDirectory)
            {
                Variables.TryAdd("folder.OriginalName", LibraryFileName);
                var di = new DirectoryInfo(filename);
                UpdateVariables(new Dictionary<string, object>
                {
                    { "folder.Name", di.Name ?? "" },
                    { "folder.FullName", di.FullName ?? "" }
                });
                if (initDone == false)
                {
                    initDone = true;
                    var diOriginal = new DirectoryInfo(this.FileName);
                    UpdateVariables(new Dictionary<string, object>
                    {
                        { "folder.Date", diOriginal.CreationTime },
                        { "folder.Date.Year", diOriginal.CreationTime.Year },
                        { "folder.Date.Month", diOriginal.CreationTime.Month },
                        { "folder.Date.Day", diOriginal.CreationTime.Day },

                        { "folder.Orig.Name", diOriginal.Name ?? "" },
                        { "folder.Orig.FullName", diOriginal.FullName ?? "" },
                    });

                    if (FileService.DirectorySize(diOriginal.FullName).Success(out var origSize))
                    {
                        folderSize = origSize;
                        Variables["folder.Orig.Size"] = origSize;
                    }
                    else
                    {
                        folderSize = 0;
                    }
                }
            }
            else
            {
                Variables.TryAdd("file.OriginalName", LibraryFileName);
                var result = FileService.FileInfo(filename);
                if (result.IsFailed)
                    return;
                var fi = result.Value;
                if (fi == null)
                    return;
                UpdateVariables(new Dictionary<string, object>
                {
                    { "ext", fi.Extension },
                    { "file.Name", fi.Name ?? "" },
                    { "file.NameNoExtension", FileHelper.GetShortFileNameWithoutExtension(fi.FullName) ?? string.Empty },
                    { "file.FullName", fi.FullName ?? "" },
                    { "file.Extension", fi.Extension ?? "" },
                    { "file.Size", fi.Length },

                    { "folder.Name", FileHelper.GetDirectoryName(fi.FullName) ?? "" },
                    { "folder.FullName", fi.Directory ?? "" },
                });

                if (initDone == false)
                {
                    Logger.ILog("init not done");
                    initDone = true;
                    if (FileName.StartsWith("http", StringComparison.InvariantCultureIgnoreCase) == false 
                        && FileService.FileInfo(this.FileName).Success(out var fiOriginal) && fiOriginal != null)
                    {
                        Variables["ORIGINAL_CREATE_UTC"] = fiOriginal.CreationTimeUtc;
                        Variables["ORIGINAL_LAST_WRITE_UTC"] = fiOriginal.LastWriteTimeUtc;

                        UpdateVariables(new Dictionary<string, object>
                        {
                            { "file.Create", fiOriginal.CreationTime },
                            { "file.Create.Year", fiOriginal.CreationTime.Year },
                            { "file.Create.Month", fiOriginal.CreationTime.Month },
                            { "file.Create.Day", fiOriginal.CreationTime.Day },

                            { "file.Modified", fiOriginal.LastWriteTime },
                            { "file.Modified.Year", fiOriginal.LastWriteTime.Year },
                            { "file.Modified.Month", fiOriginal.LastWriteTime.Month },
                            { "file.Modified.Day", fiOriginal.LastWriteTime.Day },

                            { "file.Orig.Extension", fiOriginal.Extension ?? string.Empty },
                            { "file.Orig.FileName", fiOriginal.Name ?? string.Empty },
                            { "file.Orig.Name", fiOriginal.Name ?? string.Empty },
                            {
                                "file.Orig.FileNameNoExtension",
                                FileHelper.GetShortFileNameWithoutExtension(fiOriginal.FullName) ?? string.Empty
                            },
                            {
                                "file.Orig.NameNoExtension",
                                FileHelper.GetShortFileNameWithoutExtension(fiOriginal.FullName) ?? string.Empty
                            },
                            { "file.Orig.FullName", fiOriginal.FullName ?? string.Empty },
                            { "file.Orig.Size", fiOriginal.Length },

                            { "folder.Orig.Name", FileHelper.GetDirectoryName(fiOriginal.FullName) ?? string.Empty },
                            { "folder.Orig.FullName", fiOriginal.Directory ?? "" }
                        });
                        if (FileService.DirectorySize(fiOriginal.Directory).Success(out var origSize))
                        {
                            folderSize = origSize;
                            Variables["folder.Orig.Size"] = origSize;
                        }
                        else
                        {
                            folderSize = 0;
                        }


                        if (string.IsNullOrEmpty(this.LibraryPath) == false &&
                            fiOriginal.FullName.StartsWith(this.LibraryPath))
                        {
                            UpdateVariables(new Dictionary<string, object>
                            {
                                { "file.Orig.RelativeName", fiOriginal.FullName.Substring(LibraryPath.Length + 1) }
                            });
                        }
                    }
                }
            }

            if (folderSize is > 0)
                Variables["folder.Size"] = folderSize.Value;
            else if (folderSize == null && FileService.DirectorySize(filename).Success(out var size))
                Variables["folder.Size"] = size;
        }
        catch (Exception ex)
        {
            Logger?.ELog("Failed to init file: " + ex.Message + Environment.NewLine + ex.StackTrace);
        }

    }

    /// <summary>
    /// Resets the working file back to the original library file
    /// </summary>
    public void ResetWorkingFile()
    {
        if (Fake) return;
        SetWorkingFile(this.FileName);
    }

    /// <summary>
    /// Tests if a file exists 
    /// </summary>
    /// <param name="filename">The filename to test</param>
    /// <returns>true if exists, otherwise false</returns>
    public bool FileExists(string filename)
    {
        try
        {
            return File.Exists(filename);
        }
        catch (Exception) { return false; } 
    }

    /// <summary>
    /// Updates the current working file and initializes it
    /// </summary>
    /// <param name="filename">The new working file</param>
    /// <param name="dontDelete">If the existing working file should not be deleted.</param>
    public void SetWorkingFile(string filename, bool dontDelete = false)
    {
        if (Fake) return;
        // special case eg nc: for next cloud, where the file wont be accessible so we just set it so it appears nicely in the UI but its gone
        bool fakeFile = Regex.IsMatch(filename, @"^[\w\d]{2,}:");
        
        bool isDirectory =fakeFile ? false : Directory.Exists(filename);
        Logger?.ILog("Setting working file to: " + filename);

        if (fakeFile == false && RuntimeInformation.IsOSPlatform(OSPlatform.Windows) == false)
        {
            if (filename?.ToLower().StartsWith(TempPath.ToLower()) == true)
            {
                Logger.ILog("Changing owner on working file: " + filename);
                Helpers.FileHelper.ChangeOwner(Logger, filename, file: isDirectory == false);
            }
            else
            {
                Logger.ILog("NOT changing owner on working file: " + filename + ", temp path: " + TempPath);
            }
        }

        if (isDirectory == false)
        {
            this.WorkingFileSize = FileService.FileSize(filename).ValueOrDefault;
            Logger?.ILog("New working file size: " + this.WorkingFileSize);
        }

        if (this.WorkingFile == filename)
        {
            Logger?.ILog("Working file same as new filename: " + filename);
            return;
        }

        if (dontDelete == false)
        {
            dontDelete = this.WorkingFile.ToLowerInvariant().StartsWith(TempPath.ToLowerInvariant()) == false;
        }

        if (isDirectory == false && this.WorkingFile != this.FileName
            && FileHelper.IsUrl(this.WorkingFile) == false)
        {
            string fileToDelete = this.WorkingFile;
            if (dontDelete == false)
            {
                // delete the old working file
                _ = Task.Run(async () =>
                {
                    await Task.Delay(2_000); // wait 2 seconds for the file to be released if used
                    var result = FileService.FileDelete(fileToDelete);
                    if (result.IsFailed)
                        Logger?.WLog("Failed to delete temporary file: " + result.Error);
                    else
                        Logger.ILog("Deleting old working file: " + fileToDelete);
                });
            }
        }
        this.IsDirectory = IsDirectory;
        this.WorkingFile = filename;
        if(fakeFile == false)
            InitFile(filename);
    }

    
    /// <summary>
    /// Gets a parameter by its name
    /// </summary>
    /// <param name="name">the name of the parameter</param>
    /// <typeparam name="T">The type of parameter it is</typeparam>
    /// <returns>The value if found, otherwise default(T)</returns>
    public T GetParameter<T>(string name)
    {
        if (Parameters.ContainsKey(name) == false)
        {
            if (typeof(T) == typeof(string))
                return (T)(object)string.Empty;
            return default(T)!;
        }
        return (T)Parameters[name];
    }

    /// <summary>
    /// Sets a value in the Parameters list
    /// </summary>
    /// <param name="name">The name/key of the parameter</param>
    /// <param name="value">The value to set</param>
    public void SetParameter(string name, object value)
    {
        if (Parameters.TryAdd(name, value) == false)
            Parameters[name] = value;
    }

    /// <summary>
    /// Sets the tag on the file
    /// </summary>
    /// <param name="tags">the tags to set</param>
    /// <param name="replace">if the tags should be replaced</param>
    public void SetTagsByUid(IEnumerable<Guid> tags, bool replace)
        => SetTagsFunction?.Invoke(tags.ToArray(), replace);

    /// <summary>
    /// Adds a tags by their name
    /// </summary>
    /// <param name="names">the names of the tag</param>
    /// <returns>the number of tags added</returns>
    public int AddTags(params string[] names)
        => SetTagsByNameFunction?.Invoke(names, false) ?? 0;
    /// <summary>
    /// Sets tags by their name
    /// </summary>
    /// <param name="names">the names of the tag</param>
    /// <returns>the number of tags added</returns>
    public int SetTags(params string[] names)
        => SetTagsByNameFunction?.Invoke(names, true) ?? 0;
    
    /// <summary>
    /// Moves the working file
    /// </summary>
    /// <param name="destination">the destination to move the file</param>
    /// <returns>true if successfully moved</returns>
    public Result<bool> MoveFile(string destination)
    {
        if (Fake) return true;
        
        Logger?.ILog("MoveFile: " + WorkingFile);
        Logger?.ILog("Destination: " + destination);

        if (WorkingFile == destination)
        {
            Logger?.ILog("Same file, nothing to move.");
            return true;
        }

        // Ignore it before the move
        LibraryIgnorePath(MapPath(destination));

        var result = FileService.FileMove(WorkingFile, destination, true);
        if (result.Failed(out var error))
            return Result<bool>.Fail(error);
        
        Logger?.ILog("File moved to: " + destination);
        this.WorkingFile = destination;
        try
        {
            // this can fail if the file is then moved really quickly by another process, radarr/sonarr etc
            Logger?.ILog("Initing new moved file");
            InitFile(destination);
        }
        catch (Exception) { }
        return true;
    }

    /// <summary>
    /// Copies a folder to the destination
    /// Paths will automatically be mapped relative to the Node executing it
    /// </summary>
    /// <param name="source">the source file</param>
    /// <param name="destination">the destination file</param>
    /// <param name="updateWorkingFile"></param>
    /// <returns>whether the file was copied successfully</returns>
    public Result<bool> CopyFile(string source, string destination, bool updateWorkingFile = false)
    {
        if (Fake) return true;

        if (string.IsNullOrWhiteSpace(source))
            return Result<bool>.Fail("CopyFile.Source was not supplied");
        
        if (string.IsNullOrWhiteSpace(destination))
            return Result<bool>.Fail("CopyFile.Destination was not supplied");

        // Ignore it before the copy
        LibraryIgnorePath(MapPath(destination));
        
        var result = FileService.FileCopy(source, destination, true);
        if (result.Failed(out var error))
            return Result<bool>.Fail(error);
        
        if(updateWorkingFile)
        {
            this.WorkingFile = destination;
            try
            {
                // this can fail if the file is then moved really quickly by another process, radarr/sonarr etc
                Logger?.ILog("Initing new copied file");
                InitFile(destination);
            }
            catch (Exception) { }
        }

        return true;
    }

    /// <summary>
    /// Updates the variables in the flow execution
    /// This is so the remaining nodes can now use these variable values.
    /// Note: if a value is null, that item will be removed from the Variable list
    /// </summary>
    /// <param name="updates">The updated values</param>
    public void UpdateVariables(Dictionary<string, object>? updates)
    {
        if (updates == null)
            return;
        foreach (var key in updates.Keys)
        {
            var value = updates[key];
            if (Variables.ContainsKey(key))
            {
                if (value == null)
                    Variables.Remove(key);
                else
                    Variables[key] = value;
            }
            else if(value != null)
                Variables.Add(key, updates[key]);
        }
    }

    /// <summary>
    /// Replaces variables in a given string
    /// </summary>
    /// <param name="input">the input string</param>
    /// <param name="stripMissing">if missing variables should be removed</param>
    /// <param name="cleanSpecialCharacters">if special characters (eg directory path separator) should be replaced</param>
    /// <returns>the string with the variables replaced</returns>
    public string ReplaceVariables(string input, bool stripMissing = false, bool cleanSpecialCharacters = false) 
        => VariablesHelper.ReplaceVariables(input, Variables, stripMissing, cleanSpecialCharacters);

    
    /// <summary>
    /// Gets a safe filename with any reserved characters removed or replaced
    /// </summary>
    /// <param name="fullFileName">the full filename of the file to make safe</param>
    /// <returns>the safe filename</returns>
    public string GetSafeName(string fullFileName)
    {
        string destName = FileHelper.GetShortFileName(fullFileName);
        string destDir = FileHelper.GetDirectory(fullFileName);

        // replace these here to avoid double spaces in name
        if (Path.GetInvalidFileNameChars().Contains(':'))
        {
            destName = destName.Replace(" : ", " - ");
            destName = destName.Replace(": ", " - ");
            destDir = destDir.Replace(" : ", " - ");
            destDir = destDir.Replace(": ", " - ");
        }

        foreach (char c in Path.GetInvalidFileNameChars())
        {
            if (c == ':')
            {
                destName = destName.Replace(c.ToString(), " - ");
                if(c != Path.DirectorySeparatorChar && c != Path.PathSeparator)
                    destDir = destDir.Replace(c.ToString(), " - ");
            }
            else
            {
                destName = destName.Replace(c.ToString(), "");
                if (c != Path.DirectorySeparatorChar && c != Path.PathSeparator)
                    destDir = destDir.Replace(c.ToString(), "");
            }
        }
        // put the drive letter back if it was replaced with a ' - '
        destDir = Regex.Replace(destDir, @"^([a-z]) \- ", "$1:", RegexOptions.IgnoreCase);
        string seperator = destDir.Contains("\\") ? "\\" : "/";
        if (destDir.EndsWith(seperator))
            destDir = destDir[..^1];

        return destDir + seperator + destName;
    }

    /// <summary>
    /// Copies a file into the temporary directory if it is not already in the temporary directory
    /// </summary>
    /// <param name="filename">[Optional] the filename to copy, if not set the working file will be set</param>
    /// <returns>the new filename</returns>
    public string CopyToTemp(string filename = null)
    {
        if (Fake) return filename?.EmptyAsNull() ?? "/mnt/temp/fakefile.mkv";
            
        filename ??= WorkingFile;
        var fileInfo = new FileInfo(filename);
        if (fileInfo.Directory?.FullName == TempPath)
            return filename;
        string dest = Path.Combine(TempPath, fileInfo.Name);
        File.Copy(filename, dest, true);
        return dest;
    }

    /// <summary>
    /// Creates a directory if it does not already exist
    /// </summary>
    /// <param name="directory">the directory path</param>
    /// <returns>true if the directory now exists</returns>
    public bool CreateDirectoryIfNotExists(string directory)
    {
        if (Fake) return true;
        return FileService.DirectoryCreate(directory).ValueOrDefault;
    }

    /// <summary>
    /// Executes a cmd and returns the result
    /// </summary>
    /// <param name="args">The execution parameters</param>
    /// <returns>The result of the command</returns>
    public ProcessResult Execute(ExecuteArgs args)
    {
        if (Fake) return new ProcessResult {  ExitCode = 0, Completed = true };
        
        var result = Process.ExecuteShellCommand(args).GetAwaiter().GetResult();
        return result;
    }

    /// <summary>
    /// Gets a new guid as a string
    /// </summary>
    /// <returns>a new guid as a string</returns>
    public string NewGuid() => Guid.NewGuid().ToString();


    /// <summary>
    /// Loads the plugin settings
    /// </summary>
    /// <typeparam name="T">The plugin settings to load</typeparam>
    /// <returns>The plugin settings</returns>
    public T GetPluginSettings<T>() where T : IPluginSettings
    {
        if (Fake) return default;
        var name = typeof(T).Namespace;
        if (string.IsNullOrEmpty(name))
            return default;
        name = name.Substring(name.IndexOf(".", StringComparison.Ordinal) + 1);
        string json = GetPluginSettingsJson(name);
        if (string.IsNullOrEmpty(json))
            return default;
        try
        {
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (Exception ex)
        {
            Logger.ELog("Failed deserializing plugin settings: " + ex.Message + Environment.NewLine + json);
            throw new Exception("Failed deserializing plugin settings: " + ex.Message);
        }
    }

    /// <summary>
    /// Gets the physical path of a tool
    /// Note: this is the unmapped path and if on a remote node will have to be mapped
    /// </summary>
    /// <param name="tool">the name of the tool to get</param>
    /// <returns>the physical path of a tool</returns>
    public string GetToolPath(string tool)
    {
        if (Fake || GetToolPathActual == null) return string.Empty;
        return GetToolPathActual(tool);
    }

    /// <summary>
    /// Gets a variable from the variable list if exists
    /// </summary>
    /// <param name="name">the name of the variable</param>
    /// <returns>the value of the variable, else null if not found</returns>
    public object? GetVariable(string name)
    {
        if(this.Variables?.ContainsKey(name) == true)
            return this.Variables[name];
        return null;
    }

    /// <summary>
    /// Tests if a input string matches a variable
    /// </summary>
    /// <param name="variableName">The name of the variable</param>
    /// <param name="input">the input string</param>
    /// <returns>true if matches, otherwise false</returns>
    public bool MatchesVariable(string variableName, string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            Logger.ILog("Input not set, does not match anything");
            return false;
        }

        var variable = GetToolPathActual(variableName);
        if (string.IsNullOrEmpty(variable))
        {
            Logger.WLog("Variable not found: " + variableName);
            return false;
        }

        foreach (var line in variable.Split(new string[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (string.Equals(line, input, StringComparison.InvariantCultureIgnoreCase))
                return true;
            int lastIndex = line.LastIndexOf("/", StringComparison.Ordinal);
            if (line.StartsWith("/") && lastIndex > 0)
            {
                // try a regex
                try
                {
                    string rgxCompare = line[1..lastIndex];
                    string opt = line.Substring(lastIndex + 1);
                    var options = RegexOptions.None;
                    if (opt.IndexOf("i", StringComparison.Ordinal) >= 0)
                        options |= RegexOptions.IgnoreCase;
                    var rgx = new Regex(rgxCompare, options);
                    if (rgx.IsMatch(input))
                        return true;
                }
                catch (Exception)
                {

                }
            }
        }

        return false;
    }

    /// <summary>
    /// Records a running totals statistic with the server
    /// </summary>
    /// <param name="name">the name of the statistic</param>
    /// <param name="value">the value of the statistic</param>
    public void RecordStatisticRunningTotals(string name, string value) 
        => StatisticRecorderRunningTotals?.Invoke(name, value);

    /// <summary>
    /// Records a average statistic with the server
    /// </summary>
    /// <param name="name">the name of the statistic</param>
    /// <param name="value">the value of the statistic</param>
    public void RecordStatisticAverage(string name, int value) 
        => StatisticRecorderAverage?.Invoke(name, value);
    
    /// <summary>
    /// Records a memory statistics that is not saved anywhere and will expire
    /// </summary>
    /// <param name="name">the name of the statistic</param>
    /// <param name="value">the value of the statistic</param>
    /// <param name="steps">the number of steps to keep this info around for</param>
    /// <param name="expiry">expiry time of this info</param>
    public void RecordAdditionalInfo(string name, object value, int steps, TimeSpan? expiry) 
        => AdditionalInfoRecorder?.Invoke(name, value, steps, expiry);
}


/// <summary>
/// The possible results of a executed node/flow part
/// </summary>
public enum NodeResult
{
    /// <summary>
    /// The execution failed
    /// </summary>
    Failure = 0,
    /// <summary>
    /// The execution succeeded
    /// </summary>
    Success = 1,
}

/// <summary>
/// Reprocess options for a file
/// </summary>
public class ReprocessOptions
{
    /// <summary>
    /// Gets or sets a another processing node to reprocess this file on.
    /// This will be marked for reprocessing once completed.
    /// </summary>
    public ObjectReference? ReprocessNode { get; set; }
    
    /// <summary>
    /// Gets or sets if the file should be held for a number of minutes before reprocessing
    /// </summary>
    public int? HoldForMinutes { get; set; }
}