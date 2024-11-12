/**
 * @author reven
 * @uid f9928be4-23e3-43d8-aa5f-6e0d5a8fc736
 * @description Determines if a video has a 2.0 (stereo) channel track
 * @revision 5
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