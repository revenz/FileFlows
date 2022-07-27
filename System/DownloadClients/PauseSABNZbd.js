/**
 * @name SABnzbd - Pause Downloads
 * @description Pauses SABnzbd if the current file processing queue is greater than 30 files
 * If the processing queue is not greater than 30 files, then it will resume SABnzbd
 * @revision 3
 */

import { SABnzbd } from '../../Shared/SABnzbd.js';
import { FileFlowsApi } from '../../Shared/FileFlowsApi';

let ffApi = new FileFlowsApi();
let status = ffApi.getStatus();
Logger.ILog('Unprocessed: ' + status.Unprocessed);
Logger.ILog('Processing: ' + status.Processing);
Logger.ILog('Processed: ' + status.Processed);
let maxQueueLength = Variables['SABnzbd.MaxQueueLength'];
if(maxQueueLength)
    maxQueueLength = parseInt(maxQueueLength, 10);
if(!maxQueueLength || isNaN(maxQueueLength))
    maxQueueLength = 30;
Logger.ILog("Max Queue Length: " + maxQueueLength);

let sabnzbd = new SABnzbd();
if(status.Unprocessed > maxQueueLength)
    sabnzbd.pause();
else
{
    // check disk space before resuming
    let freeGBs = sabnzbd.getFreeDiskSpace();
    if(freeGBs > 100)
    {
        Logger.ILog(`Free space left: ${freeGBs}`)
        sabnzbd.resume();
    }
    else
        Logger.ILog(`Free space less ${freeGBs} than 50GB, not resuming`);
}