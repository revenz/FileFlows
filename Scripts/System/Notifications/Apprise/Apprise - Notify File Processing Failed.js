/**
 * @name Apprise - Notify File Processing Failed
 * @description Sends a Apprise notification when a file failed to be processed
 * @revision 3
 * @minimumVersion 1.0.0.0
 */

import { Apprise } from '../../../Shared/Apprise.js';

let apprise = new Apprise();

let file = Variables.LibraryFile;
let library = Variables.Library;
if(!file || !library)
{
    Logger.WLog('This script is expected to run with a file event');
    return;   
}

apprise.sendMessage('error', `File failed to process ${file.Name}\nFrom Library ${library.Name}`);