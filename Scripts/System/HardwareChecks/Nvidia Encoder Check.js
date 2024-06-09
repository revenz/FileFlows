/**
 * @name Nvidia - Encoder Check
 * @uid 55de29ea-749d-4c64-88ca-497fdcd9a1be
 * @description Checks to see if the NVIDIA encoder is available before an encode and if not will request FileFlows restart
 * @revision 8
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

Logger.ILog("About to test NVIDIA hardware encoder");

let result = Execute({
    Command: ffmpeg,
    ArgumentList: args
});

if(result.ExitCode === 0) 
{
    Logger.ILog("Successfully able to use NVIDIA hardware encoder");
    return true;
}

Logger.WLog("Could not use NVIDIA hardware encoder, requesting restart");
return 'restart';