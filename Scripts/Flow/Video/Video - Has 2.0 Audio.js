/**
 * Determines if a video has a 2.0 (stereo) channel track
 * @author John Andrews
 * @revision 3
 * @minimumVersion 1.0.0.0
 * @output Does have 2.0
 * @output Does not have 2.0
 */
function Script() 
{
  let audioStreams = Variables.vi?.VideoInfo?.AudioStreams;
  if (!audioStreams) {
    Logger.ILog("no audio streams");
    return 2;
  }

  for (let i = 0; i < audioStreams.length; i++) {
    let audio = audioStreams[i];
    if(audio.Deleted)
      continue;
    Logger.ILog("Audio channel found: " + audio.Channels);
    if (audio.Channels === 2)
      return 1;
  }

  return 2;
}