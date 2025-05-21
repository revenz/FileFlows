/**
 * @name Video - Filesize bigger than calculated
 * @description Calculates a video's maximum filesize based on minutes and returns if it is bigger or not
 * @author esprit
 * @revision 2
 * @uid f3e173f1-e3a4-497c-9057-dc6debb3c8ac
 * @param {string} VideoFileSizeArray Comma-separated list of upper filesize limit for 4k, 1080p, 720p, SD in GB/hr e.g. 15.5,4,2,1
 * @output Video is Bigger than calculated
 * @output Video is Smaller than calculated
 */
function Script(VideoFileSizeArray)
 {
   // Array of upper filesize limit for 4k, 1080p, 720p, SD in GB/hr (default)
   if (VideoFileSizeArray.trim().length === 0) VideoFileSizeArray = "15.5,4,2,1";
   VideoFileSizeArray = VideoFileSizeArray.split(",");
   // convert GB into Bytes
   var Byteconversion = 1073741824;
   
   // get the first video stream, likely the only one
   let video = Variables.vi?.VideoInfo?.VideoStreams[0];
   if(!video)
       return -1;
   let VideoResolution = 3; // SD
   if(video.Width > 1200)
       VideoResolution = 2; // 720p
   if(video.Width > 1800)
       VideoResolution = 1; // 1080p
   if(video.Width > 3700)
       VideoResolution = 0; // 4k 

   let Videolength = Math.ceil(video.Duration.TotalMinutes);
   let MaxFileSizeCalculated = VideoFileSizeArray[VideoResolution] * Byteconversion / 60 * Videolength;

  //Logger.ILog("MaxFileSizeInGB: " + VideoFileSizeArray[VideoResolution]);
  //Logger.ILog("Byteconversion: " + Byteconversion);
  Logger.ILog("ActualVideoLength: " + Videolength + " min");
  Logger.ILog("Current Filesize: " + Variables.file.Size);
  Logger.ILog("Calculated Filesize: " + MaxFileSizeCalculated);

   if (Variables.file.Size > MaxFileSizeCalculated) {
       Logger.ILog("File is bigger than it could be.");
       return 1; // Bigger than calculated
   }
   else {
       Logger.ILog("File is small enough.");
       return 2; // Smaller than calculated
   }
 }
