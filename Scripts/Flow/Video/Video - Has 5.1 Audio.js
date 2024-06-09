/**
 * Determines if a video has a 5.1 channel track
 * @author John Andrews
 * @uid c87f9e27-6587-490b-a51e-f65e7542b891
 * @revision 5
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
    if(audio.Deleted)
      continue;
    Logger.ILog("Audio channel found: " + audio.Channels);
    if (audio.Channels === 5.1)
      return 1;
  }

  return 2;
}