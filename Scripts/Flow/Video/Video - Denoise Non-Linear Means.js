/**
 * @description Denoise frames using Non-Local Means algorithm.
 * @author Alexander von Schmidsfeld
 * @uid b4ed740e-b851-4336-b412-a63ddc64ca0b
 * @revision 4
 * @param {string} Strength Set denoising strength. Default is 1.0. Must be in range [1.0, 30.0].
 * @param {int} Patch Set patch size. Default is 5. Must be odd number in range [0, 99].
 * @param {int} Research Set research size. Default is 7. Must be odd number in range [0, 99].
 * @output Deblocked Video 
 */

function Script(strength,patch,research)
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
  
  Logger.ILog(`Denoising the Video using Non-Local Means algorithm`);
  video.Filter.Add(`nlmeans=${strength}:${patch}:${patch}:${research}:${research}`);
  return 1;
}
