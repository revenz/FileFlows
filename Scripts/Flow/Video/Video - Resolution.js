/**
 * @author reven
 * @uid d5962d9f-6200-4441-a0af-1c47f5ac8101
 * @revision 4
 * @description Determines a video's resolution and outputs accordingly
 * @output Video Is 4k
 * @output Video is 1080p
 * @output Video is 720p
 * @output Video is SD
 */
 function Script()
 {
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
 }