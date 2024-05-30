/**
 * Determines if a video has a 7.1 channel track
 * @author David Maskell
 * @revision 1
 * @minimumVersion 1.0.0.0
 * @output Does have 7.1
 * @output Does not have 7.1
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
    if (audio.Channels === 7.1)
      return 1;
  }

  return 2;
}