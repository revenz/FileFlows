/**
 * @name Apprise - FileFlows Server Updating
 * @description Sends a Apprise notification the FileFlows Server is being automatically updated
 * @revision 1
 * @minimumVersion 1.0.0.0
 */

import { Apprise } from '../../../Shared/Apprise.js';

let apprise = new Apprise();

let version = Variables.Version;
if(!version)
{
    Logger.WLog('This script is expected to run with a update event');
    return;   
}

apprise.sendMessage('info', `FileFlows Version ${version} is now being automatically installed`);