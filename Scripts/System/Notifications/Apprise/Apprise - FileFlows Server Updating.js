/**
 * @name Apprise - FileFlows Server Updating
 * @uid 9b3451e1-a40c-4dcb-8640-e477f477686f
 * @description Sends a Apprise notification the FileFlows Server is being automatically updated
 * @revision 3
 * @minimumVersion 1.0.0.0
 */

import { Apprise } from '../../../Shared/Apprise';

let apprise = new Apprise();

let version = Variables.Version;
if(!version)
{
    Logger.WLog('This script is expected to run with a update event');
    return;   
}

apprise.sendMessage('info', `FileFlows Version ${version} is now being automatically installed`);