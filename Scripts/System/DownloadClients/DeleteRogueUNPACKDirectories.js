/**
 * @name Delete Rogue _UNPACK_ Directories
 * @uid fadc8bec-cb54-4207-8606-ffa1c35f08e2
 * @description Will check any _UNPACK_ directories, and if they haven't be modified in 24 hours will delete that directory
 * @revision 2
 * @minimumVersion 1.0.0.0
 */

let folders = Variables['RogueUnpackDirectoryList'];
if(!folders){
    Logger.WLog('RogueUnpackDirectoryList variable not defined.')
    return;
}

folders = folders.trim().split('\n');
if(!folders.length){
    Logger.WLog('RogueUnpackDirectoryList variable has no folders.')
    return;
}

for(let folder of folders)
{
    var dir = new System.IO.DirectoryInfo(folder);      
    if(dir.Exists === false)  
        continue;
    Logger.ILog('Checking diretory: ' + folder);

    const cutoffDate =  new Date(Date.now() - (24 * 60 * 60 * 1000));
    let subdirs = dir.GetDirectories('_UNPACK_*', System.IO.SearchOption.AllDirectories);
    for(let subdir of subdirs)
    {        
        if(subdirs.Exists === false)
            continue;
            
        Logger.ILog('Found _UNPACK_ directory:' + subdir.FullName);
        
        if (subdir.LastWriteTime > cutoffDate || subdir.CreationTime > cutoffDate)
        {
            Logger.ILog('Directory was written/created recently: ' + subdir.FullName);
            continue;
        }

        try
        {
            Logger.ILog('Deleting rogue directory: ' + subdir.FullName);
            subdir.Delete(true);
        }
        catch(err)
        {
            Logger.WLog('Failed to delete directory: ' + ex.Message);
        }
    }
}