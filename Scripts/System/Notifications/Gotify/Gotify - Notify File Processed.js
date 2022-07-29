/**
 * @name Gotify - Notify File Processed
 * @description Sends a Gotify notification when a file has been successfully processed
 * @revision 1
 */

 import { Gotify } from '../../../Shared/Gotify.js';
 
 let gotify = new Gotify();

 var file = Variables.LibraryFile;
 var library = Variables.Library;
 if(!file || !library)
{
    Logger.WLog('This script is expected to run with a file event');
    return;   
}

gotify.sendMessage('File Processed', `File processed ${file.Name}\nFrom Library ${library.Name}`);