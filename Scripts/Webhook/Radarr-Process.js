/**
 * @name Radarr - Process
 * @uid 5cc2bf61-6379-4048-a472-aaf1be0e5f79
 * @description When a new file is added, call this webhook to process the file
 * @revision 2
 * @minimumVersion 1.1.1.0
 * @route radarr
 * @method POST
 */

import { FileFlowsApi } from './FileFlowsApi';

var json = Variables.Body;
let payload = JSON.parse(json);

let path = payload?.movie?.folderPath;
if(!path)
{
    Logger.WLog('Folder Path not passed in or not found in request');
    return false;
}

Logger.ILog('Path found: ' + path);

let ffApi = new FileFlowsApi();

let result = ffApi.processFile(path);
    
if(result === true)
{
    Logger.ILog('File added: ' + path);
    return true;
}

Logger.WLog(result || 'Unkown error');

return false;