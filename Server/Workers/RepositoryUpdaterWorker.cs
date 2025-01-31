using System.Text.RegularExpressions;
using FileFlows.Services;
using FileFlows.ServerShared.Workers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Workers;

/// <summary>
/// A worker that runs FileFlows Tasks
/// </summary>
public class RepositoryUpdaterWorker: ServerWorker
{
    private static RepositoryUpdaterWorker Instance;
    
    
    /// <summary>
    /// Creates a new instance of the Scheduled Task Worker
    /// </summary>
    public RepositoryUpdaterWorker() : base(ScheduleType.Daily, 5)
    {
        Instance = this;
        Execute();
    }
    
    /// <inheritdoc />
    protected override void ExecuteActual(Settings settings)
    {
        var service = ServiceLoader.Load<RepositoryService>();
        service.Init().Wait();
        service.DownloadLibraryTemplates().Wait();
        service.DownloadFunctionScripts().Wait();

        ServiceLoader.Load<ScriptService>().RescanFunctionTemplates();

        new RevisionCleaner(DirectoryHelper.TemplateDirectoryFlow).DeleteOldRevisions();
    }

    class RevisionCleaner
    {
        private readonly string FolderPath;

        /// <summary>
        /// Creates an instance
        /// </summary>
        /// <param name="folderPath">the folder path</param>
        public RevisionCleaner(string folderPath)
        {
            this.FolderPath = folderPath;
        }

        /// <summary>
        /// Deletes older revisions of JSON files based on a naming convention.
        /// </summary>
        public void DeleteOldRevisions()
        {
            if (Directory.Exists(FolderPath) == false)
                return;

            // Group files by base filename and sort by revision number
            var revisionGroups = Directory.GetFiles(FolderPath, "*_*.json")
                .Where(file => GetRevisionNumber(file) >= 0)
                .GroupBy(file => GetBaseFileName(file))
                .Select(group => group.OrderByDescending(file => GetRevisionNumber(file)));

            foreach (var group in revisionGroups)
            {
                foreach (var file in group.Skip(1)) // Skip the newest revision
                {
                    Logger.Instance.ILog("Deleting old file revision: " + file);
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.WLog("Failed to delete file: " + ex.Message);
                    }
                }
            }
        }

        /// <summary>
        /// Extracts the base filename from a file path.
        /// </summary>
        /// <param name="filePath">The full path of the file.</param>
        /// <returns>The base filename without extension.</returns>
        string GetBaseFileName(string filePath)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            int lastUnderscoreIndex = fileName.LastIndexOf('_');
            if (lastUnderscoreIndex >= 0)
            {
                return fileName.Substring(0, lastUnderscoreIndex);
            }
            return fileName;
        }

        /// <summary>
        /// Extracts the revision number from a filename.
        /// </summary>
        /// <param name="fileName">The filename to extract the revision number from.</param>
        /// <returns>The extracted revision number.</returns>
        int GetRevisionNumber(string fileName)
        {
            Match match = Regex.Match(fileName, @"_(\d+)\.json$");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int revisionNumber))
                return revisionNumber;

            return -1; // Default revision number if parsing fails
        }
    }
}