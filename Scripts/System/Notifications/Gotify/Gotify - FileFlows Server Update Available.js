/**
 * @name Gotify - FileFlows Server Update Available
 * @uid dcc90331-a51d-4399-9cdf-eb0e74f48158
 * @description Sends a Gotify notification when a FileFlows server update is available
 * @revision 4
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

gotify.sendMessage('FileFlows Update Available', `FileFlows Version ${version} is now available`);