import { Language } from '../../../Shared/Language';

/**
 * Gets either TV Show Information or Movie Information from a meta flow element and updates any audio tracks with missing language codes 
 * with the original language of the show/movie
 * Requires the "Movie Lookup" or "TV Show Lookup" to be executed first to work
 * @author John Andrews
 * @revision 1
 * @minimumVersion 23.08.3.0
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

    let helper = new Language();
    lang = helper.getIso2Code(lang);

  
    for(let i=0;i<ffModel.AudioStreams.length;i++)
    {
        let audio = ffModel.AudioStreams[i];
        if(!audio || audio.Language) 
            continue;
        Logger.ILog('Setting language on track to ' + lang);
        audio.Language = lang;
        changed = true;
    }

    return changed ? 1 : 2;
}