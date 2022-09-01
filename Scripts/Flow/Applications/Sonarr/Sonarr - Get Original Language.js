import { Sonarr } from '../../../Shared/Sonarr';
import { Language } from '../../../Shared/Language';


/**
* Lookups a file in Sonarr and gets its original language ISO-693-1 code for it
* @author John Andrews 
* @revision 2
* @minimumVersion 1.0.0.0
* @param {string} Path The full file path to lookup in Sonarr
* @output The language was found and stored in the variable OriginalLanguage
* @output The language was not found
*/
function Script(Path)
{
    const sonarr = new Sonarr();
    try
    {
        let language = sonarr.getOriginalLanguageFromPath(Path);
        if(!language)
            return 2;
            
        let helper = new Language();
        language = helper.getIsoCode(language);
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