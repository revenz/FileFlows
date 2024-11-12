/**
 * @author reven
 * @uid 3a3909c7-aded-45d7-8340-b27e76589b02
 * @revision 6
 * @description Gets either TV Show Information or Movie Information from a meta flow element and updates any audio tracks with missing language codes 
 * with the original language of the show/movie
 * Requires the "Movie Lookup" or "TV Show Lookup" to be executed first to work
 * @output Tracks updated
 * @output No tracks updated
 */
function Script()
{
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
  
    var videoMetadata = Variables.VideoMetadata;
    var lang = videoMetadata?.OriginalLanguage || 'eng';
    let changed = false;

    lang = LanguageHelper.GetIso2Code(lang);

  
    for(let i=0;i<ffModel.AudioStreams.length;i++)
    {
        let audio = ffModel.AudioStreams[i];
        if(!audio || audio.Language) 
            continue;
        Logger.ILog('Setting language on track to ' + lang);
        audio.Language = lang;
        changed = true;
        ffModel.ForceEncode = true;
    }

    return changed ? 1 : 2;
}