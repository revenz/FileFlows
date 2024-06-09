import { Radarr } from '../../../Shared/Radarr';
import { Language } from '../../../Shared/Language';

/**
* Lookups a file in Radarr and gets its original language ISO-693-1 code for it
* @author John Andrews 
* @uid 3915f110-4b07-4e11-b7b9-50de3f5a1255
* @revision 5
* @minimumVersion 1.0.0.0
* @param {string} Path The full file path to lookup in Radarr
* @param {bool} ISO2 If ISO-639-2 should be returned, otherwise ISO-639-1 will be used
* @output The language was found and stored in the variable OriginalLanguage
* @output The language was not found
*/
function Script(Path, ISO2)
{
    if(!Path)
        return 2;
    const radarr = new Radarr();
    try
    {
        let language = radarr.getOriginalLanguageFromPath(Path.toString());
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