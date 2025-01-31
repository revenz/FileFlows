using FileFlows.Plugin;
using FileFlows.Plugin.Models;
using FileFlows.Plugin.Services;
using FileFlows.Shared.Models;

namespace FileFlows.ServerShared.FileServices;

/// <summary>
/// Implementation of <see cref="IFileService"/> that maps file operations to a remote node using <see cref="ProcessingNode"/>.
/// </summary>
public class MappedFileService : IFileService
{
    /// <summary>
    /// Gets or sets the path separator based on the system's directory separator character.
    /// </summary>
    public char PathSeparator { get; init; } = Path.DirectorySeparatorChar;

    /// <summary>
    /// Gets or sets a function for replacing variables in a string.
    /// </summary>
    /// <remarks>
    /// The function takes a string input, a boolean indicating whether to strip missing variables,
    /// and a boolean indicating whether to clean special characters.
    /// </remarks>
    public ReplaceVariablesDelegate? ReplaceVariables { get; set; }

    /// <summary>
    /// Gets or sets the logger used for logging
    /// </summary>
    public ILogger? Logger { get; set; }

    private readonly IFileService _localFileService;
    private ProcessingNode _node;

    /// <summary>
    /// Initializes a new instance of the <see cref="MappedFileService"/> class with the specified <paramref name="node"/>.
    /// </summary>
    /// <param name="node">The processing node used for mapping file operations.</param>
    /// <param name="logger">the logger</param>
    /// <param name="dontUseTemporaryFilesForMoveCopy">If temporary files should not be used for move/copy</param>
    public MappedFileService(ProcessingNode node, ILogger logger, bool dontUseTemporaryFilesForMoveCopy)
    {
        Logger = logger;
        int permissions = node.PermissionsFiles ?? Globals.DefaultPermissionsFile;
        _localFileService = new LocalFileService(dontUseTemporaryFilesForMoveCopy)
        {
            Logger = logger,
            PermissionsFile = permissions,
            PermissionsFolder = node.PermissionsFolders ?? Globals.DefaultPermissionsFile
        };
        _node = node;
    }

    /// <summary>
    /// Maps a path using the associated <see cref="ProcessingNode"/>.
    /// </summary>
    /// <param name="path">The path to be mapped.</param>
    /// <returns>The mapped path.</returns>
    private string Map(string path)
    {
        if(ReplaceVariables != null)
            path = ReplaceVariables(path, true);

        string original = path;
        string mapped = _node.Map(path);
        if (original != mapped)
            Logger?.ILog($"Path mapped '{original}' => '{mapped}'");
        else
            Logger?.ILog($"Path did not need mapping: {mapped}");
        return mapped;
    }

    /// <summary>
    /// Gets the files in the specified directory with optional search pattern and recursion.
    /// </summary>
    /// <param name="path">The directory path.</param>
    /// <param name="searchPattern">The search pattern for file names.</param>
    /// <param name="recursive">Indicates whether to search recursively.</param>
    /// <returns>A result containing an array of file paths.</returns>
    public Result<string[]> GetFiles(string path, string searchPattern = "", bool recursive = false)
        => _localFileService.GetFiles(Map(path), searchPattern, recursive);

    /// <summary>
    /// Gets the directories in the specified path.
    /// </summary>
    /// <param name="path">The directory path.</param>
    /// <returns>A result containing an array of directory paths.</returns>
    public Result<string[]> GetDirectories(string path)
        => _localFileService.GetDirectories(Map(path));

    /// <summary>
    /// Checks whether the specified directory exists.
    /// </summary>
    /// <param name="path">The directory path.</param>
    /// <returns>A result indicating whether the directory exists.</returns>
    public Result<bool> DirectoryExists(string path)
        => _localFileService.DirectoryExists(Map(path));

    /// <inheritdoc />
    public Result<bool> DirectoryEmpty(string path, string[]? includePatterns = null)
        => _localFileService.DirectoryEmpty(Map(path), includePatterns);

    /// <summary>
    /// Deletes the specified directory.
    /// </summary>
    /// <param name="path">The directory path.</param>
    /// <param name="recursive">Indicates whether to delete recursively.</param>
    /// <returns>A result indicating whether the directory was successfully deleted.</returns>
    public Result<bool> DirectoryDelete(string path, bool recursive = false)
        => _localFileService.DirectoryDelete(Map(path), recursive);

    /// <summary>
    /// Moves the specified directory to the specified destination.
    /// </summary>
    /// <param name="path">The source directory path.</param>
    /// <param name="destination">The destination directory path.</param>
    /// <returns>A result indicating whether the directory was successfully moved.</returns>
    public Result<bool> DirectoryMove(string path, string destination)
        => _localFileService.DirectoryMove(Map(path), destination);

