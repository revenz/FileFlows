/**
 * @name Apprise - FileFlows Server Update Available
 * @uid 964cd7c4-a763-4be8-b5f4-f81568d67950
 * @description Sends a Apprise notification when a FileFlows server update is available
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

apprise.sendMessage('info', `FileFlows Version ${version} is now available`);