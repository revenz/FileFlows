/**
 * @author reven
 * @uid 62b01a65-b602-49cb-ac81-8583ead54ca7
 * @description If a video's resolution is greater than 1080p, this script will update the FFMPEG Builder video track with a filter to downscale the video to 1080p when the FFMPEG Builder Executor runs.
 * @revision 6
 * @output Video is greater than 1080p, FFMPEG Builder Updated
 * @output Video is not greater than 1080p
 */
function Script()
{
  let ffmpeg = Variables.FfmpegBuilderModel;
  if(!ffmpeg)
  {
    Logger.ELog('FFMPEG Builder variable not found');
    return -1;
  }

  let video = ffmpeg.VideoStreams[0];
  if(!video)
  {
    Logger.ELog('FFMPEG Builder no video stream found');
    return -1;
  }


  if (video.Stream.Width < 1921)
  {
    Logger.ILog('Do not need to downscale');
    return 2;
  }
  
  Logger.ILog(`Need to downscale from ${video.Stream.Width}x${video.Stream.Height}`);
  video.Filter.Add(`scale=1920:-2:flags=lanczos`);
  return 1;
}