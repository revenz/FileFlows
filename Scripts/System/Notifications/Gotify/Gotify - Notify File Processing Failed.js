/**
 * @name Gotify - Notify File Processing Failed
 * @uid 4cbf36fe-cb45-4325-8d58-d369ebe774bb
 * @description Sends a Gotify notification when a file failed to be processed
 * @revision 5
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

gotify.sendMessage('File Processing Failed', `File failed to process ${file.Name}\nFrom Library ${library.Name}`);