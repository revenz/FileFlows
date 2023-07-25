/**
 * Checks if a video's fps is greater than specified.
 * @author Alexander
 * @revision 2
 * @minimumVersion 1.0.0.0
 * @param {int} MaxFps The maximum to check in fps
 * @output Video's fps is greater than specified
 * @output Video's fps is not greater than specified
 */
function Script(MaxFps)
{
  // check if the fps for a video is over a certain amount
  let MAX_FPS = MaxFps * 1.0; 

  let vi = Variables.vi?.VideoInfo;
  if(!vi || !vi.VideoStreams || !vi.VideoStreams[0])
    return -1; // no video information found

  // get the video stream
  let fps = vi.VideoStreams[0].FramesPerSecond;

  // check if the fps is over the maximum bitrate
  if(fps > MAX_FPS)
    return 1; // it is, so call output 1
  return 2; // it isn't so call output 2
}