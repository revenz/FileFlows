/**
 * A switch statement for video resolution
 * @revision 1
 * @outputs 4
 */

// get the first video stream, likely the only one
let video = Variables.vi?.VideoInfo?.VideoStreams[0];
if(!video)
    return -1; // no video streams detected
if(video.Width > 3700)
    return 1; // 4k 
if(video.Width > 1800)
    return 2; // 1080p
if(video.Width > 1200)
    return 3; // 720p
return 4; // SD