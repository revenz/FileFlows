using System.IO.Compression;
using System.Text.RegularExpressions;
using FileFlows.FlowRunner.Helpers.ArchiveHelpers;
using FileFlows.Plugin;
using FileFlows.Plugin.Helpers;
using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Common;
using SharpCompress.Readers;
using ZipFile = System.IO.Compression.ZipFile;

namespace FileFlows.FlowRunner.Helpers;

/// <summary>
/// Archive helper
/// </summary>
public partial class ArchiveHelper : IArchiveHelper
{
    private readonly ILogger Logger;
    private readonly RarArchiveHelper rarHelper;
    private readonly SevenZipArchiveHelper sevenHelper;

    /// <summary>
    /// Initialises a new instance of the archive helper
    /// </summary>
    /// <param name="args">the Node Parameters</param>
    public ArchiveHelper(NodeParameters args)
    {
        Logger = args.Logger;
        rarHelper = new(args);
        sevenHelper = new SevenZipArchiveHelper(args);
    }

    /// <summary>
    /// Initialises a new instance of the archive helper
    /// </summary>
    /// <param name="logger">The logger</param>
    /// <param name="rarExecutable">The rar executable</param>
    /// <param name="unrarExecutable">The unrar executable</param>
    /// <param name="sevenZipExecutable">The seven zip executable</param>
    public ArchiveHelper(ILogger logger, string rarExecutable, string unrarExecutable, string sevenZipExecutable)
    {
        Logger = logger;
        rarHelper = new(logger, rarExecutable, unrarExecutable);;
        sevenHelper = new(logger, sevenZipExecutable);
    }

    /// <summary>
    /// Gets if the archive is a rar file
    /// </summary>
    /// <param name="archivePath">the archive path</param>
    /// <returns>true if is a rar file, otherwise false</returns>
    private bool IsRar(string archivePath)
        => IsRarRegex().IsMatch(archivePath);
    
    /// <inheritdoc />
    public Result<bool> FileExists(string archivePath, string file)
    {
        // Check if the archive file exists
        if (File.Exists(archivePath) == false)
            return Result<bool>.Fail("Archive file not found: " + archivePath);

        try
        {
            // Open the zip archive
            using ZipArchive archive = ZipFile.OpenRead(archivePath);
            return archive.Entries.Any(x => x.FullName.ToLowerInvariant().Equals(file.ToLowerInvariant()));
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(ex.Message);
        }
    }

    /// <inheritdoc />
    public Result<bool> AddToArchive(string archivePath, string file)
    {
        // Check if the archive file exists
        if (File.Exists(archivePath) == false)
            return Result<bool>.Fail("Archive file not found: " + archivePath);

        if (archivePath.ToLowerInvariant().EndsWith(".rar") || archivePath.ToLowerInvariant().EndsWith(".cbr"))
            return rarHelper.AddToArchive(archivePath, file);

        try
        {
            // Open the zip archive
            using ZipArchive archive = ZipFile.Open(archivePath, ZipArchiveMode.Update);

            // Create a new entry for the file
            archive.CreateEntryFromFile(file, Path.GetFileName(file));

            // Successfully added the file
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(ex.Message);
        }
    }

    /// <inheritdoc />
    public Result<int> GetFileCount(string archivePath, string pattern)
    {
        // Check if the archive file exists
        if (File.Exists(archivePath) == false)
            return Result<int>.Fail("Archive file not found: " + archivePath);
        bool isRar = IsRar(archivePath);
        try
        {
            var rgxFiles = new Regex(pattern, RegexOptions.IgnoreCase);
            using var archive = ArchiveFactory.Open(archivePath);
            var files = archive.Entries.Where(entry => !entry.IsDirectory).ToArray();
            return files.Count(x => x.Key != null && rgxFiles.IsMatch(x.Key));
        }
        catch (Exception ex) when (isRar && ex.Message.Contains("Unknown Rar Header"))
        {
            return rarHelper.GetFileCount(archivePath, pattern);
        }
    }

