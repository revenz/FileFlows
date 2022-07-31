/**
 * @name Apprise - Notify File Processed
 * @description Sends a Apprise notification when a file has been successfully processed
 * @revision 2
 * @minimumVersion 1.0.0.0
 */

import { Apprise } from '../../../Shared/Apprise';

let apprise = new Apprise();

let file = Variables.LibraryFile;
let library = Variables.Library;
if(!file || !library)
{
    Logger.WLog('This script is expected to run with a file event');
    return;   
}

apprise.sendMessage('success', `File processed ${file.Name}\nFrom Library ${library.Name}`);