/**
 * @name Folder - Copy or Move Contents
 * @uid e35d65b5-1486-42b9-8a0c-651804adea88
 * @description Copies or Movies a subset of files from a folder based on supplied filename
 * @author apwelsh
 * @revision 1
 * @param {string} SourceDirectory The directory to copy (typically the folder being processed ex: {folder.FullName})
 * @param {string} DestinationDirectory The directory location to copy to (typically the location your servarr app is looking to for completed downloads)
 * @param {string} MediaFileName The file basename to copy all matching files of. The paths, and extension coponents will be ignored.  (typically {file.Name})
 * @param {bool} MoveFiles Set true to move, or false to copy
 * @output Directory copied
 */
function Script(SourceDirectory, DestinationDirectory, MediaFileName, MoveFiles)
 {
     MoveFiles = MoveFiles || false;
     let src = Flow.ReplaceVariables(SourceDirectory);
     let dest = System.IO.Path.Combine(Flow.ReplaceVariables(DestinationDirectory), System.IO.Path.GetFileName(src));

      Logger.ILog(`Source Directory: ${src}`);
      Logger.ILog(`Target Directory: ${dest}`);

     // Create filter of provided file basename, w/o extension, or * if none provided
     let filter = System.IO.Path.GetFileNameWithoutExtension(Flow.ReplaceVariables(MediaFileName || "*")) + ".*";

     if(System.IO.Directory.Exists(dest) == false)
        System.IO.Directory.CreateDirectory(dest);

     // recursively locate all the full paths of files matching the input criteria.
     let allFiles = System.IO.Directory.GetFiles(src, filter, System.IO.SearchOption.AllDirectories);
     for (let srcFile of allFiles) 
     {
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
    let allFolder = System.IO.Directory.GetDirectories(directory, "*", System.IO.SearchOption.AllDirectories);
    allFolder.sort((a,b) => b.length - a.length);

    for (subdir in System.IO.Directory.GetDirectories(directory))
    {
        if (System.IO.Directory.GetFiles(subdir).Length == 0 && System.IO.Directory.GetDirectories(subdir).Length == 0)
        {
            Logger.ILog(`Remove empty directory: ${subdir}`);
            Directory.Delete(subdir, false);
        }
    }
    if (System.IO.Directory.GetFiles(directory).Length == 0 && System.IO.Directory.GetDirectories(directory).Length == 0)
    {
        Logger.ILog(`Remove empty directory: ${directory}`);
        Directory.Delete(directory, false);
    }
}