    /// <inheritdoc />
    public Result<bool> Compress(string path, string output, string pattern = "",
        bool allDirectories = true, Action<float>? percentCallback = null)
    {
        if (IsRar(output))
            return rarHelper.Compress(path, output, pattern, allDirectories, percentCallback);

        var dir = new DirectoryInfo(path);
        if (dir.Exists)
        {
            var files = dir.GetFiles(pattern?.StartsWith("*") == true ? pattern : "*",
                allDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            if (string.IsNullOrWhiteSpace(pattern) == false && pattern.StartsWith("*") == false)
            {
                var regex = new Regex(pattern, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
                files = files.Where(x => regex.IsMatch(x.Name)).ToArray();
            }

            using FileStream fs = new FileStream(output, FileMode.Create);
            using ZipArchive arch = new ZipArchive(fs, ZipArchiveMode.Create);
            percentCallback?.Invoke(0);
            float current = 0;
            float count = files.Length;
            foreach (var file in files)
            {
                ++count;
                string relative = file.FullName.Substring(dir.FullName.Length + 1);
                try
                {
                    arch.CreateEntryFromFile(file.FullName, relative, CompressionLevel.SmallestSize);
                }
                catch (Exception ex)
                {
                    Logger?.WLog("Failed to add file to zip: " + file.FullName + " => " + ex.Message);
                }

                float percent = (current / count) * 100f;
                percentCallback?.Invoke(percent);
            }

            percentCallback?.Invoke(100);
            return true;
        }

        if (File.Exists(path))
        {
            percentCallback?.Invoke(0);
            try
            {
                using FileStream fs = new FileStream(output, FileMode.Create);
                using ZipArchive arch = new ZipArchive(fs, ZipArchiveMode.Create);
                arch.CreateEntryFromFile(path, Plugin.Helpers.FileHelper.GetShortFileName(path), CompressionLevel.SmallestSize);
            }
            catch (Exception ex)
            {
                return Result<bool>.Fail(ex.Message);
            }

            percentCallback?.Invoke(100);
            return true;
        }

        return Result<bool>.Fail("Path does not exist");
    }

    /// <inheritdoc />
    public Result<bool> Extract(string archivePath, string destinationPath, Action<float>? percentCallback = null)
    {
        // Check if the archive file exists
        if (File.Exists(archivePath) == false)
            return Result<bool>.Fail("Archive file not found: " + archivePath);

        try
        {
            // Check if the file is part of a multi-part RAR archive
            if (IsMultipartRar(archivePath))
            {
                return ExtractMultipartRar(archivePath, destinationPath);
            }

            if (Regex.IsMatch(archivePath, @"\.(iso|cue|img|dmg)$",
                    RegexOptions.CultureInvariant | RegexOptions.IgnoreCase))
            {
                Logger.ILog("Extracting disk image using 7-zip");
                return sevenHelper.Extract(archivePath, destinationPath, percentCallback);
            }

            var result = ExtractSharpCompress(archivePath, destinationPath, percentCallback);
            if (result.IsFailed == false)
                return result;

            if (sevenHelper.IsSevenZipArchive(archivePath))
                return sevenHelper.Extract(archivePath, destinationPath, percentCallback);

            // ZipFile.ExtractToDirectory(archivePath, destinationPath, overwriteFiles:true);
            return rarHelper.Extract(archivePath, destinationPath, percentCallback);

        }
        catch (Exception ex)
        {
            return Result<bool>.Fail("Failed to extract archive: " + ex.Message);
        }
    }

    /// <summary>
    /// Extracts a file using SharpCompress
    /// </summary>
    /// <param name="archivePath">the archive to extract</param>
    /// <param name="destinationPath">the location to extract to</param>
    /// <param name="percentCallback">the percent callback</param>
    /// <returns>the result of the extraction</returns>
    private Result<bool> ExtractSharpCompress(string archivePath, string destinationPath, Action<float>? percentCallback)
    {
        try
        {
            Logger.ILog("About to open archive: " + archivePath);
            int extractedFiles = 0;
            int totalFiles = 0;
            
            using (Stream streamCount = File.OpenRead(archivePath))
            {
                using var readerCount = ReaderFactory.Open(streamCount);
                while (readerCount.MoveToNextEntry())
                {
                    if (readerCount.Entry.IsDirectory)
                        continue;
                    totalFiles++;
                }
            }

            Logger.ILog("Total Files: " + totalFiles);
            
            using Stream stream = File.OpenRead(archivePath);
            using var reader = ReaderFactory.Open(stream);

            while (reader.MoveToNextEntry())
            {
                // Determine the type of the archive entry
                if (reader.Entry.IsDirectory)
                {
                    // Skip directories, as we're interested in files
                    continue;
                }

                // Extract the file
                var entryPath = Path.Combine(destinationPath, reader.Entry.Key);
                Logger?.ILog("Extracting file: " + entryPath);
                reader.WriteEntryToDirectory(destinationPath, new ExtractionOptions()
                {
                    ExtractFullPath = true, // Extract files with full path, ie in the appropriate sub directories
                    Overwrite = true // Overwrite existing files
                });
                
                extractedFiles++;
                float percentExtracted = (float)extractedFiles / totalFiles * 100;
                percentCallback?.Invoke(percentExtracted);
            }
            Logger.ILog($"Extracted: {extractedFiles} File{(extractedFiles)}");

            return true;
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail("Failed to extract archive: " + ex.Message);
        }
    }

    // /// <summary>
    // /// Extracts a file using SharpZipLib
    // /// </summary>
    // /// <param name="archivePath">the archive to extract</param>
    // /// <param name="destinationPath">the location to extract to</param>
    // /// <param name="percentCallback">the percent callback</param>
    // /// <returns>the result of the extraction</returns>
    // private Result<bool> ExtractSharpZipLib(string archivePath, string destinationPath, Action<float>? percentCallback)
    // {
    //     try
    //     {
    //         using (var fileStreamIn = new FileStream(archivePath, FileMode.Open, FileAccess.Read))
    //         using (var zipStream = new ZipInputStream(fileStreamIn))
    //         {
    //             ZipEntry entry;
    //             long totalBytes = new FileInfo(archivePath).Length;
    //             long extractedBytes = 0;
    //             while ((entry = zipStream.GetNextEntry()) != null)
    //             {
    //                 if (entry.IsDirectory)
    //                     continue;
    //
    //                 string entryPath = Path.Combine(destinationPath, entry.Name);
    //                 Logger?.ILog("Extracting file: " + entryPath);
    //             
    //                 using (var fileStreamOut = File.Create(entryPath))
    //                 {
    //                     byte[] buffer = new byte[4096];
    //                     int bytesRead;
    //                     while ((bytesRead = zipStream.Read(buffer, 0, buffer.Length)) > 0)
    //                     {
    //                         fileStreamOut.Write(buffer, 0, bytesRead);
    //                         extractedBytes += bytesRead;
    //                         float percentExtracted = (float)extractedBytes / totalBytes * 100;
    //                         percentCallback?.Invoke(percentExtracted);
    //                     }
    //                 }
    //             }
    //         }
    //
    //         return true;
    //     }
    //     catch (Exception ex)
    //     {
    //         return Result<bool>.Fail("Failed to extract archive: " + ex.Message);
    //     }
    // }

    /// <summary>
    /// Determines if the given file path is part of a multi-part RAR archive.
    /// </summary>
    /// <param name="archivePath">The path to the archive file.</param>
    /// <returns>True if the file is part of a multipart RAR archive, otherwise false.</returns>
    private bool IsMultipartRar(string archivePath)
    {
        var baseArchivePath = Path.GetDirectoryName(archivePath);
        var baseFileName = Path.GetFileNameWithoutExtension(archivePath);
        var extension = Path.GetExtension(archivePath).ToLower();

        // Check if the archive path ends with .rar or .rXX (where XX are digits)
        if (extension == ".rar")
        {
            // Check if there are other parts (e.g., .r01, .r02, etc.) in the same directory
            var otherParts = Directory.GetFiles(baseArchivePath, $"{baseFileName}.r*")
                .Where(f => f != archivePath && IsMultipartRarExtension(f))
                .ToList();
            return otherParts.Count > 0;
        }

        return IsMultipartRarExtension(archivePath);
    }

    /// <summary>
    /// Determines if the file has a multi-part RAR extension.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <returns>True if the file has a multipart RAR extension, otherwise false.</returns>
    private bool IsMultipartRarExtension(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLower();
        return extension == ".rar" || Regex.IsMatch(extension, @"\.(r)?[\d]{2,}$",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase) || 
               Regex.IsMatch(filePath, @"\.part[\d]+\.rar$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Extracts files from a multi-part RAR archive.
    /// </summary>
    /// <param name="archivePath">The path to the archive file.</param>
    /// <param name="destinationPath">The path where the files will be extracted.</param>
    /// <returns>A Result object indicating success or failure of the extraction.</returns>
    public Result<bool> ExtractMultipartRar(string archivePath, string destinationPath)
    {
        try
        {
            // Determine the base path for the multipart archive
            var baseArchivePath = Path.GetDirectoryName(archivePath);
            var baseFileName = Path.GetFileNameWithoutExtension(archivePath);

            // Collect all parts of the RAR archive
            var allFiles = new DirectoryInfo(baseArchivePath).GetFiles($"{baseFileName}.*");
            var archiveFiles = allFiles
                .Where(f => IsMultipartRarExtension(f.FullName))
                .OrderBy(f => f.Extension.Equals(".rar", StringComparison.InvariantCultureIgnoreCase) ? 0 : 1)
                .ThenBy(f => f.Name)
                .ToList();

            // Open the multipart RAR archive
            using var archive = RarArchive.Open(archiveFiles);
            foreach (var entry in archive.Entries)
            {
                if (!entry.IsDirectory)
                {
                    // Extract the file
                    var entryPath = Path.Combine(destinationPath, entry.Key);
                    Logger?.ILog("Extracting file: " + entryPath);
                    entry.WriteToDirectory(destinationPath, new ExtractionOptions()
                    {
                        ExtractFullPath = true, // Extract files with full path
                        Overwrite = true // Overwrite existing files
                    });
                }
            }

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail("Failed to extract multipart RAR archive: " + ex.Message);
        }
    }

    /// <summary>
    /// Precompiled is rar regex
    /// </summary>
    /// <returns>the rar regex</returns>
    [GeneratedRegex(@"\.(rar|cbr)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex IsRarRegex();
}