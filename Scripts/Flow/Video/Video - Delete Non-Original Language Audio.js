/**
 * @author reven
 * @author Shaun Agius
 * @uid d3d80753-4c85-4202-af33-ba73e585c771
 * @revision 8
 * @description Checks the "Movie Lookup"/"TV Show Lookup" information for original language, and will delete any audio tracks with languages set that do not match the original language.  Requires the "Movie Lookup"/"TV Show Lookup" node to be executed first to work
 * @param {bool} TreatUnknownAsBad Treat a track with no language set as a bad language and do not include it, otherwise it will be treated as a good track
 * @param {bool} KeepFirstAudio If no matching langauges are found, keep the first audio track, otherwise all audio could be removed
 * @param {string} OtherLanguages The ISO 639-2 language code to use
 * @output Audio tracks were deleted
 * @output Audio tracks were not deleted
 * @output No audio tracks found
 */
function Script(TreatUnknownAsBad, KeepFirstAudio, OtherLanguages)
{
  let lang = Variables.VideoMetadata?.OriginalLanguage;
  if(!lang)
  {
    Logger.ILog("No original language found");
    return 3;
  }

  Logger.ILog('Original Audio Language: ' + lang);
  let langIso = LanguageHelper.GetIso2Code(lang);
  Logger.ILog('Original Audio Language (ISO-2): ' + langIso);

  let oLangIsos = (OtherLanguages === undefined) ? "x" : String(OtherLanguages).split(",");
  let oLangs = [];
  for(let i=0;i<oLangIsos.length;i++)
  {
    if(oLangIsos[i].length > 2 && oLangIsos[i] != langIso)
    {
      let oLang = LanguageHelper.GetIso2Code(oLangIsos[i]);
      oLangs.push(oLang);
      Logger.ILog(`Other Audio Language:${oLang} (ISO-2):${oLangIsos[i]}`);
    }
  }
  
  oLangIsos.push(langIso);
  oLangs.push(lang);

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


  for(let i=0;i<ffModel.AudioStreams.length;i++)
  {
    let audio = ffModel.AudioStreams[i];
    Logger.ILog(`Original Track Status[${i}] [Lang: ${audio.Language}] [Deleted: ${audio.Deleted}]`);
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
    let aLangIso = LanguageHelper.GetIso2Code(audio.Language);
    if(oLangIsos.includes(aLangIso) || oLangs.includes(audio.Language))
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


  for(let i=0;i<ffModel.AudioStreams.length;i++)
  {
    let audio = ffModel.AudioStreams[i];
    Logger.ILog(`Final Track Status[${i}] [Lang: ${audio.Language}] [Deleted: ${audio.Deleted}]`);
  }

  return hasAudio === false ? 3 : 
         deleted > 0 ? 1 : 2;
}
