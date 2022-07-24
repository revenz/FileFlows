/**
 * @name SABnzbd - Pause Downloads
 * @description Pauses SABnzbd if the current file processing queue is greater than 30 files
 * If the processing queue is not greater than 30 files, then it will resume SABnzbd
 * @revision 1
 */

import { SABnzbd } from '../../Shared/SABnzbd.js';
import { FileFlowsApi } from '../../Shared/FileFlowsApi';

let ffApi = new FileFlowsApi();
let unprocessed = ffApi.getUnprocessedCount();
Logger.ILog('unprocessed: ', unprocessed);

let sabnzbd = new SABnzbd();
if(unprocessed > 30)
    sabnzbd.pause();
else
    sabnzbd.resume();