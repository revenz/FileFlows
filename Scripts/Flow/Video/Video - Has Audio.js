/**
 * @author reven
 * @uid c57f5849-0df1-4882-b668-62145bda1304
 * @description Checks if there is an audio track on a video file
 * @revision 4
 * @output Has an audio track
 * @output Does not have an audio track
 */
 function Script()
 {
     let hasAudio = !!Variables.vi?.VideoInfo?.AudioStreams?.length;
     return hasAudio ? 1 : 2;
 }