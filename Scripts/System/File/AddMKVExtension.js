/**
 * @name Add MKV extension to files without an extension
 * @uid bf5d95be-8fb7-4dc2-91da-9efcdcd086db
 * @description Monitors the directories listed in the variable AddExtensionDirectoryList and any file thats found without an extenion, MKV will be added to it
 * @revision 2
 * @minimumVersion 1.0.0.0
 */

let folders = Variables['AddExtensionDirectoryList'];
if(!folders){
    Logger.WLog('AddExtensionDirectoryList variable not defined.')
    return;
}

folders = folders.trim().split('\n');
if(!folders.length){
    Logger.WLog('AddExtensionDirectoryList variable has no folders.')
    return;
}

for(let folder of folders)
{

    var dir = new System.IO.DirectoryInfo(folder);
    Logger.ILog('Dir exists:'  + dir.Exists);
    var files = dir.GetFiles("*",  System.IO.SearchOption.AllDirectories);

    Logger.ILog('Files: ' + files.length);

    for(var file of files)
    {
        let name = file.FullName;

        if(name.endsWith('.mp4.mkv') || name.endsWith('.avi.mkv')){
            file.MoveTo(name.substring(0, name.length - 4));
            continue;
        }

        if(name !== file.Directory.FullName && name.indexOf('.mkv.mkv') < 0)
        {
            let index = name.lastIndexOf('.');
            if(index > 0)
            {
                let extension = name.substring(index + 1);
                Logger.ILog('Extension: '+ extension);
                if(extension.length < 5)
                    continue;
            }
        }
        if (file.Length < 200000000)
            continue;
            
        if(name.indexOf('.mkv.mkv') > 0)
            name = name.replace(/\.mkv/g, '');

        Logger.ILog("Adding MKV extension to file: " + file.FullName);

        file.MoveTo(name + ".mkv");
    }
}