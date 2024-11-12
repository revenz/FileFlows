/**
 * @author reven 
 * @uid 8efa6316-8108-4c1a-8aeb-49bb1c9f8323
 * @description Copies a folder from one location to another
 * @revision 4
 * @param {string} SourceDirectory The directory to copy
 * @param {string} DestinationDirectory The directory location to copy to
 * @output Directory copied
 */
 function Script(SourceDirectory, DestinationDirectory)
 {
     let src = Flow.ReplaceVariables(SourceDirectory);
     let dest = Flow.ReplaceVariables(DestinationDirectory);
     let allDirectories = System.IO.Directory.GetDirectories(src, "*", System.IO.SearchOption.AllDirectories)

     if(System.IO.Directory.Exists(dest) == false)
        System.IO.Directory.CreateDirectory(dest);

     for(let dir of allDirectories) 
     { 
         let dirToCreate = dir.replace(src, dest); 
         System.IO.Directory.CreateDirectory(dirToCreate); 
     }
  
     let allFiles = System.IO.Directory.GetFiles(src, "*.*", System.IO.SearchOption.AllDirectories);
     for (let newPath of allFiles) 
     {
         System.IO.File.Copy(newPath, newPath.replace(src, dest), true); 
     } 
 
     return 1;
 }