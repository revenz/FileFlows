/**
 * @author reven
 * @uid c6c1a4b6-5f50-4b7d-81bd-e8d38af2b698
 * @description Checks if a video's bitrate is greater than specified.
 * @revision 4
 * @param {int} MaxBitrateKbps The maximum to check in KBps
 * @output Video's bitrate is greater than specified
 * @output Video's bitrate is not greater than specified
 */
 function Script(MaxBitrateKbps)
 {
   // check if the bitrate for a video is over a certain amount
   let MAX_BITRATE = MaxBitrateKbps * 1000; 
 
   let vi = Variables.vi?.VideoInfo;
   if(!vi || !vi.VideoStreams || !vi.VideoStreams[0])
     return -1; // no video information found
 
   // get the video stream
   let bitrate = vi.VideoStreams[0].Bitrate;
 
   if(!bitrate)
   {
     // video stream doesn't have bitrate information
     // need to use the overall bitrate
     let overall = vi.Bitrate;
     if(!overall)
       return 0; // couldn't get overall bitrate either
 
     // overall bitrate includes all audio streams, so we try and subtract those
     let calculated = overall;
     if(vi.AudioStreams?.length) // check there are audio streams
     {
       for(let audio of vi.AudioStreams)
       {
         if(audio.Bitrate > 0)
           calculated -= audio.Bitrate;
         else{
           // audio doesn't have bitrate either, so we just subtract 5% of the original bitrate
           // this is a guess, but it should get us close
           calculated -= (overall * 0.05);
         }
       }
     }
     bitrate = calculated;
   }
 
   // check if the bitrate is over the maximum bitrate
   if(bitrate > MAX_BITRATE)
     return 1; // it is, so call output 1
   return 2; // it isn't so call output 2
 }