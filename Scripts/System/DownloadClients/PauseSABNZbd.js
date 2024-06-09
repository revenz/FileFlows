/**
 * @name SABnzbd - Pause Downloads
 * @uid 086f8a37-5e0c-4ee9-a033-44153198e9bf
 * @description Pauses SABnzbd if the current file processing queue is greater than 30 files.  If the processing queue is not greater than 30 files, then it will resume SABnzbd
 * @revision 8
 * @minimumVersion 1.0.0.0
 */

import { SABnzbd } from '../../Shared/SABnzbd';
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
    maxQueueLength = 20;
Logger.ILog("Max Queue Length: " + maxQueueLength);

let minimumFreeGBs = Variables['SABnzbd.MinimumFreeGBs'];
if(minimumFreeGBs)
    minimumFreeGBs = parseFloat(minimumFreeGBs, 10);
if(!minimumFreeGBs || isNaN(minimumFreeGBs))
    minimumFreeGBs = 100;
Logger.ILog(`Minimum Free Space: ${minimumFreeGBs} GB`);

let sabnzbd = new SABnzbd();
if(status.Unprocessed > maxQueueLength)
    sabnzbd.pause();
else
{
    // check disk space before resuming
    let freeGBs = sabnzbd.getFreeDiskSpace();
    if(freeGBs > minimumFreeGBs)
    {
        Logger.ILog(`Free space left: ${freeGBs}`)
        sabnzbd.resume();
    }
    else
        Logger.ILog(`Free space less ${freeGBs} GB than ${minimumFreeGBs} GB, not resuming`);
}