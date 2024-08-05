/**
 * A switch statement for video resolution
 * @revision 1
 * @outputs 4
 * @minimumVersion 24.08.1.3441
 */

// get the first video stream, likely the only one
if(Variables.TryGetValue("vi.VideoInfo", out var oVideoInfo) == false || oVideoInfo is FileFlows.VideoNodes.VideoInfo videoInfo == false)
{
    Logger.ILog("Failed to locate VideoInformation in variables");
    return -1;
}
Logger.ILog("Got video information.");
var video = videoInfo.VideoStreams.FirstOrDefault();
if(video == null)
{
    Logger.ILog("No video streams detected.");
    return -1;
}

Logger.ILog("Video stream detected.");
if(video.Width > 3700)
    return 1; // 4k 
if(video.Width > 1800)
    return 2; // 1080p
if(video.Width > 1200)
    return 3; // 720p
return 4; // SD