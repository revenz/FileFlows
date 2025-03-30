/**
 * @author reven
 * @uid c87f9e27-6587-490b-a51e-f65e7542b891
 * @description Determines if a video has a 5.1 channel track
 * @revision 7
 * @output Does have 5.1
 * @output Does not have 5.1
 */
function Script() 
{
  let audioStreams = Variables.vi?.VideoInfo?.AudioStreams;
  const TARGET_CHANNELS = 5.1;
  const TOLERANCE = 0.03;
  
  if (!audioStreams) {
    Logger.ILog("no audio streams");
    return 2;
  }
  for (let i = 0; i < audioStreams.length; i++) {
    let audio = audioStreams[i];
    if(audio.Deleted)
      continue;
    Logger.ILog("Audio channel found: " + audio.Channels);
    if (Math.abs(audio.Channels - TARGET_CHANNELS) <= TOLERANCE)
      return 1;
  }

  return 2;
}
