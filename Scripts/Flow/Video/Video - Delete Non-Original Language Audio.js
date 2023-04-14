import { Language } from '../../../Shared/Language';

/**
 * Checks the "Movie Lookup" information for original language, and will delete any audio tracks 
 * with languages set that do not match the original language.
 * Requires the "Movie Lookup" node to be executed first to work
 * @author John Andrews
 * @revision 2
 * @minimumVersion 1.0.0.0
 * @param {bool} TreatUnknownAsBad Treat a track with no language set as a bad language and do not include it, otherwise it will be treated as a good track
 * @param {bool} KeepFirstAudio If no matching langauges are found, keep the first audio track, otherwise all audio could be removed
 * @output Audio tracks were deleted
 * @output Audio tracks were not deleted
 * @output No original language was found
 */
function Script(TreatUnknownAsBad, KeepFirstAudio)
{
  let lang = Variables.VideoMetadata?.OriginalLanguage;
  if(!lang)
  {
    Logger.ILog("No original language found");
    return 3;
  }

  Logger.ILog('Original Audio Language: ' + lang);
  let helper = new Language();
  let langIso = helper.getIso2Code(lang);
  Logger.ILog('Original Audio Language (ISO-2): ' + langIso);

  let ffModel = Variables.FfmpegBuilderModel;
  if(!ffModel)
  {
    Logger.ELog('FFMPEG Builder variable not found');
    return -1;
  }

  if(!ffModel.AudioStreams)
  {
    Logger.ELog("No video information with audio streams found")
    return -1; // no video information found
  }


  // ensure these variables are actually true or false
  TreatUnknownAsBad = !!TreatUnknownAsBad;
  KeepFirstAudio = !!KeepFirstAudio;

  let deleted = 0;
  let hasAudio = false;
  let firstAudio;
  for(let i=0;i<ffModel.AudioStreams.length;i++)
  {
    let audio = ffModel.AudioStreams[i];
    if(!audio) 
        continue;
    if(!firstAudio)
        firstAudio = audio;

    if(!audio.Language)
    {
        if(TreatUnknownAsBad){
          Logger.ILog("Audio language not set treating as bad, deleting track number " + (i + 1));
          audio.Deleted = true;
          ++deleted;
        }
        else
          hasAudio |= !audio.Deleted;
        continue;
    }
    let aLangIso = helper.getIso2Code(audio.Language);
    if(aLangIso == langIso || audio.Language == lang)
    {
        hasAudio |= !audio.Deleted;
        Logger.ILog("Matching language found, keeping: " + audio.Language);
        continue;
    }

    Logger.ILog("Audio language not original, deleting: " + audio.Language);
    audio.Deleted = true;
    ++deleted;
  }

  // make sure one audio track is still included
  if(hasAudio === false && firstAudio && KeepFirstAudio)
  {
    Logger.ILog('No audio remaining, so marking first audio track to not deleted');
    firstAudio.Deleted = false;
    --deleted;
  }

  return hasAudio === false ? 3 : 
         deleted > 0 ? 1 : 2;
}