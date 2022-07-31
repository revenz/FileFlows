/**
 * @name Gotify - Notify File Processed
 * @description Sends a Gotify notification when a file has been successfully processed
 * @revision 4
 * @minimumVersion 1.0.0.0
 */

import { Gotify } from '../../../Shared/Gotify';

let gotify = new Gotify();

let file = Variables.LibraryFile;
let library = Variables.Library;
if(!file || !library)
{
    Logger.WLog('This script is expected to run with a file event');
    return;   
}

gotify.sendMessage('File Processed', `File processed ${file.Name}\nFrom Library ${library.Name}`);