    /// <summary>
    /// Creates a directory at the specified path.
    /// </summary>
    /// <param name="path">The directory path to be created.</param>
    /// <returns>A result indicating whether the directory was successfully created.</returns>
    public Result<bool> DirectoryCreate(string path)
        => _localFileService.DirectoryCreate(Map(path));

    /// <inheritdoc />
    public Result<DateTime> DirectoryCreationTimeUtc(string path)
        => _localFileService.DirectoryCreationTimeUtc(Map(path));

    /// <inheritdoc />
    public Result<DateTime> DirectoryLastWriteTimeUtc(string path)
        => _localFileService.DirectoryLastWriteTimeUtc(Map(path));

    /// <summary>
    /// Checks whether the specified file exists.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>A result indicating whether the file exists.</returns>
    public Result<bool> FileExists(string path)
        => _localFileService.FileExists(Map(path));

    /// <summary>
    /// Gets information about the specified file.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>A result containing information about the file.</returns>
    public Result<FileInformation> FileInfo(string path)
        => _localFileService.FileInfo(Map(path));

    /// <summary>
    /// Deletes the specified file.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>A result indicating whether the file was successfully deleted.</returns>
    public Result<bool> FileDelete(string path)
        => _localFileService.FileDelete(Map(path));

    /// <summary>
    /// Gets the size of the specified file.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>A result containing the size of the file in bytes.</returns>
    public Result<long> FileSize(string path)
        => _localFileService.FileSize(Map(path));

    /// <summary>
    /// Gets the UTC creation time of the specified file.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>A result containing the UTC creation time of the file.</returns>
    public Result<DateTime> FileCreationTimeUtc(string path)
        => _localFileService.FileCreationTimeUtc(Map(path));

    /// <summary>
    /// Gets the UTC last write time of the specified file.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>A result containing the UTC last write time of the file.</returns>
    public Result<DateTime> FileLastWriteTimeUtc(string path)
        => _localFileService.FileLastWriteTimeUtc(Map(path));

    /// <summary>
    /// Moves the specified file to the specified destination.
    /// </summary>
    /// <param name="path">The source file path.</param>
    /// <param name="destination">The destination file path.</param>
    /// <param name="overwrite">Indicates whether to overwrite the destination file if it already exists.</param>
    /// <returns>A result indicating whether the file was successfully moved.</returns>
    public Result<bool> FileMove(string path, string destination, bool overwrite = true)
        => _localFileService.FileMove(Map(path), Map(destination), overwrite);

    /// <summary>
    /// Copies the specified file to the specified destination.
    /// </summary>
    /// <param name="path">The source file path.</param>
    /// <param name="destination">The destination file path.</param>
    /// <param name="overwrite">Indicates whether to overwrite the destination file if it already exists.</param>
    /// <returns>A result indicating whether the file was successfully copied.</returns>
    public Result<bool> FileCopy(string path, string destination, bool overwrite = true)
        => _localFileService.FileCopy(Map(path), Map(destination), overwrite);

    /// <summary>
    /// Appends the specified text to the end of the specified file.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <param name="text">The text to append to the file.</param>
    /// <returns>A result indicating whether the text was successfully appended to the file.</returns>
    public Result<bool> FileAppendAllText(string path, string text)
        => _localFileService.FileAppendAllText(Map(path), text);

    /// <summary>
    /// Checks if the specified file is local.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>True if the file is local; otherwise, false.</returns>
    public bool FileIsLocal(string path)
        => _localFileService.FileIsLocal(Map(path));

    /// <summary>
    /// Gets the local path for the specified mapped path.
    /// </summary>
    /// <param name="path">The mapped path.</param>
    /// <returns>A result containing the local path.</returns>
    public Result<string> GetLocalPath(string path)
        => _localFileService.GetLocalPath(Map(path));

    /// <summary>
    /// Touches (updates the last access and modification time) the specified file.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>A result indicating whether the file was successfully touched.</returns>
    public Result<bool> Touch(string path)
        => _localFileService.Touch(Map(path));

    /// <inheritdoc />
    public Result<long> DirectorySize(string path)
        => _localFileService.DirectorySize(Map(path));

    /// <summary>
    /// Sets the UTC creation time of the specified file.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <param name="date">The UTC creation time to set.</param>
    /// <returns>A result indicating whether the creation time was successfully set.</returns>
    public Result<bool> SetCreationTimeUtc(string path, DateTime date)
        => _localFileService.SetCreationTimeUtc(Map(path), date);

    /// <summary>
    /// Sets the UTC last write time of the specified file.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <param name="date">The UTC last write time to set.</param>
    /// <returns>A result indicating whether the last write time was successfully set.</returns>
    public Result<bool> SetLastWriteTimeUtc(string path, DateTime date)
        => _localFileService.SetLastWriteTimeUtc(Map(path), date);
}