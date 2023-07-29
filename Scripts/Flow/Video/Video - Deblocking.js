/**
 * Remove blocking artifacts from input video. 
 * @author Alexander von Schmidsfeld
 * @revision 1
 * @minimumVersion 1.0.0.0
 * @param {string} Filter Set filter type, can be weak or strong. Default is strong.
 * @param {int} Block Set size of block, allowed range is from 4 to 512. Default is 8.
 * @output Deblocked Video 
 */
function Script(filter,block)
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
  
  Logger.ILog(`Removeing  blocking artifacts using the ${filter} preset`);
  video.Filter.Add(`deblock=filter=${filter}:block=${block}`);
  return 1;
}
