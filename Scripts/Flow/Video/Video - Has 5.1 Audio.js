/**
 * Determines if a video has a 5.1 channel track
 * @author John Andrews
 * @revision 3
 * @minimumVersion 1.0.0.0
 * @output Does have 5.1
 * @output Does not have 5.1
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
    Logger.ILog("Audio channel found: " + audio.Channels);
    if (audio.Channels === 5.1)
      return 1;
  }

  return 2;
}