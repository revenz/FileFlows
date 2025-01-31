using FileFlows.Plugin;
using FileFlows.Plugin.Models;
using FileFlows.Plugin.Services;
using FileHelper = FileFlows.Plugin.Helpers.FileHelper;

namespace FileFlows.ServerShared.FileServices;

/// <summary>
/// Local file service
/// </summary>
/// <param name="dontUseTemporaryFilesForMoveCopy">If temporary files should not be used for move/copy</param>
public class LocalFileService(bool dontUseTemporaryFilesForMoveCopy) : IFileService
{
    /// <summary>
    /// Gets or sets the path separator for the file system
    /// </summary>
    public char PathSeparator { get; init; } = Path.DirectorySeparatorChar;
    
    /// <summary>
    /// Gets or sets the allowed paths the file service can access
    /// </summary>
    public string[]? AllowedPaths { get; init; }
    
    /// <summary>
    /// Gets or sets a function for replacing variables in a string.
    /// </summary>
    /// <remarks>
    /// The function takes a string input, a boolean indicating whether to strip missing variables,
    /// and a boolean indicating whether to clean special characters.
    /// </remarks>
    public ReplaceVariablesDelegate? ReplaceVariables { get; set; }

    private int? _PermissionsFile;

    /// <summary>
    /// Gets or sets the permissions to use for files
    /// </summary>
    public int? PermissionsFile
    {
        get
        {
            if (_PermissionsFile is null or < 1 or > 777)
                return Globals.DefaultPermissionsFile;
            return _PermissionsFile.Value;
        }
        set => _PermissionsFile = value;
    }

    private int? _PermissionsFolder;
    /// <summary>
    /// Gets or sets the permissions to use for folders
    /// </summary>
    public int? PermissionsFolder 
    {
        get
        {
            if (_PermissionsFolder is null or < 1 or > 777)
                return Globals.DefaultPermissionsFolder;
            return _PermissionsFolder.Value;
        }
        set => _PermissionsFolder = value;
    }
    
    /// <summary>
    /// Gets or sets the owner:group to use for files
    /// </summary>
    public string OwnerGroup { get; set; } = null!;

    /// <summary>
    /// Gets or sets the logger used for logging
    /// </summary>
    public ILogger? Logger { get; set; }
    
    /// <summary>
    /// Gets or sets if protective paths should be checked
    /// </summary>
    public bool CheckProtectivePaths { get; set; }

