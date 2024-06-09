import { Sonarr } from '../../../Shared/Sonarr';
import { Language } from '../../../Shared/Language';


/**
 * @author John Andrews 
 * @uid 51cf3c4f-f4a3-45e2-a083-6629397aab90
 * @revision 6
 * @description Lookups a file in Sonarr and gets its original language ISO-693-1 code for it
 * @param {string} Path The full file path to lookup in Sonarr
 * @param {bool} ISO2 If ISO-639-2 should be returned, otherwise ISO-639-1 will be used
 * @output The language was found and stored in the variable OriginalLanguage
 * @output The language was not found
 */
function Script(Path, ISO2)
{
    if(!Path)
        return 2;
    const sonarr = new Sonarr();
    try
    {
        let language = sonarr.getOriginalLanguageFromPath(Path.toString());
        if(!language)
            return 2;
            
        let helper = new Language();
        language = ISO2 ? helper.getIso2Code(language) : helper.getIso1Code(language);
        Logger.ILog('Got original language: ' + language);
        Variables.OriginalLanguage = language;
        return 1;
    }
    catch(err)
    {
        Logger.WLog('Error in script: ' + err);
        return 2;
    }
}