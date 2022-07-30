/**
 * Checks if there is an audio track on a video file
 * @author John Andrews
 * @revision 2
 * @minimumVersion 1.0.0.0
 * @output Has an audio track
 * @output Does not have an audio track
 */
 function Script()
 {
     let hasAudio = !!Variables.vi?.VideoInfo?.AudioStreams?.length;
     return hasAudio ? 1 : 2;
 }