    /// <inheritdoc />
    public Result<string[]> GetFiles(string path, string searchPattern = "", bool recursive = false)
    {
        if (IsProtectedPath(ref path))
            return Result<string[]>.Fail("Cannot access protected path: " + path);
        try
        {
            return Directory.GetFiles(path, searchPattern ?? string.Empty,
                recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
        }
        catch (Exception)
        {
            return new string[] { };
        }
    }

    /// <inheritdoc />
    public Result<string[]> GetDirectories(string path)
    {
        if (IsProtectedPath(ref path))
            return Result<string[]>.Fail("Cannot access protected path: " + path);
        try
        {
            return Directory.GetDirectories(path);
        }
        catch (Exception)
        {
            return new string[] { };
        }
    }

    /// <inheritdoc />
    public Result<bool> DirectoryExists(string path)
    {
        if (IsProtectedPath(ref path))
            return Result<bool>.Fail("Cannot access protected path: " + path);
        try
        {
            return Directory.Exists(path);
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public Result<bool> DirectoryEmpty(string path, string[]? includePatterns = null)
    {
        if (IsProtectedPath(ref path))
            return Result<bool>.Fail("Cannot access protected path: " + path);
        try
        {
            if (Directory.Exists(path) == false)
                return true; // Path doesn't exist, considered empty
            
            // Get all files in the directory
            var files = Directory.GetFiles(path);

            // If there are patterns, only count matching files
            if (includePatterns is { Length: > 0 })
            {
                foreach (var file in files)
                {
                    foreach (var pattern in includePatterns)
                    {
                        try
                        {
                            if (System.Text.RegularExpressions.Regex.IsMatch(file, pattern.Trim(),
                                    System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            {
                                Logger?.ILog("Matching file found: " + file);
                                return false; // File matches, directory is not empty
                            }
                        }
                        catch (Exception)
                        {
                            // Handle regex exceptions silently
                        }
                    }
                }
            }
            else if (files.Length > 0)
            {
                // No patterns provided, directory is not empty if any file exists
                return false;
            }

            // Check for directories (subdirectories are not affected by includePatterns)
            var dirs = Directory.GetDirectories(path);
            if (dirs.Length > 0)
                return false; // Directory contains subdirectories, not empty

            return true; // Directory is empty
        }
        catch (UnauthorizedAccessException ex)
        {
            return Result<bool>.Fail("Unauthorized access to path: " + path + " - " + ex.Message);
        }
        catch (IOException ex)
        {
            return Result<bool>.Fail("IO error while accessing path: " + path + " - " + ex.Message);
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail("Error while accessing path: " + path + " - " + ex.Message);
        }
    }

    /// <inheritdoc />
    public Result<bool> DirectoryDelete(string path, bool recursive = false)
    {
        if (IsProtectedPath(ref path))
            return Result<bool>.Fail("Cannot access protected path: " + path);
        try
        {
            Directory.Delete(path, recursive);
            return true;
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(ex.Message);
        }
    }

    /// <inheritdoc />
    public Result<bool> DirectoryMove(string path, string destination)
    {
        if (IsProtectedPath(ref path))
            return Result<bool>.Fail("Cannot access protected path: " + path);
        if (IsProtectedPath(ref destination))
            return Result<bool>.Fail("Cannot access protected path: " + destination);
        try
        {
            Directory.Move(path, destination);
            SetPermissions(destination);
            return true;
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(ex.Message);
        }
    }

    /// <inheritdoc />
    public Result<bool> DirectoryCreate(string path)
    {
        if (IsProtectedPath(ref path))
            return Result<bool>.Fail("Cannot access protected path: " + path);
        try
        {
            CreateDirectoryIfNotExists(path);
            return true;
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(ex.Message);
        }
    }

    /// <inheritdoc />
    public Result<DateTime> DirectoryCreationTimeUtc(string path)
    {
        if (IsProtectedPath(ref path))
            return Result<DateTime>.Fail("Cannot access protected path: " + path);
        try
        {
            var dir = new DirectoryInfo(path);
            if (dir.Exists == false)
                return Result<DateTime>.Fail($"Directory '{path}' does not exist");
            return dir.CreationTimeUtc;
        }
        catch (Exception ex)
        {
            return Result<DateTime>.Fail(ex.Message);
        }
    }

    /// <inheritdoc />
    public Result<DateTime> DirectoryLastWriteTimeUtc(string path)
    {
        if (IsProtectedPath(ref path))
            return Result<DateTime>.Fail("Cannot access protected path: " + path);
        try
        {
            var dir = new DirectoryInfo(path);
            if (dir.Exists == false)
                return Result<DateTime>.Fail($"Directory '{path}' does not exist");
            return dir.LastWriteTimeUtc;
        }
        catch (Exception ex)
        {
            return Result<DateTime>.Fail(ex.Message);
        }
    }

    /// <inheritdoc />
    public Result<bool> FileExists(string path)
    {
        if (IsProtectedPath(ref path))
            return Result<bool>.Fail("Cannot access protected path: " + path);
        try
        {
            return File.Exists(path);
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public Result<FileInformation> FileInfo(string path)
    {
        if (IsProtectedPath(ref path))
            return Result<FileInformation>.Fail("Cannot access protected path: " + path);
        try
        {
            FileInfo fileInfo = new FileInfo(path);

            return new FileInformation
            {
                CreationTime = fileInfo.CreationTime,
                CreationTimeUtc = fileInfo.CreationTimeUtc,
                LastWriteTime = fileInfo.LastWriteTime,
                LastWriteTimeUtc = fileInfo.LastWriteTimeUtc,
                Extension = fileInfo.Extension,
                Name = fileInfo.Name,
                FullName = fileInfo.FullName,
                Length = fileInfo.Length,
                Directory = fileInfo.DirectoryName ?? string.Empty
            };
        }
        catch (Exception ex)
        {
            return Result<FileInformation>.Fail(ex.Message);
        }
    }

    /// <inheritdoc />
    public Result<bool> FileDelete(string path)
    {
        if (IsProtectedPath(ref path))
            return Result<bool>.Fail("Cannot access protected path: " + path);
        try
        {
            var fileInfo = new FileInfo(path);
            if(fileInfo.Exists)
                fileInfo.Delete();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public Result<long> FileSize(string path)
    {
        if (IsProtectedPath(ref path))
            return Result<long>.Fail("Cannot access protected path: " + path);
        try
        {
            var fileInfo = new FileInfo(path);
            if (fileInfo.Exists == false)
                return Result<long>.Fail("File does not exist");
            return fileInfo.Length;
        }
        catch (Exception ex)
        {
            return Result<long>.Fail(ex.Message);
        }
    }

    /// <inheritdoc />
    public Result<DateTime> FileCreationTimeUtc(string path)
    {
        if (IsProtectedPath(ref path))
            return Result<DateTime>.Fail("Cannot access protected path: " + path);
        try
        {
            var fileInfo = new FileInfo(path);
            if (fileInfo.Exists == false)
                return Result<DateTime>.Fail("File does not exist");
            return fileInfo.CreationTimeUtc;
        }
        catch (Exception ex)
        {
            return Result<DateTime>.Fail(ex.Message);
        }
    }

    /// <inheritdoc />
    public Result<DateTime> FileLastWriteTimeUtc(string path)
    {
        if (IsProtectedPath(ref path))
            return Result<DateTime>.Fail("Cannot access protected path: " + path);
        try
        {
            var fileInfo = new FileInfo(path);
            if (fileInfo.Exists == false)
                return Result<DateTime>.Fail("File does not exist");
            return fileInfo.LastWriteTimeUtc;
        }
        catch (Exception ex)
        {
            return Result<DateTime>.Fail(ex.Message);
        }
    }

    /// <inheritdoc />
    public Result<bool> FileMove(string path, string destination, bool overwrite = true)
    {
        if (IsProtectedPath(ref path))
            return Result<bool>.Fail("Cannot access protected path: " + path);
        if (IsProtectedPath(ref destination))
            return Result<bool>.Fail("Cannot access protected path: " + destination);
        try
        {
            Logger?.ILog("LocalFileService.FileMove: Path: " + path);
            Logger?.ILog("LocalFileService.FileMove: Destination: " + destination);
            Logger?.ILog("LocalFileService.FileMove: Overwrite: " + overwrite);

            var fileInfo = new FileInfo(path);
            if (fileInfo.Exists == false)
                return Result<bool>.Fail("File does not exist");
            Logger?.ILog("File exists: " + path);
            var destDir = new FileInfo(destination).Directory;
            Logger?.ILog("Checking destination exists: " + destDir);
            CreateDirectoryIfNotExists(destDir?.FullName!);
            if (File.Exists(destination))
            {
                if (overwrite == false)
                {
                    Logger?.ILog("File already exists: " + destination);
                    return false;
                }

                try
                {
                    File.Delete(destination);
                }
                catch (Exception)
                {
                    // Ignored
                }
            }

            if (dontUseTemporaryFilesForMoveCopy)
            {
                if (DoMove(fileInfo.FullName, destination, overwrite) == false)
                    return Result<bool>.Fail("Failed to move file to final file");
            }
            else
            {
                var tempDest = destination + ".fftemp";

                if (DoMove(fileInfo.FullName, tempDest, true) == false)
                    return Result<bool>.Fail("Failed to move file to temp file");
                if (DoMove(tempDest, destination, overwrite) == false)
                    return Result<bool>.Fail("Failed to move temp file to final file");
            }

            SetPermissions(destination);
            return true;
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(ex.Message);
        }

        bool DoMove(string fileName, string destination, bool overwrite)
        {
            Logger?.ILog($"About to move file '{fileName}' to '{destination}'");
            int count = 0;
            while (count++ < 4)
            {
                try
                {
                    File.Move(fileName, destination, overwrite);
                    return true;
                }
                catch (Exception ex)
                {
                    if (OperatingSystem.IsMacOS() && ex.Message.Contains("Input/output error",
                            StringComparison.InvariantCultureIgnoreCase))
                    {
                        Logger?.ILog("Input/output error, retrying move");
                        Thread.Sleep(count * 30_000);
                    }
                    else if (ex is IOException && ex.Message.Contains("Access to the path") &&
                             ex.Message.Contains("is denied"))
                    {
                        Logger?.ILog("Access denied error, retrying move");
                        Thread.Sleep(count * 30_000);
                    }
                    else
                    {
                        return Result<bool>.Fail(ex.Message);
                    }
                }
            }

            return false;
        }
    }
    

    /// <summary>
    /// Creates a directory at the specified path if it does not already exist.
    /// </summary>
    /// <param name="path">The path of the directory to create.</param>
    /// <remarks>
    /// If the directory already exists, this method does nothing.
    /// </remarks>
    private void CreateDirectoryIfNotExists(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;
        
        if (Directory.Exists(path))
            return;
        try
        {
            Logger?.ILog("Directory does not exist, creating: " + path);
            if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS())
            {
                Directory.CreateDirectory(path);
                return;
            }

            Logger?.ILog("Recursively creating directory: " + path);
            Stack<string> directoryStack = new Stack<string>();
            string? currentPath = path;

            // Push all parent directories onto the stack until we reach the root directory
            while (Directory.Exists(currentPath) == false)
            {
                directoryStack.Push(currentPath);
                currentPath = Path.GetDirectoryName(currentPath);
                if (string.IsNullOrEmpty(currentPath))
                {
                    throw new DirectoryNotFoundException("Parent directory not found.");
                }
            }

            // Create directories and set permissions for each directory in the stack
            while (directoryStack.Count > 0)
            {
                string dir = directoryStack.Pop();
                Logger?.ILog("Creating path: " + dir);
                Directory.CreateDirectory(dir);
                SetPermissions(dir);
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to created directory: " + ex.Message);
        }
    }

    /// <inheritdoc />
    public Result<bool> FileCopy(string path, string destination, bool overwrite = true)
    {
        if (IsProtectedPath(ref path))
            return Result<bool>.Fail("Cannot access protected path: " + path);
        if (IsProtectedPath(ref destination))
            return Result<bool>.Fail("Cannot access protected path: " + destination);
        try
        {
            var fileInfo = new FileInfo(path);
            if (fileInfo.Exists == false)
                return Result<bool>.Fail("File does not exist");
            
            var destDir = new FileInfo(destination).Directory;
            CreateDirectoryIfNotExists(destDir?.FullName!);
            
            if (File.Exists(destination))
            {
                if (overwrite == false)
                {
                    Logger?.ILog("File already exists: " + destination);
                    return false;
                }

                try
                {
                    File.Delete(destination);
                }
                catch (Exception)
                {
                    // Ignored
                }
            }

            if (dontUseTemporaryFilesForMoveCopy)
            {
                if (DoCopy(fileInfo.FullName, destination, overwrite, move: true) == false)
                    return Result<bool>.Fail("Failed to copy file to final file");
            }
            else
            {

                var tempDest = destination + ".fftemp";
                if (DoCopy(fileInfo.FullName, tempDest, true) == false)
                    return Result<bool>.Fail("Failed to copy file to temp file");
                if (DoCopy(tempDest, destination, overwrite, move: true) == false)
                    return Result<bool>.Fail("Failed to copy temp file to final file");
            }

            SetPermissions(destination);
            return true;
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(ex.Message);
        }

        bool DoCopy(string fileName, string dest, bool replace, bool move = false)
        {
            int count = 0;
            while (count++ < 4)
            {
                try
                {
                    if(move)
                        File.Move(fileName, dest, replace);
                    else
                        File.Copy(fileName, dest, replace);
                    return true;
                }
                catch (Exception ex)
                {
                    if (OperatingSystem.IsMacOS() && ex.Message.Contains("Input/output error", StringComparison.InvariantCultureIgnoreCase))
                    {
                        Logger?.ILog("Input/output error, retrying move");
                        Thread.Sleep(count * 30_000);
                    }
                    else if (ex is IOException && ex.Message.Contains("Access to the path") &&
                             ex.Message.Contains("is denied"))
                    {
                        Logger?.ILog("Access denied error, retrying copy");
                        Thread.Sleep(count * 30_000);
                    }
                    else
                    {
                        return Result<bool>.Fail(ex.Message);
                    }
                }
            }

            return false;
        }
    }
    
    /// <inheritdoc />
    public Result<bool> FileAppendAllText(string path, string text)
    {
        if (IsProtectedPath(ref path))
            return Result<bool>.Fail("Cannot access protected path: " + path);
        try
        {
            File.AppendAllText(path, text);
            SetPermissions(path);
            return true;
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(ex.Message);
        }
    }

    /// <inheritdoc />
    public bool FileIsLocal(string path) => true;

    /// <summary>
    /// Gets the local path
    /// </summary>
    /// <param name="path">the path</param>
    /// <returns>the local path to the file</returns>
    public Result<string> GetLocalPath(string path)
        => Result<string>.Success(path);

    /// <inheritdoc />
    public Result<bool> Touch(string path)
    {
        if (IsProtectedPath(ref path))
            return Result<bool>.Fail("Cannot access protected path: " + path);
        
        if (DirectoryExists(path).Is(true))
        {
            try
            {
                Directory.SetLastWriteTimeUtc(path, DateTime.UtcNow);
                return true;
            }
            catch (Exception ex)
            {
                return Result<bool>.Fail("Failed to touch directory: " + ex.Message);
            }
        }
        
        try
        {
            if (File.Exists(path))
                File.SetLastWriteTimeUtc(path, DateTime.UtcNow);
            else
            {
                File.Create(path);
                SetPermissions(path);
            }

            return true;
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail($"Failed to touch file: '{path}' => {ex.Message}");
        }
    }

    /// <inheritdoc />
    public Result<long> DirectorySize(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return 0;
        
        if (File.Exists(path))
            path = new FileInfo(path).Directory?.FullName ?? string.Empty;
        
        if (string.IsNullOrWhiteSpace(path))
            return 0;
        
        if (Directory.Exists(path) == false)
            return 0;
        
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

    /// <inheritdoc />
    public Result<bool> SetCreationTimeUtc(string path, DateTime date)
    {
        if (IsProtectedPath(ref path))
            return Result<bool>.Fail("Cannot access protected path: " + path);
        try
        {
            if (!File.Exists(path))
                return Result<bool>.Fail("File not found.");

            File.SetCreationTimeUtc(path, date);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail($"Error setting creation time: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public Result<bool> SetLastWriteTimeUtc(string path, DateTime date)
    {
        if (IsProtectedPath(ref path))
            return Result<bool>.Fail("Cannot access protected path: " + path);
        try
        {
            if (!File.Exists(path))
                return Result<bool>.Fail("File not found.");

            File.SetLastWriteTimeUtc(path, date);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail($"Error setting last write time: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if a path is accessible by the file server
    /// </summary>
    /// <param name="path">the path to check</param>
    /// <returns>true if accessible, otherwise false</returns>
    public bool IsProtectedPath(ref string path)
    {
        if (CheckProtectivePaths == false)
            return false;
        
        if (OperatingSystem.IsWindows())
            path = path.Replace("/", "\\");
        else
            path = path.Replace("\\", "/");
        
        if(ReplaceVariables != null)
            path = ReplaceVariables(path, true);
        
        if (FileHelper.IsSystemDirectory(path))
            return true; // a system directory, no access

        if (AllowedPaths?.Any() != true)
            return false; // no allowed paths configured, allow all

        if (OperatingSystem.IsWindows())
            path = path.ToLowerInvariant();
        
        for(int i=0;i<AllowedPaths.Length;i++)
        {
            string p = OperatingSystem.IsWindows() ? AllowedPaths[i].ToLowerInvariant().TrimEnd('\\') : AllowedPaths[i].TrimEnd('/');
            if (path.StartsWith(p))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Sets permissions on a file or foolder
    /// </summary>
    /// <param name="path">the path</param>
    /// <param name="logMethod">the log method</param>
    public void SetPermissions(string path, Action<string>? logMethod = null)
    {
        logMethod ??= (string message) => Logger?.ILog(message);

        bool isFile = File.Exists(path);
        bool isFolder = Directory.Exists(path);
        if(isFile == false && isFolder == false)
        {
            logMethod($"SetPermissions: '{path}' doesnt existing, skipping");
            return;
        }

        int permissions = isFile ? (PermissionsFile ?? Globals.DefaultPermissionsFile) : (PermissionsFolder ?? Globals.DefaultPermissionsFolder);

        StringLogger stringLogger = new StringLogger();

        FileHelper.SetPermissions(stringLogger, path, file: isFile, permissions: permissions);
        
        FileHelper.ChangeOwner(stringLogger, path, file: isFile, ownerGroup: OwnerGroup);
        
        logMethod(stringLogger.ToString());
    }
}