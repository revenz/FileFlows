/**
 * @name Gotify - FileFlows Server Updating
 * @uid 81b67ae5-fc97-44e9-a36d-619bd965af89
 * @description Sends a Gotify notification the FileFlows Server is being automatically updated
 * @revision 3
 * @minimumVersion 1.0.0.0
 */

import { Gotify } from '../../../Shared/Gotify';

let gotify = new Gotify();

let version = Variables.Version;
if(!version)
{
    Logger.WLog('This script is expected to run with a update event');
    return;   
}

gotify.sendMessage('FileFlows Updating', `FileFlows Version ${version} is now being automatically installed`);