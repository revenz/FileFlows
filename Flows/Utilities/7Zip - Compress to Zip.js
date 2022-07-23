/**
 * Requires a 7Zip tool to be configured and will zip files
 * @author John Andrews
 * @revision 1
 * @param {string} ArchiveFile The name of the zip file to create, if empty a random name will be used
 * @param {string} Pattern The filename pattern to use, eg *.txt
 * @param {bool} SetWorkingFileToZip If the working file in the flow should be set to the newly created zip file
 * @output Zip file created
 */
 function Script(ArchiveFile, Pattern, SetWorkingFileToZip)
 {
   let output = '' + ArchiveFile; // ensures ArchiveFile is a string
   if(!output || output.trim().length == 0)
     output = Flow.TempPath + '/' + Flow.NewGuid() + '.zip';
   Logger.ILog('Output: ' + output);
     let sevenZip = Flow.GetToolPath('7zip');
 
     let process = Flow.Execute({
       command: sevenZip,
       argumentList: [
         'a',
         output,
         Pattern
     ]
     });
 
     if(process.exitCode !== 0){
       Logger.ELog('Failed to zip: ' + process.exitCode);
       return -1;
     }
 
     if(SetWorkingFileToZip)
       Flow.SetWorkingFile(output);
     return 1;
 }