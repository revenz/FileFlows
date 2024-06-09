/**
 * Checks if there is an audio track on a video file
 * @author John Andrews
 * @uid c57f5849-0df1-4882-b668-62145bda1304
 * @revision 3
 * @minimumVersion 1.0.0.0
 * @output Has an audio track
 * @output Does not have an audio track
 */
 function Script()
 {
     let hasAudio = !!Variables.vi?.VideoInfo?.AudioStreams?.length;
     return hasAudio ? 1 : 2;
 }