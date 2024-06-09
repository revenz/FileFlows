/**
 * @author Alexander von Schmidsfeld
 * @uid d4a4b12e-b9d5-47d1-b9d6-06f359cb0b2d
 * @description Checks if a video's fps is greater than specified.
 * @revision 5
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
   let fps = Math.ceil(vi.VideoStreams[0].FramesPerSecond);
   // Bugfix for way to high framerate detected by loosing the decimal point
   if(fps > 200) {
    fps = fps/100
   }
 
   // check if the fps is over the maximum bitrate
   if(fps > MAX_FPS) {
     // Logger.DLog(fps);
     Logger.ILog('The video framerate is ${fps} above the threshold of ${MAX_FPS}');
     return 1; // it is, so call output 1
     }
   Logger.ILog('The video framerate is ${fps} below the threshold of ${MAX_FPS}');
   return 2; // it isn't so call output 2 also for unknown fps
 }
