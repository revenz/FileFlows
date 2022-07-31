/**
 * @name Apprise - FileFlows Server Update Available
 * @description Sends a Apprise notification when a FileFlows server update is available
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

apprise.sendMessage('info', `FileFlows Version ${version} is now available`);