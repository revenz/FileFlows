/**
 * Determines if a video has a 5.1 channel track
 * @author John Andrews
 * @revision 1
 * @output Does have 5.1
 * @output Does not have 5.1
 */
 function Script()
 {
   // get the first video stream, likely the only one
   let vi =  Variables.vi?.VideoInfo;
   if(!vi)
     return 2;
   for(let audio of vi.AudioStream){
     if(audio.Channels === 5.1)
       return 1;
   }
   return 2;
 }