/**
 * If a video's resolution is greater than 720p, this script will update the FFMPEG Builder video track with a filter 
 * to downscale the video to 720p when the FFMPEG Builder Executor runs.
 * @author John Andrews
 * @revision 1
 * @minimumVersion 1.0.0.0
 * @output Video is greater than 720p, FFMPEG Builder Updated
 * @output Video is not greater than 720p
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


  if (video.Stream.Width < 1281)
  {
    Logger.ILog('Do not need to downscale');
    return 2;
  }
  
  Logger.ILog(`Need to downscale from ${video.Width}x${video.Height}`);
  video.Filter.Add(`scale=1280:-2:flags=lanczos`);
  return 1;
}