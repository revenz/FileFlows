/**
 * @name Nvidia - Encoder Check
 * @description Checks to see if the NVIDIA encoder is available before an encode and if not will 
 * request FileFlows restart
 * @revision 1
 * @minimumVersion 1.0.0.0
 */

import { FileFlowsApi } from '../../Shared/FileFlowsApi';

let ffApi = new FileFlowsApi();
let ffmpeg = ffApi.getVariable('ffmpeg') || 'ffmpeg';
let args = [
    "-loglevel",
    "error",
    "-f",
    "lavfi",
    "-i",
    "color=black:s=1080x1080",
    "-vframes",
    "1",
    "-an",
    "-c:v",
    "hevc_nvenc", // the encoder
    "-f",
    "null",
    "-"
];

let result = Execute({
    Command: ffmpeg,
    ArgumentList: args
});

if(result.ExitCode !== 0) 
{
    Logger.WLog("Could not use NVIDIA hardware encoder");
    return false;
}
Logger.ILog("Successfully able to use NVIDA hardware encoder");
return true;