/**
 * @name Folder - Copy or Move Contents
 * @uid e35d65b5-1486-42b9-8a0c-651804adea88
 * @description Copies or Movies a subset of files from a folder based on supplied filename
 * @author apwelsh
 * @revision 5
 * @param {string} SourceDirectory The directory to copy (typically the folder being processed ex: {folder.FullName})
 * @param {string} DestinationDirectory The directory location to copy to (typically a subdirectoy in the location your servarr app is looking to for completed downloads)
 * @param {string} MediaFileName The file basename to copy all matching files of. The paths, and extension components will be ignored.  (typically {file.Name})
 * @param {bool} MoveFiles Set true to move, or false to copy. When true, files matching the ignore list will be deleted from SourceDirector.
 * @param {string} IgnoreExtensions (optional) Pipe ('|') delimited list of file extensions that will not be copied/moved.
 * @output Directory copied
 */
function Script(SourceDirectory, DestinationDirectory, MediaFileName, MoveFiles, IgnoreExtensions)
{
    MoveFiles = MoveFiles || false;
    IgnoreExtensions = IgnoreExtensions || '';
    let ignoreExtensionsList = IgnoreExtensions.split('|').map(ext => ext.trim().toLowerCase());
    let src = Flow.ReplaceVariables(SourceDirectory);
    let dest = Flow.ReplaceVariables(DestinationDirectory);

    Logger.ILog(`Source Directory: ${src}`);
    Logger.ILog(`Target Directory: ${dest}`);

    // Validate source directory
    if (!System.IO.Directory.Exists(src)) {
        throw new Error(`Source directory does not exist: ${src}`);
    }

    // Ensure all directories in the destination path exist
    if (!System.IO.Directory.Exists(dest)) {
        Logger.ILog(`Creating target directory and any missing parent directories: ${dest}`);
        System.IO.Directory.CreateDirectory(dest);
    } else {
        Logger.ILog(`Reusing existing target directory: ${dest}`);

        // Determine if the destination directory exists or needs to be created
    if (System.IO.Directory.Exists(dest)) {
        Logger.ILog(`Reusing existing target directory: ${dest}`);
    } else {
        Logger.ILog(`Creating target directory: ${dest}`);
        System.IO.Directory.CreateDirectory(dest);
    }

    // Create filter of provided file basename, w/o extension, or * if none provided
    let filter = System.IO.Path.GetFileNameWithoutExtension(Flow.ReplaceVariables(MediaFileName || "*")) + ".*";
    Logger.ILog(`Search filter: ${filter}`);
    
    // Recursively locate all the full paths of files matching the input criteria.
    let allFiles = System.IO.Directory.GetFiles(src, filter, System.IO.SearchOption.AllDirectories);
    for (let srcFile of allFiles) 
    {
        // Check if the file extension is in the ignore list
        let extension = System.IO.Path.GetExtension(srcFile).toLowerCase();
        if (ignoreExtensionsList.includes(extension)) {
            Logger.ILog(`Ignoring file: ${srcFile} with extension: ${extension}`);
            if (MoveFiles) {
                Logger.ILog(`Deleting ignored file: ${srcFile}`);
                System.IO.File.Delete(srcFile);
            }
            continue;
        }

        // This method will flatten a complex directory structure into files under a single folder.
        let dstFile = System.IO.Path.Combine(dest, System.IO.Path.GetFileName(srcFile));

        if (MoveFiles) {
            Logger.ILog(`Move File: ${srcFile} to: ${dstFile}`);
            System.IO.File.Move(srcFile, dstFile, true);
        } else {
            Logger.ILog(`Copy File: ${srcFile} to: ${dstFile}`);
            System.IO.File.Copy(srcFile, dstFile, true);
        }
    }

    if (MoveFiles) {
        cleanDirectory(src);
    }

    return 1;
}

function cleanDirectory(directory)
{
    let allFolders = System.IO.Directory.GetDirectories(directory, "*", System.IO.SearchOption.AllDirectories);
    allFolders.sort((a, b) => b.length - a.length);

    for (let subdir of allFolders)
    {
        if (System.IO.Directory.GetFiles(subdir).length == 0 && System.IO.Directory.GetDirectories(subdir).length == 0)
        {
            Logger.ILog(`Remove empty directory: ${subdir}`);
            System.IO.Directory.Delete(subdir, false);
        }
    }
}
